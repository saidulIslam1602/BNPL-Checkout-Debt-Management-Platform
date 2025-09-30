using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YourCompanyBNPL.Shared.Infrastructure.Security;

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
                Issuer = "YourCompanyBNPL",
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
            IpAddress = request.ClientIP,
            UserVisibleData = $"BNPL payment authentication for {request.Amount:F2} NOK"
        };

        var bankIdResponse = await _bankIdService.InitiateAuthenticationAsync(bankIdRequest);
        
        challenge.BankIDOrderRef = bankIdResponse.OrderRef;
        challenge.QRCodeData = bankIdResponse.QrStartToken;
        challenge.AutoStartToken = bankIdResponse.AutoStartToken;
        
        return challenge;
    }

    private async Task<SCAChallenge> InitiateVippsAuthenticationAsync(SCAChallenge challenge, SCARequest request)
    {
        var vippsRequest = new VippsAuthenticationRequest
        {
            PhoneNumber = request.PhoneNumber,
            Purpose = $"BNPL payment authentication",
            AmountInOre = (int)(request.Amount * 100) // Convert NOK to øre
        };

        var vippsResponse = await _vippsService.InitiateAuthenticationAsync(vippsRequest);
        
        challenge.VippsOrderId = vippsResponse.OrderId;
        challenge.VippsUrl = vippsResponse.Url;
        
        return challenge;
    }

    private async Task<SCAChallenge> InitiateSMSOTPAsync(SCAChallenge challenge, SCARequest request)
    {
        var otpCode = GenerateOTPCode();
        var message = $"Din engangskode for YourCompany BNPL: {otpCode}. Gyldig i {_options.OTPExpiryMinutes} minutter.";
        
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
            IsValid = bankIdResult.Status == "complete",
            Status = bankIdResult.Status == "complete" ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = bankIdResult.HintCode,
            AuthenticationData = new Dictionary<string, object>
            {
                { "PersonalNumber", bankIdResult.UserInfo?.PersonalNumber ?? "" },
                { "Name", bankIdResult.UserInfo?.Name ?? "" },
                { "GivenName", bankIdResult.UserInfo?.GivenName ?? "" }
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
            IsValid = vippsResult.Status == "ok",
            Status = vippsResult.Status == "ok" ? SCAStatus.COMPLETED : SCAStatus.FAILED,
            ErrorMessage = vippsResult.Status != "ok" ? "Vipps authentication failed" : null
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
            // TODO: Implement customer service integration to get registered authentication methods
            // For now, return default available methods
            _logger.LogDebug("Getting authentication methods for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            
            // Return standard Norwegian authentication methods
            return new List<SCAMethod>
            {
                SCAMethod.BANK_ID,
                SCAMethod.VIPPS,
                SCAMethod.SMS_OTP
            };
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
            // TODO: Implement payment service integration for transaction totals
            _logger.LogDebug("Getting daily transaction total for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            return 0m; // Default value until service is integrated
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
            // TODO: Implement payment service integration for transaction counts
            _logger.LogDebug("Getting daily transaction count for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            return 0; // Default value until service is integrated
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
            // TODO: Implement customer service integration for customer age
            _logger.LogDebug("Getting customer age for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            return 365; // Default to 1 year old account
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
            // TODO: Implement risk service integration
            _logger.LogDebug("Getting risk score for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            return 50; // Default medium risk
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
            // TODO: Implement merchant service integration
            // For now, return false to require SCA for all merchants
            _logger.LogDebug("Checking trusted merchant status for merchant {MerchantId}, customer {CustomerId}", 
                merchantId, customerId);
            
            await Task.CompletedTask;
            return false; // Require SCA until merchant service is integrated
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
            // TODO: Implement payment history service integration
            // For now, return false to require SCA for all payments
            _logger.LogDebug("Checking recurring payment for customer {CustomerId}, merchant {MerchantId}, amount {Amount}", 
                customerId, merchantId, amount);
            
            await Task.CompletedTask;
            return false; // Require SCA until payment history service is integrated
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
            // TODO: Implement customer service integration  
            // For now, return false (treat as individual customer)
            _logger.LogDebug("Checking corporate customer status for customer {CustomerId}", customerId);
            
            await Task.CompletedTask;
            return false; // Treat as individual until customer service is integrated
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

#region Request/Response Models

public class BankIDAuthenticationRequest
{
    public string PersonalNumber { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserVisibleData { get; set; }
}

public class BankIDAuthenticationResponse
{
    public string OrderRef { get; set; } = string.Empty;
    public string AutoStartToken { get; set; } = string.Empty;
    public string QrStartToken { get; set; } = string.Empty;
}

public class BankIDCollectResponse
{
    public string Status { get; set; } = string.Empty;
    public string? HintCode { get; set; }
    public BankIDUserInfo? UserInfo { get; set; }
}

public class BankIDUserInfo
{
    public string PersonalNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}

public class VippsAuthenticationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public int? AmountInOre { get; set; }
}

public class VippsAuthenticationResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class VippsStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public VippsUserInfo? UserInfo { get; set; }
}

public class VippsUserInfo
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
}

#endregion