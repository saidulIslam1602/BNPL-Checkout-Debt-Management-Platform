using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for payment method tokenization and secure storage
/// </summary>
public interface IPaymentTokenizationService
{
    Task<ApiResponse<PaymentTokenResponse>> TokenizePaymentMethodAsync(TokenizePaymentMethodRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentMethodDetails>> GetPaymentMethodAsync(string token, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeletePaymentMethodAsync(string token, CancellationToken cancellationToken = default);
    Task<PagedApiResponse<PaymentTokenResponse>> GetCustomerPaymentMethodsAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> ProcessTokenizedPaymentAsync(ProcessTokenizedPaymentRequest request, CancellationToken cancellationToken = default);
}

public class PaymentTokenizationService : IPaymentTokenizationService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentTokenizationService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentTokenizationService(
        PaymentDbContext context,
        ILogger<PaymentTokenizationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<PaymentTokenResponse>> TokenizePaymentMethodAsync(TokenizePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Tokenizing payment method for customer {CustomerId}", request.CustomerId);

            // Validate customer exists
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return ApiResponse<PaymentTokenResponse>.ErrorResult("Customer not found", 404);
            }

            // Check if payment method already exists
            var existingToken = await FindExistingPaymentMethodAsync(request, cancellationToken);
            if (existingToken != null)
            {
                var existingResponse = new PaymentTokenResponse
                {
                    Token = existingToken.Token,
                    PaymentMethod = existingToken.PaymentMethod,
                    MaskedDetails = existingToken.MaskedDetails,
                    ExpiresAt = existingToken.ExpiresAt,
                    IsDefault = existingToken.IsDefault
                };

                return ApiResponse<PaymentTokenResponse>.SuccessResult(existingResponse, "Existing payment method returned");
            }

            // Generate secure token
            var token = GenerateSecureToken();

            // Encrypt sensitive payment data
            var encryptedData = await EncryptPaymentDataAsync(request.PaymentData);

            // Create payment token record
            var paymentToken = new PaymentToken
            {
                Token = token,
                CustomerId = request.CustomerId,
                PaymentMethod = request.PaymentMethod,
                EncryptedData = encryptedData,
                MaskedDetails = GenerateMaskedDetails(request.PaymentMethod, request.PaymentData),
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddYears(2),
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = null
            };

            // If this is set as default, unset other default methods
            if (request.IsDefault)
            {
                await UnsetDefaultPaymentMethodsAsync(request.CustomerId, cancellationToken);
            }

            _context.PaymentTokens.Add(paymentToken);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new PaymentTokenResponse
            {
                Token = paymentToken.Token,
                PaymentMethod = paymentToken.PaymentMethod,
                MaskedDetails = paymentToken.MaskedDetails,
                ExpiresAt = paymentToken.ExpiresAt,
                IsDefault = paymentToken.IsDefault
            };

            _logger.LogInformation("Payment method tokenized successfully for customer {CustomerId}", request.CustomerId);

            return ApiResponse<PaymentTokenResponse>.SuccessResult(response, "Payment method tokenized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tokenizing payment method for customer {CustomerId}", request.CustomerId);
            return ApiResponse<PaymentTokenResponse>.ErrorResult("Failed to tokenize payment method", 500);
        }
    }

    public async Task<ApiResponse<PaymentMethodDetails>> GetPaymentMethodAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentToken = await _context.PaymentTokens
                .FirstOrDefaultAsync(pt => pt.Token == token && pt.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (paymentToken == null)
            {
                return ApiResponse<PaymentMethodDetails>.ErrorResult("Payment method not found or expired", 404);
            }

            // Decrypt payment data
            var decryptedData = await DecryptPaymentDataAsync(paymentToken.EncryptedData);

            var response = new PaymentMethodDetails
            {
                Token = paymentToken.Token,
                PaymentMethod = paymentToken.PaymentMethod,
                MaskedDetails = paymentToken.MaskedDetails,
                PaymentData = decryptedData,
                ExpiresAt = paymentToken.ExpiresAt,
                IsDefault = paymentToken.IsDefault,
                LastUsedAt = paymentToken.LastUsedAt
            };

            return ApiResponse<PaymentMethodDetails>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method for token {Token}", token);
            return ApiResponse<PaymentMethodDetails>.ErrorResult("Failed to retrieve payment method", 500);
        }
    }

    public async Task<ApiResponse> DeletePaymentMethodAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentToken = await _context.PaymentTokens
                .FirstOrDefaultAsync(pt => pt.Token == token, cancellationToken);

            if (paymentToken == null)
            {
                return ApiResponse.ErrorResult("Payment method not found", 404);
            }

            _context.PaymentTokens.Remove(paymentToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment method token {Token} deleted successfully", token);

            return ApiResponse.SuccessResponse("Payment method deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method token {Token}", token);
            return ApiResponse.ErrorResult("Failed to delete payment method", 500);
        }
    }

    public async Task<PagedApiResponse<PaymentTokenResponse>> GetCustomerPaymentMethodsAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PaymentTokens
                .Where(pt => pt.CustomerId == customerId && pt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(pt => pt.IsDefault)
                .ThenByDescending(pt => pt.LastUsedAt)
                .ThenByDescending(pt => pt.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var tokens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = tokens.Select(pt => new PaymentTokenResponse
            {
                Token = pt.Token,
                PaymentMethod = pt.PaymentMethod,
                MaskedDetails = pt.MaskedDetails,
                ExpiresAt = pt.ExpiresAt,
                IsDefault = pt.IsDefault,
                LastUsedAt = pt.LastUsedAt
            }).ToList();

            return PagedApiResponse<PaymentTokenResponse>.SuccessResult(responses, page, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods for customer {CustomerId}", customerId);
            return new PagedApiResponse<PaymentTokenResponse>
            {
                Success = false,
                Errors = new List<string> { "Failed to retrieve payment methods" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse<PaymentResponse>> ProcessTokenizedPaymentAsync(ProcessTokenizedPaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing tokenized payment with token {Token}", request.PaymentToken);

            // Get payment method details
            var paymentMethodResult = await GetPaymentMethodAsync(request.PaymentToken, cancellationToken);
            if (!paymentMethodResult.Success || paymentMethodResult.Data == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Invalid payment token", 400);
            }

            var paymentMethod = paymentMethodResult.Data;

            // Update last used timestamp
            var paymentToken = await _context.PaymentTokens
                .FirstOrDefaultAsync(pt => pt.Token == request.PaymentToken, cancellationToken);

            if (paymentToken != null)
            {
                paymentToken.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Create payment request with decrypted payment data
            var paymentRequest = new CreatePaymentRequest
            {
                CustomerId = paymentToken!.CustomerId,
                MerchantId = request.MerchantId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = paymentMethod.PaymentMethod,
                OrderReference = request.OrderReference,
                Description = request.Description,
                Metadata = request.Metadata
            };

            // Process payment using existing payment service
            // This would integrate with your existing PaymentService
            // For now, we'll simulate the payment processing

            var payment = new Models.Payment
            {
                CustomerId = paymentRequest.CustomerId,
                MerchantId = paymentRequest.MerchantId,
                Amount = paymentRequest.Amount,
                Currency = paymentRequest.Currency,
                PaymentMethod = paymentRequest.PaymentMethod,
                OrderReference = paymentRequest.OrderReference,
                Description = paymentRequest.Description,
                Status = PaymentStatus.Pending,
                TransactionId = GenerateTransactionId(),
                PaymentTokenId = paymentToken.Id
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new PaymentResponse
            {
                Id = payment.Id,
                CustomerId = payment.CustomerId,
                MerchantId = payment.MerchantId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                OrderReference = payment.OrderReference,
                Description = payment.Description,
                CreatedAt = payment.CreatedAt
            };

            return ApiResponse<PaymentResponse>.SuccessResult(response, "Tokenized payment processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tokenized payment");
            return ApiResponse<PaymentResponse>.ErrorResult("Failed to process tokenized payment", 500);
        }
    }

    #region Private Methods

    private async Task<PaymentToken?> FindExistingPaymentMethodAsync(TokenizePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        // For credit/debit cards, check by last 4 digits and expiry
        if (request.PaymentMethod == PaymentMethod.CreditCard || request.PaymentMethod == PaymentMethod.DebitCard)
        {
            if (request.PaymentData.TryGetValue("last4", out var last4) && 
                request.PaymentData.TryGetValue("expiry", out var expiry))
            {
                var maskedPattern = $"****-****-****-{last4}";
                return await _context.PaymentTokens
                    .FirstOrDefaultAsync(pt => pt.CustomerId == request.CustomerId &&
                                             pt.PaymentMethod == request.PaymentMethod &&
                                             pt.MaskedDetails.Contains(last4.ToString()!) &&
                                             pt.MaskedDetails.Contains(expiry.ToString()!) &&
                                             pt.ExpiresAt > DateTime.UtcNow, cancellationToken);
            }
        }

        return null;
    }

    private async Task UnsetDefaultPaymentMethodsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var defaultMethods = await _context.PaymentTokens
            .Where(pt => pt.CustomerId == customerId && pt.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var method in defaultMethods)
        {
            method.IsDefault = false;
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return $"pm_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private async Task<string> EncryptPaymentDataAsync(Dictionary<string, object> paymentData)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(paymentData);
            var key = _configuration["Encryption:Key"] ?? "default-encryption-key-32-chars!!";
            
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            await swEncrypt.WriteAsync(json);
            swEncrypt.Close();

            var iv = aes.IV;
            var encrypted = msEncrypt.ToArray();
            var result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting payment data");
            throw;
        }
    }

    private async Task<Dictionary<string, object>> DecryptPaymentDataAsync(string encryptedData)
    {
        try
        {
            var key = _configuration["Encryption:Key"] ?? "default-encryption-key-32-chars!!";
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var json = await srDecrypt.ReadToEndAsync();
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting payment data");
            throw;
        }
    }

    private static string GenerateMaskedDetails(PaymentMethod paymentMethod, Dictionary<string, object> paymentData)
    {
        return paymentMethod switch
        {
            PaymentMethod.CreditCard or PaymentMethod.DebitCard => GenerateCardMaskedDetails(paymentData),
            PaymentMethod.BankTransfer => GenerateBankMaskedDetails(paymentData),
            _ => "****"
        };
    }

    private static string GenerateCardMaskedDetails(Dictionary<string, object> paymentData)
    {
        if (paymentData.TryGetValue("last4", out var last4) && 
            paymentData.TryGetValue("expiry", out var expiry) &&
            paymentData.TryGetValue("brand", out var brand))
        {
            return $"{brand} ****-****-****-{last4} {expiry}";
        }

        return "****-****-****-****";
    }

    private static string GenerateBankMaskedDetails(Dictionary<string, object> paymentData)
    {
        if (paymentData.TryGetValue("account_last4", out var last4) && 
            paymentData.TryGetValue("bank_name", out var bankName))
        {
            return $"{bankName} ****{last4}";
        }

        return "Bank Account ****";
    }

    private static string GenerateTransactionId()
    {
        return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    #endregion
}

// Supporting DTOs
public class TokenizePaymentMethodRequest
{
    public Guid CustomerId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Dictionary<string, object> PaymentData { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public bool IsDefault { get; set; }
}

public class PaymentTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string MaskedDetails { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class PaymentMethodDetails
{
    public string Token { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string MaskedDetails { get; set; } = string.Empty;
    public Dictionary<string, object> PaymentData { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class ProcessTokenizedPaymentRequest
{
    public string PaymentToken { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public string? OrderReference { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}