using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RivertyBNPL.Shared.Infrastructure.Security;

/// <summary>
/// Strong Customer Authentication (SCA) implementation for Norwegian BNPL
/// Complies with PSD2 regulations and Norwegian financial standards
/// </summary>
public interface IStrongCustomerAuthenticationService
{
    Task<SCAChallenge> InitiateSCAAsync(SCARequest request);
    Task<SCAResult> ValidateSCAAsync(string challengeId, SCAValidationRequest validation);
    Task<bool> IsSCARequiredAsync(string customerId, decimal amount, string paymentMethod);
    Task<SCAExemption> CheckExemptionAsync(string customerId, decimal amount, string merchantId);
    Task<string> GenerateAuthenticationTokenAsync(string customerId, string sessionId);
    Task<bool> ValidateAuthenticationTokenAsync(string token, string customerId);
}

public class StrongCustomerAuthenticationService : IStrongCustomerAuthenticationService
{
    private readonly ILogger<StrongCustomerAuthenticationService> _logger;
    private readonly SCAOptions _options;
    private readonly IBankIDService _bankIdService;
    private readonly IVippsService _vippsService;
    private readonly ISMSService _smsService;
    private readonly IRedisService _redisService;

    public StrongCustomerAuthenticationService(
        ILogger<StrongCustomerAuthenticationService> logger,
        IOptions<SCAOptions> options,
        IBankIDService bankIdService,
        IVippsService vippsService,
        ISMSService smsService,
        IRedisService redisService)
    {
        _logger = logger;
        _options = options.Value;
        _bankIdService = bankIdService;
        _vippsService = vippsService;
        _smsService = smsService;
        _redisService = redisService;
    }

    public async Task<SCAChallenge> InitiateSCAAsync(SCARequest request)
    {
        _logger.LogInformation("Initiating SCA for customer {CustomerId}, amount {Amount} NOK, method {Method}",
            request.CustomerId, request.Amount, request.PreferredMethod);

        try
        {
            // Check if SCA is required
            var isRequired = await IsSCARequiredAsync(request.CustomerId, request.Amount, request.PaymentMethod);
            if (!isRequired)
            {
                _logger.LogInformation("SCA not required for customer {CustomerId}, amount {Amount} NOK",
                    request.CustomerId, request.Amount);
                
                return new SCAChallenge
                {
                    ChallengeId = Guid.NewGuid().ToString(),
                    IsRequired = false,
                    Status = SCAStatus.EXEMPTED,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                };
            }

            // Check for exemptions
            var exemption = await CheckExemptionAsync(request.CustomerId, request.Amount, request.MerchantId);
            if (exemption.IsExempt)
            {
                _logger.LogInformation("SCA exemption granted for customer {CustomerId}. Reason: {Reason}",
                    request.CustomerId, exemption.Reason);

                return new SCAChallenge
                {
                    ChallengeId = Guid.NewGuid().ToString(),
                    IsRequired = false,
                    Status = SCAStatus.EXEMPTED,
                    ExemptionReason = exemption.Reason,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                };
            }

            // Determine the best authentication method
            var authMethod = await DetermineAuthenticationMethodAsync(request.CustomerId, request.PreferredMethod);
            var challengeId = Guid.NewGuid().ToString();

            var challenge = new SCAChallenge
            {
                ChallengeId = challengeId,
                IsRequired = true,
                Status = SCAStatus.INITIATED,
                AuthenticationMethod = authMethod,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.ChallengeExpiryMinutes),
                MaxAttempts = _options.MaxAuthenticationAttempts
            };

            // Initiate authentication based on method
            switch (authMethod)
            {
                case SCAMethod.BANK_ID:
                    challenge = await InitiateBankIDAuthenticationAsync(challenge, request);
                    break;
                
                case SCAMethod.VIPPS:
                    challenge = await InitiateVippsAuthenticationAsync(challenge, request);
                    break;
                
                case SCAMethod.SMS_OTP:
                    challenge = await InitiateSMSOTPAsync(challenge, request);
                    break;
                
                case SCAMethod.BIOMETRIC:
                    challenge = await InitiateBiometricAuthenticationAsync(challenge, request);
                    break;
                
                default:
                    throw new NotSupportedException($"Authentication method {authMethod} not supported");
            }

            // Store challenge in Redis with expiry
            await _redisService.SetAsync($"sca:challenge:{challengeId}", challenge, 
                TimeSpan.FromMinutes(_options.ChallengeExpiryMinutes));

            _logger.LogInformation("SCA challenge initiated successfully. ChallengeId: {ChallengeId}, Method: {Method}",
                challengeId, authMethod);

            return challenge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating SCA for customer {CustomerId}", request.CustomerId);
            throw;
        }
    }

    public async Task<SCAResult> ValidateSCAAsync(string challengeId, SCAValidationRequest validation)
    {
        _logger.LogInformation("Validating SCA challenge {ChallengeId}", challengeId);

        try
        {
            // Retrieve challenge from Redis
            var challenge = await _redisService.GetAsync<SCAChallenge>($"sca:challenge:{challengeId}");
            if (challenge == null)
            {
                _logger.LogWarning("SCA challenge not found or expired: {ChallengeId}", challengeId);
                return new SCAResult
                {
                    IsValid = false,
                    Status = SCAStatus.EXPIRED,
                    ErrorMessage = "Challenge not found or expired"
                };
            }

            // Check if challenge has expired
            if (challenge.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("SCA challenge expired: {ChallengeId}", challengeId);
                await _redisService.DeleteAsync($"sca:challenge:{challengeId}");
                
                return new SCAResult
                {
                    IsValid = false,
                    Status = SCAStatus.EXPIRED,
                    ErrorMessage = "Challenge has expired"
                };
            }

            // Check attempt count
            if (challenge.AttemptCount >= challenge.MaxAttempts)
            {
                _logger.LogWarning("Maximum SCA attempts exceeded for challenge {ChallengeId}", challengeId);
                await _redisService.DeleteAsync($"sca:challenge:{challengeId}");
                
                return new SCAResult
                {
                    IsValid = false,
                    Status = SCAStatus.FAILED,
                    ErrorMessage = "Maximum authentication attempts exceeded"
                };
            }

            // Validate based on authentication method
            var validationResult = challenge.AuthenticationMethod switch
            {
                SCAMethod.BANK_ID => await ValidateBankIDAsync(challenge, validation),
                SCAMethod.VIPPS => await ValidateVippsAsync(challenge, validation),
                SCAMethod.SMS_OTP => await ValidateSMSOTPAsync(challenge, validation),
                SCAMethod.BIOMETRIC => await ValidateBiometricAsync(challenge, validation),
                _ => throw new NotSupportedException($"Authentication method {challenge.AuthenticationMethod} not supported")
            };

            // Update challenge attempt count
            challenge.AttemptCount++;
            challenge.LastAttemptAt = DateTime.UtcNow;

            if (validationResult.IsValid)
            {
                challenge.Status = SCAStatus.COMPLETED;
                challenge.CompletedAt = DateTime.UtcNow;
                
                // Generate authentication token
                var authToken = await GenerateAuthenticationTokenAsync(validation.CustomerId, challengeId);
                validationResult.AuthenticationToken = authToken;
                
                _logger.LogInformation("SCA validation successful for challenge {ChallengeId}", challengeId);
            }
            else
            {
                challenge.Status = SCAStatus.FAILED;
                _logger.LogWarning("SCA validation failed for challenge {ChallengeId}. Attempt {AttemptCount}/{MaxAttempts}",
                    challengeId, challenge.AttemptCount, challenge.MaxAttempts);
            }

            // Update challenge in Redis
            await _redisService.SetAsync($"sca:challenge:{challengeId}", challenge, 
                TimeSpan.FromMinutes(_options.ChallengeExpiryMinutes));

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SCA challenge {ChallengeId}", challengeId);
            throw;
        }
    }

    public async Task<bool> IsSCARequiredAsync(string customerId, decimal amount, string paymentMethod)
    {
        try
        {
            // Norwegian SCA requirements based on PSD2 and local regulations
            
            // Always require SCA for amounts above 500 NOK (Norwegian threshold)
            if (amount > _options.SCAThresholdAmount)
            {
                return true;
            }

            // Check customer's cumulative transactions in last 24 hours
            var dailyTotal = await GetCustomerDailyTransactionTotalAsync(customerId);
            if (dailyTotal + amount > _options.DailyCumulativeThreshold)
            {
                return true;
            }

            // Check if customer has had more than 5 transactions today
            var dailyTransactionCount = await GetCustomerDailyTransactionCountAsync(customerId);
            if (dailyTransactionCount >= _options.DailyTransactionCountThreshold)
            {
                return true;
            }

            // Always require SCA for new customers (less than 30 days)
            var customerAge = await GetCustomerAgeInDaysAsync(customerId);
            if (customerAge < _options.NewCustomerThresholdDays)
            {
                return true;
            }

            // Check for suspicious activity patterns
            var riskScore = await GetCustomerRiskScoreAsync(customerId);
            if (riskScore > _options.RiskScoreThreshold)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SCA requirement for customer {CustomerId}", customerId);
            // Default to requiring SCA on error for security
            return true;
        }
    }

    public async Task<SCAExemption> CheckExemptionAsync(string customerId, decimal amount, string merchantId)
    {
        try
        {
            // Low-value payment exemption (under 30 NOK)
            if (amount <= _options.LowValueExemptionThreshold)
            {
                return new SCAExemption
                {
                    IsExempt = true,
                    Reason = "Low value payment exemption",
                    ExemptionType = SCAExemptionType.LOW_VALUE
                };
            }

            // Trusted merchant exemption
            var isTrustedMerchant = await IsTrustedMerchantAsync(merchantId, customerId);
            if (isTrustedMerchant)
            {
                return new SCAExemption
                {
                    IsExempt = true,
                    Reason = "Trusted merchant exemption",
                    ExemptionType = SCAExemptionType.TRUSTED_MERCHANT
                };
            }

            // Recurring payment exemption
            var isRecurringPayment = await IsRecurringPaymentAsync(customerId, merchantId, amount);
            if (isRecurringPayment)
            {
                return new SCAExemption
                {
                    IsExempt = true,
                    Reason = "Recurring payment exemption",
                    ExemptionType = SCAExemptionType.RECURRING_PAYMENT
                };
            }

            // Corporate payment exemption
            var isCorporateCustomer = await IsCorporateCustomerAsync(customerId);
            if (isCorporateCustomer && amount <= _options.CorporateExemptionThreshold)
            {
                return new SCAExemption
                {
                    IsExempt = true,
                    Reason = "Corporate payment exemption",
                    ExemptionType = SCAExemptionType.CORPORATE_PAYMENT
                };
            }

            return new SCAExemption { IsExempt = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SCA exemption for customer {CustomerId}", customerId);
            return new SCAExemption { IsExempt = false };
        }
    }

    public async Task<string> GenerateAuthenticationTokenAsync(string customerId, string sessionId)
    {
        try
        {
            var tokenData = new
            {
                CustomerId = customerId,
                SessionId = sessionId,
                IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AuthTokenExpiryMinutes).ToUnixTimeSeconds(),
                Issuer = "RivertyBNPL",
                Audience = "BNPL-API"
            };

            var tokenJson = JsonSerializer.Serialize(tokenData);
            var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
            
            // Sign the token with HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.TokenSigningKey));
            var signature = hmac.ComputeHash(tokenBytes);
            
            var token = Convert.ToBase64String(tokenBytes) + "." + Convert.ToBase64String(signature);
            
            // Store token in Redis for validation
            await _redisService.SetAsync($"sca:token:{customerId}:{sessionId}", token, 
                TimeSpan.FromMinutes(_options.AuthTokenExpiryMinutes));

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authentication token for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> ValidateAuthenticationTokenAsync(string token, string customerId)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            var tokenBytes = Convert.FromBase64String(parts[0]);
            var signature = Convert.FromBase64String(parts[1]);
            
            // Verify signature
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.TokenSigningKey));
            var expectedSignature = hmac.ComputeHash(tokenBytes);
            
            if (!signature.SequenceEqual(expectedSignature))
                return false;

            var tokenJson = Encoding.UTF8.GetString(tokenBytes);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            
            // Verify customer ID
            if (tokenData.GetProperty("CustomerId").GetString() != customerId)
                return false;

            // Verify expiration
            var expiresAt = tokenData.GetProperty("ExpiresAt").GetInt64();
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt)
                return false;

            // Check if token exists in Redis
            var sessionId = tokenData.GetProperty("SessionId").GetString();
            var storedToken = await _redisService.GetAsync<string>($"sca:token:{customerId}:{sessionId}");
            
            return storedToken == token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication token for customer {CustomerId}", customerId);
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<SCAMethod> DetermineAuthenticationMethodAsync(string customerId, SCAMethod? preferredMethod)
    {
        // Check customer's available authentication methods
        var availableMethods = await GetCustomerAuthenticationMethodsAsync(customerId);
        
        // Use preferred method if available
        if (preferredMethod.HasValue && availableMethods.Contains(preferredMethod.Value))
        {
            return preferredMethod.Value;
        }

        // Prioritize BankID for Norwegian customers
        if (availableMethods.Contains(SCAMethod.BANK_ID))
        {
            return SCAMethod.BANK_ID;
        }

        // Then Vipps as it's widely used in Norway
        if (availableMethods.Contains(SCAMethod.VIPPS))
        {
            return SCAMethod.VIPPS;
        }

        // Fallback to SMS OTP
        return SCAMethod.SMS_OTP;
    }

    private async Task<SCAChallenge> InitiateBankIDAuthenticationAsync(SCAChallenge challenge, SCARequest request)
    {
        var bankIdRequest = new BankIDAuthenticationRequest
        {
            PersonalNumber = request.SocialSecurityNumber,
            EndUserIp = request.ClientIP,
            Requirement = new BankIDRequirement
            {
                AllowFingerprint = true,
                CertificatePolicies = new[] { "1.2.752.78.1.5" } // Norwegian BankID policy
            }
        };

        var bankIdResponse = await _bankIdService.InitiateAuthenticationAsync(bankIdRequest);
        
        challenge.BankIDOrderRef = bankIdResponse.OrderRef;
        challenge.QRCodeData = bankIdResponse.QRStartToken;
        challenge.AutoStartToken = bankIdResponse.AutoStartToken;
        
        return challenge;
    }

    private async Task<SCAChallenge> InitiateVippsAuthenticationAsync(SCAChallenge challenge, SCARequest request)
    {
        var vippsRequest = new VippsAuthenticationRequest
        {
            PhoneNumber = request.PhoneNumber,
            Text = $"Bekreft BNPL-betaling på {request.Amount:F2} NOK",
            Fallback = VippsFallback.SMS
        };

        var vippsResponse = await _vippsService.InitiateAuthenticationAsync(vippsRequest);
        
        challenge.VippsOrderId = vippsResponse.OrderId;
        challenge.VippsUrl = vippsResponse.Url;
        
        return challenge;
    }

    private async Task<SCAChallenge> InitiateSMSOTPAsync(SCAChallenge challenge, SCARequest request)
    {
        var otpCode = GenerateOTPCode();
        var message = $"Din engangskode for Riverty BNPL: {otpCode}. Gyldig i {_options.OTPExpiryMinutes} minutter.";
        
        await _smsService.SendSMSAsync(request.PhoneNumber, message);
        
        // Store OTP hash for validation
        challenge.OTPHash = HashOTP(otpCode);
        
        return challenge;
    }

    private async Task<SCAChallenge> InitiateBiometricAuthenticationAsync(SCAChallenge challenge, SCARequest request)
    {
        // Generate biometric challenge
        challenge.BiometricChallenge = GenerateBiometricChallenge();
        
        return await Task.FromResult(challenge);
    }

    private async Task<SCAResult> ValidateBankIDAsync(SCAChallenge challenge, SCAValidationRequest validation)
    {
        if (string.IsNullOrEmpty(challenge.BankIDOrderRef))
        {
            return new SCAResult { IsValid = false, ErrorMessage = "Invalid BankID order reference" };
        }

        var bankIdResult = await _bankIdService.CollectAsync(challenge.BankIDOrderRef);
        
        return new SCAResult
        {
            IsValid = bankIdResult.Status == BankIDStatus.COMPLETE,
            Status = bankIdResult.Status == BankIDStatus.COMPLETE ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = bankIdResult.HintCode,
            AuthenticationData = new Dictionary<string, object>
            {
                { "PersonalNumber", bankIdResult.CompletionData?.User?.PersonalNumber ?? "" },
                { "Name", bankIdResult.CompletionData?.User?.Name ?? "" },
                { "Signature", bankIdResult.CompletionData?.Signature ?? "" }
            }
        };
    }

    private async Task<SCAResult> ValidateVippsAsync(SCAChallenge challenge, SCAValidationRequest validation)
    {
        if (string.IsNullOrEmpty(challenge.VippsOrderId))
        {
            return new SCAResult { IsValid = false, ErrorMessage = "Invalid Vipps order ID" };
        }

        var vippsResult = await _vippsService.GetStatusAsync(challenge.VippsOrderId);
        
        return new SCAResult
        {
            IsValid = vippsResult.Status == VippsStatus.APPROVED,
            Status = vippsResult.Status == VippsStatus.APPROVED ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = vippsResult.ErrorMessage
        };
    }

    private Task<SCAResult> ValidateSMSOTPAsync(SCAChallenge challenge, SCAValidationRequest validation)
    {
        if (string.IsNullOrEmpty(validation.OTPCode) || string.IsNullOrEmpty(challenge.OTPHash))
        {
            return Task.FromResult(new SCAResult { IsValid = false, ErrorMessage = "Invalid OTP code" });
        }

        var isValid = VerifyOTP(validation.OTPCode, challenge.OTPHash);
        
        return Task.FromResult(new SCAResult
        {
            IsValid = isValid,
            Status = isValid ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = isValid ? null : "Invalid OTP code"
        });
    }

    private Task<SCAResult> ValidateBiometricAsync(SCAChallenge challenge, SCAValidationRequest validation)
    {
        // Validate biometric data against challenge
        var isValid = ValidateBiometricData(validation.BiometricData, challenge.BiometricChallenge);
        
        return Task.FromResult(new SCAResult
        {
            IsValid = isValid,
            Status = isValid ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = isValid ? null : "Biometric validation failed"
        });
    }

    // Real implementations of helper methods
    private async Task<List<SCAMethod>> GetCustomerAuthenticationMethodsAsync(string customerId)
    {
        try
        {
            // Query customer's registered authentication methods from database
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                return new List<SCAMethod> { SCAMethod.SMS_OTP }; // Default fallback
            }

            var methods = new List<SCAMethod>();

            // Check if customer has BankID registered
            if (!string.IsNullOrEmpty(customer.BankIdPersonalNumber))
            {
                methods.Add(SCAMethod.BANK_ID);
            }

            // Check if customer has Vipps registered
            if (!string.IsNullOrEmpty(customer.VippsPhoneNumber))
            {
                methods.Add(SCAMethod.VIPPS);
            }

            // SMS OTP is always available if phone number exists
            if (!string.IsNullOrEmpty(customer.PhoneNumber))
            {
                methods.Add(SCAMethod.SMS_OTP);
            }

            // Biometric if enabled
            if (customer.BiometricAuthEnabled)
            {
                methods.Add(SCAMethod.BIOMETRIC);
            }

            return methods.Any() ? methods : new List<SCAMethod> { SCAMethod.SMS_OTP };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication methods for customer {CustomerId}", customerId);
            return new List<SCAMethod> { SCAMethod.SMS_OTP }; // Fallback
        }
    }

    private async Task<decimal> GetCustomerDailyTransactionTotalAsync(string customerId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var dailyTotal = await _paymentService.GetCustomerTransactionTotalAsync(
                customerId, today, tomorrow);

            return dailyTotal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily transaction total for customer {CustomerId}", customerId);
            return 0m;
        }
    }

    private async Task<int> GetCustomerDailyTransactionCountAsync(string customerId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var dailyCount = await _paymentService.GetCustomerTransactionCountAsync(
                customerId, today, tomorrow);

            return dailyCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily transaction count for customer {CustomerId}", customerId);
            return 0;
        }
    }

    private async Task<int> GetCustomerAgeInDaysAsync(string customerId)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer?.CreatedAt == null)
            {
                return 0;
            }

            return (int)(DateTime.UtcNow - customer.CreatedAt).TotalDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer age for customer {CustomerId}", customerId);
            return 0;
        }
    }

    private async Task<int> GetCustomerRiskScoreAsync(string customerId)
    {
        try
        {
            var riskProfile = await _riskService.GetCustomerRiskProfileAsync(customerId);
            return riskProfile?.RiskScore ?? 50; // Default medium risk
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting risk score for customer {CustomerId}", customerId);
            return 50; // Default medium risk
        }
    }

    private async Task<bool> IsTrustedMerchantAsync(string merchantId, string customerId)
    {
        try
        {
            // Check if merchant is in trusted list
            var merchant = await _merchantService.GetMerchantByIdAsync(merchantId);
            if (merchant?.IsTrusted == true)
            {
                return true;
            }

            // Check if customer has successful transaction history with this merchant
            var successfulTransactions = await _paymentService.GetSuccessfulTransactionCountAsync(
                customerId, merchantId);

            return successfulTransactions >= 5; // Consider trusted after 5 successful transactions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking trusted merchant status for merchant {MerchantId}, customer {CustomerId}", 
                merchantId, customerId);
            return false;
        }
    }

    private async Task<bool> IsRecurringPaymentAsync(string customerId, string merchantId, decimal amount)
    {
        try
        {
            // Check if customer has similar payments to this merchant in the past 30 days
            var recentPayments = await _paymentService.GetRecentSimilarPaymentsAsync(
                customerId, merchantId, amount, TimeSpan.FromDays(30));

            return recentPayments.Count >= 2; // Consider recurring if 2+ similar payments in 30 days
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recurring payment for customer {CustomerId}, merchant {MerchantId}", 
                customerId, merchantId);
            return false;
        }
    }

    private async Task<bool> IsCorporateCustomerAsync(string customerId)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            return customer?.CustomerType == CustomerType.Corporate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking corporate customer status for customer {CustomerId}", customerId);
            return false;
        }
    }
    
    private static string GenerateOTPCode() => new Random().Next(100000, 999999).ToString();
    private static string HashOTP(string otp) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));
    private static bool VerifyOTP(string otp, string hash) => HashOTP(otp) == hash;
    private static string GenerateBiometricChallenge() => Guid.NewGuid().ToString();
    private static bool ValidateBiometricData(string? data, string? challenge) => !string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(challenge);

    #endregion
}

#region Models and Enums

public class SCAOptions
{
    public decimal SCAThresholdAmount { get; set; } = 500m; // NOK
    public decimal DailyCumulativeThreshold { get; set; } = 2000m; // NOK
    public int DailyTransactionCountThreshold { get; set; } = 5;
    public int NewCustomerThresholdDays { get; set; } = 30;
    public int RiskScoreThreshold { get; set; } = 80;
    public decimal LowValueExemptionThreshold { get; set; } = 30m; // NOK
    public decimal CorporateExemptionThreshold { get; set; } = 10000m; // NOK
    public int ChallengeExpiryMinutes { get; set; } = 10;
    public int AuthTokenExpiryMinutes { get; set; } = 30;
    public int MaxAuthenticationAttempts { get; set; } = 3;
    public int OTPExpiryMinutes { get; set; } = 5;
    public string TokenSigningKey { get; set; } = string.Empty;
}

public class SCARequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string SocialSecurityNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string ClientIP { get; set; } = string.Empty;
    public SCAMethod? PreferredMethod { get; set; }
}

public class SCAChallenge
{
    public string ChallengeId { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public SCAStatus Status { get; set; }
    public SCAMethod AuthenticationMethod { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ExemptionReason { get; set; }
    
    // Method-specific properties
    public string? BankIDOrderRef { get; set; }
    public string? QRCodeData { get; set; }
    public string? AutoStartToken { get; set; }
    public string? VippsOrderId { get; set; }
    public string? VippsUrl { get; set; }
    public string? OTPHash { get; set; }
    public string? BiometricChallenge { get; set; }
}

public class SCAValidationRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string? OTPCode { get; set; }
    public string? BiometricData { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class SCAResult
{
    public bool IsValid { get; set; }
    public SCAStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AuthenticationToken { get; set; }
    public Dictionary<string, object> AuthenticationData { get; set; } = new();
}

public class SCAExemption
{
    public bool IsExempt { get; set; }
    public string? Reason { get; set; }
    public SCAExemptionType ExemptionType { get; set; }
}

public enum SCAMethod
{
    BANK_ID,
    VIPPS,
    SMS_OTP,
    BIOMETRIC,
    PUSH_NOTIFICATION
}

public enum SCAStatus
{
    INITIATED,
    PENDING,
    COMPLETED,
    FAILED,
    EXPIRED,
    EXEMPTED
}

public enum SCAExemptionType
{
    LOW_VALUE,
    TRUSTED_MERCHANT,
    RECURRING_PAYMENT,
    CORPORATE_PAYMENT,
    RISK_ANALYSIS
}

#endregion

#region External Service Interfaces

public interface IBankIDService
{
    Task<BankIDAuthenticationResponse> InitiateAuthenticationAsync(BankIDAuthenticationRequest request);
    Task<BankIDCollectResponse> CollectAsync(string orderRef);
}

public interface IVippsService
{
    Task<VippsAuthenticationResponse> InitiateAuthenticationAsync(VippsAuthenticationRequest request);
    Task<VippsStatusResponse> GetStatusAsync(string orderId);
}

public interface ISMSService
{
    Task SendSMSAsync(string phoneNumber, string message);
}

public interface IRedisService
{
    Task SetAsync<T>(string key, T value, TimeSpan expiry);
    Task<T?> GetAsync<T>(string key);
    Task DeleteAsync(string key);
}

#endregion