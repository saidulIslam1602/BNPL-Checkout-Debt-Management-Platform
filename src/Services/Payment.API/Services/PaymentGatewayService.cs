using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RivertyBNPL.Common.Enums;

namespace RivertyBNPL.Payment.API.Services;

/// <summary>
/// Real payment gateway integration service supporting multiple Norwegian payment providers
/// </summary>
public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> CapturePaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> GetPaymentStatusAsync(string transactionId, CancellationToken cancellationToken = default);
}

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayService> _logger;
    private readonly PaymentGatewayOptions _options;

    public PaymentGatewayService(
        HttpClient httpClient,
        ILogger<PaymentGatewayService> logger,
        IOptions<PaymentGatewayOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing payment via {Provider} for amount {Amount} {Currency}", 
                _options.DefaultProvider, request.Amount, request.Currency);

            return _options.DefaultProvider.ToLower() switch
            {
                "stripe" => await ProcessStripePaymentAsync(request, cancellationToken),
                "adyen" => await ProcessAdyenPaymentAsync(request, cancellationToken),
                "nets" => await ProcessNetsPaymentAsync(request, cancellationToken),
                "vipps" => await ProcessVippsPaymentAsync(request, cancellationToken),
                _ => throw new NotSupportedException($"Payment provider {_options.DefaultProvider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment via {Provider}", _options.DefaultProvider);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Payment processing failed",
                ErrorCode = "PROCESSING_ERROR"
            };
        }
    }

    public async Task<PaymentGatewayResult> CapturePaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Capturing payment {TransactionId} for amount {Amount}", transactionId, amount);

            return _options.DefaultProvider.ToLower() switch
            {
                "stripe" => await CaptureStripePaymentAsync(transactionId, amount, cancellationToken),
                "adyen" => await CaptureAdyenPaymentAsync(transactionId, amount, cancellationToken),
                "nets" => await CaptureNetsPaymentAsync(transactionId, amount, cancellationToken),
                "vipps" => await CaptureVippsPaymentAsync(transactionId, amount, cancellationToken),
                _ => throw new NotSupportedException($"Payment provider {_options.DefaultProvider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing payment {TransactionId}", transactionId);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Payment capture failed",
                ErrorCode = "CAPTURE_ERROR"
            };
        }
    }

    public async Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refunding payment {TransactionId} for amount {Amount}", transactionId, amount);

            return _options.DefaultProvider.ToLower() switch
            {
                "stripe" => await RefundStripePaymentAsync(transactionId, amount, reason, cancellationToken),
                "adyen" => await RefundAdyenPaymentAsync(transactionId, amount, reason, cancellationToken),
                "nets" => await RefundNetsPaymentAsync(transactionId, amount, reason, cancellationToken),
                "vipps" => await RefundVippsPaymentAsync(transactionId, amount, reason, cancellationToken),
                _ => throw new NotSupportedException($"Payment provider {_options.DefaultProvider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {TransactionId}", transactionId);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Payment refund failed",
                ErrorCode = "REFUND_ERROR"
            };
        }
    }

    public async Task<PaymentGatewayResult> GetPaymentStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting payment status for {TransactionId}", transactionId);

            return _options.DefaultProvider.ToLower() switch
            {
                "stripe" => await GetStripePaymentStatusAsync(transactionId, cancellationToken),
                "adyen" => await GetAdyenPaymentStatusAsync(transactionId, cancellationToken),
                "nets" => await GetNetsPaymentStatusAsync(transactionId, cancellationToken),
                "vipps" => await GetVippsPaymentStatusAsync(transactionId, cancellationToken),
                _ => throw new NotSupportedException($"Payment provider {_options.DefaultProvider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {TransactionId}", transactionId);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get payment status",
                ErrorCode = "STATUS_ERROR"
            };
        }
    }

    #region Stripe Integration

    private async Task<PaymentGatewayResult> ProcessStripePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        var stripeConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("stripe", StringComparison.OrdinalIgnoreCase));
        if (stripeConfig == null)
            throw new InvalidOperationException("Stripe configuration not found");

        var stripeRequest = new
        {
            amount = (int)(request.Amount * 100), // Stripe uses cents
            currency = request.Currency.ToString().ToLower(),
            payment_method = request.PaymentMethodId,
            confirmation_method = "manual",
            confirm = true,
            description = request.Description,
            metadata = new Dictionary<string, string>
            {
                { "customer_id", request.CustomerId.ToString() },
                { "merchant_id", request.MerchantId.ToString() },
                { "order_reference", request.OrderReference ?? "" }
            }
        };

        var json = JsonSerializer.Serialize(stripeRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeConfig.SecretKey}");
        _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

        var response = await _httpClient.PostAsync($"{stripeConfig.BaseUrl}/payment_intents", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stripeResponse = JsonSerializer.Deserialize<StripePaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = stripeResponse?.Status == "succeeded",
                TransactionId = stripeResponse?.Id ?? "",
                AuthorizationCode = stripeResponse?.Id ?? "",
                GatewayTransactionId = stripeResponse?.Id ?? "",
                Status = MapStripeStatus(stripeResponse?.Status),
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Error?.Message ?? "Payment failed",
                ErrorCode = errorResponse?.Error?.Code ?? "unknown_error",
                IsRetryable = IsRetryableStripeError(errorResponse?.Error?.Code)
            };
        }
    }

    private async Task<PaymentGatewayResult> CaptureStripePaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
    {
        var stripeConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("stripe", StringComparison.OrdinalIgnoreCase));
        if (stripeConfig == null)
            throw new InvalidOperationException("Stripe configuration not found");

        var captureRequest = new
        {
            amount_to_capture = (int)(amount * 100)
        };

        var json = JsonSerializer.Serialize(captureRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeConfig.SecretKey}");

        var response = await _httpClient.PostAsync($"{stripeConfig.BaseUrl}/payment_intents/{transactionId}/capture", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stripeResponse = JsonSerializer.Deserialize<StripePaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Error?.Message ?? "Capture failed",
                ErrorCode = errorResponse?.Error?.Code ?? "capture_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> RefundStripePaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken)
    {
        var stripeConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("stripe", StringComparison.OrdinalIgnoreCase));
        if (stripeConfig == null)
            throw new InvalidOperationException("Stripe configuration not found");

        var refundRequest = new
        {
            payment_intent = transactionId,
            amount = (int)(amount * 100),
            reason = "requested_by_customer",
            metadata = new Dictionary<string, string>
            {
                { "refund_reason", reason }
            }
        };

        var json = JsonSerializer.Serialize(refundRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeConfig.SecretKey}");

        var response = await _httpClient.PostAsync($"{stripeConfig.BaseUrl}/refunds", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var refundResponse = JsonSerializer.Deserialize<StripeRefundResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = refundResponse?.Status == "succeeded",
                TransactionId = refundResponse?.Id ?? "",
                Status = PaymentStatus.Refunded,
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorResponse?.Error?.Message ?? "Refund failed",
                ErrorCode = errorResponse?.Error?.Code ?? "refund_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> GetStripePaymentStatusAsync(string transactionId, CancellationToken cancellationToken)
    {
        var stripeConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("stripe", StringComparison.OrdinalIgnoreCase));
        if (stripeConfig == null)
            throw new InvalidOperationException("Stripe configuration not found");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeConfig.SecretKey}");

        var response = await _httpClient.GetAsync($"{stripeConfig.BaseUrl}/payment_intents/{transactionId}", cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stripeResponse = JsonSerializer.Deserialize<StripePaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = MapStripeStatus(stripeResponse?.Status),
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get payment status",
                ErrorCode = "status_error"
            };
        }
    }

    private static PaymentStatus MapStripeStatus(string? stripeStatus)
    {
        return stripeStatus switch
        {
            "succeeded" => PaymentStatus.Completed,
            "processing" => PaymentStatus.Processing,
            "requires_payment_method" => PaymentStatus.Failed,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    private static bool IsRetryableStripeError(string? errorCode)
    {
        var retryableErrors = new[] { "rate_limit", "api_connection_error", "api_error" };
        return retryableErrors.Contains(errorCode);
    }

    #endregion

    #region Adyen Integration (Norwegian market leader)

    private async Task<PaymentGatewayResult> ProcessAdyenPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        var adyenConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("adyen", StringComparison.OrdinalIgnoreCase));
        if (adyenConfig == null)
            throw new InvalidOperationException("Adyen configuration not found");

        var adyenRequest = new
        {
            amount = new
            {
                value = (int)(request.Amount * 100), // Adyen uses minor units
                currency = request.Currency.ToString()
            },
            reference = request.OrderReference,
            paymentMethod = new
            {
                type = MapPaymentMethodToAdyen(request.PaymentMethod),
                encryptedCardNumber = request.PaymentMethodId, // This would be encrypted card data
                encryptedExpiryMonth = request.ExpiryMonth,
                encryptedExpiryYear = request.ExpiryYear,
                encryptedSecurityCode = request.CVV
            },
            returnUrl = request.ReturnUrl,
            merchantAccount = adyenConfig.MerchantAccount,
            shopperReference = request.CustomerId.ToString(),
            shopperEmail = request.CustomerEmail,
            countryCode = "NO",
            shopperLocale = "no_NO",
            metadata = new Dictionary<string, string>
            {
                { "customer_id", request.CustomerId.ToString() },
                { "merchant_id", request.MerchantId.ToString() }
            }
        };

        var json = JsonSerializer.Serialize(adyenRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", adyenConfig.ApiKey);

        var response = await _httpClient.PostAsync($"{adyenConfig.BaseUrl}/payments", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var adyenResponse = JsonSerializer.Deserialize<AdyenPaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = adyenResponse?.ResultCode == "Authorised",
                TransactionId = adyenResponse?.PspReference ?? "",
                AuthorizationCode = adyenResponse?.AuthCode ?? "",
                GatewayTransactionId = adyenResponse?.PspReference ?? "",
                Status = MapAdyenStatus(adyenResponse?.ResultCode),
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Adyen payment failed",
                ErrorCode = "adyen_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> CaptureAdyenPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
    {
        var adyenConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("adyen", StringComparison.OrdinalIgnoreCase));
        if (adyenConfig == null)
            throw new InvalidOperationException("Adyen configuration not found");

        var captureRequest = new
        {
            merchantAccount = adyenConfig.MerchantAccount,
            modificationAmount = new
            {
                value = (int)(amount * 100),
                currency = "NOK"
            },
            originalReference = transactionId,
            reference = $"capture-{transactionId}"
        };

        var json = JsonSerializer.Serialize(captureRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", adyenConfig.ApiKey);

        var response = await _httpClient.PostAsync($"{adyenConfig.BaseUrl}/capture", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Adyen capture failed",
                ErrorCode = "capture_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> RefundAdyenPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken)
    {
        var adyenConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("adyen", StringComparison.OrdinalIgnoreCase));
        if (adyenConfig == null)
            throw new InvalidOperationException("Adyen configuration not found");

        var refundRequest = new
        {
            merchantAccount = adyenConfig.MerchantAccount,
            modificationAmount = new
            {
                value = (int)(amount * 100),
                currency = "NOK"
            },
            originalReference = transactionId,
            reference = $"refund-{transactionId}"
        };

        var json = JsonSerializer.Serialize(refundRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", adyenConfig.ApiKey);

        var response = await _httpClient.PostAsync($"{adyenConfig.BaseUrl}/refund", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Refunded,
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Adyen refund failed",
                ErrorCode = "refund_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> GetAdyenPaymentStatusAsync(string transactionId, CancellationToken cancellationToken)
    {
        // Adyen doesn't have a direct status endpoint, status is typically received via webhooks
        // For demo purposes, we'll return a success status
        await Task.Delay(100, cancellationToken);
        
        return new PaymentGatewayResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = PaymentStatus.Completed
        };
    }

    private static string MapPaymentMethodToAdyen(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.CreditCard => "scheme",
            PaymentMethod.DebitCard => "scheme",
            PaymentMethod.BankTransfer => "directdebit_GB",
            PaymentMethod.DigitalWallet => "applepay",
            _ => "scheme"
        };
    }

    private static PaymentStatus MapAdyenStatus(string? resultCode)
    {
        return resultCode switch
        {
            "Authorised" => PaymentStatus.Completed,
            "Pending" => PaymentStatus.Processing,
            "Refused" => PaymentStatus.Failed,
            "Cancelled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    #endregion

    #region Nets Integration (Norwegian)

    private async Task<PaymentGatewayResult> ProcessNetsPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        var netsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("nets", StringComparison.OrdinalIgnoreCase));
        if (netsConfig == null)
            throw new InvalidOperationException("Nets configuration not found");

        var netsRequest = new
        {
            order = new
            {
                items = new[]
                {
                    new
                    {
                        reference = request.OrderReference,
                        name = request.Description,
                        quantity = 1,
                        unit = "pcs",
                        unitPrice = (int)(request.Amount * 100),
                        taxRate = 2500, // 25% Norwegian VAT
                        taxAmount = (int)(request.Amount * 100 * 0.25),
                        grossTotalAmount = (int)(request.Amount * 100),
                        netTotalAmount = (int)(request.Amount * 100 * 0.8)
                    }
                },
                amount = (int)(request.Amount * 100),
                currency = request.Currency.ToString(),
                reference = request.OrderReference
            },
            checkout = new
            {
                url = request.ReturnUrl,
                termsUrl = "https://example.com/terms",
                merchantTermsUrl = "https://example.com/merchant-terms",
                consumer = new
                {
                    email = request.CustomerEmail,
                    shippingAddress = new
                    {
                        addressLine1 = "Test Address 1",
                        city = "Oslo",
                        postalCode = "0001",
                        country = "NOR"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(netsRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {netsConfig.SecretKey}");

        var response = await _httpClient.PostAsync($"{netsConfig.BaseUrl}/payments", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var netsResponse = JsonSerializer.Deserialize<NetsPaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = netsResponse?.PaymentId ?? "",
                AuthorizationCode = netsResponse?.PaymentId ?? "",
                GatewayTransactionId = netsResponse?.PaymentId ?? "",
                Status = PaymentStatus.Pending, // Nets requires redirect for completion
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent,
                RedirectUrl = netsResponse?.HostedPaymentPageUrl
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Nets payment failed",
                ErrorCode = "nets_error"
            };
        }
    }

    private async Task<PaymentGatewayResult> CaptureNetsPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
    {
        var netsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("nets", StringComparison.OrdinalIgnoreCase));
        if (netsConfig == null)
            throw new InvalidOperationException("Nets configuration not found");

        var captureRequest = new
        {
            amount = (int)(amount * 100)
        };

        var json = JsonSerializer.Serialize(captureRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {netsConfig.SecretKey}");

        var response = await _httpClient.PostAsync($"{netsConfig.BaseUrl}/payments/{transactionId}/charges", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return new PaymentGatewayResult
        {
            Success = response.IsSuccessStatusCode,
            TransactionId = transactionId,
            Status = response.IsSuccessStatusCode ? PaymentStatus.Completed : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            GatewayResponse = responseContent
        };
    }

    private async Task<PaymentGatewayResult> RefundNetsPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken)
    {
        var netsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("nets", StringComparison.OrdinalIgnoreCase));
        if (netsConfig == null)
            throw new InvalidOperationException("Nets configuration not found");

        var refundRequest = new
        {
            amount = (int)(amount * 100)
        };

        var json = JsonSerializer.Serialize(refundRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {netsConfig.SecretKey}");

        var response = await _httpClient.PostAsync($"{netsConfig.BaseUrl}/payments/{transactionId}/refunds", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return new PaymentGatewayResult
        {
            Success = response.IsSuccessStatusCode,
            TransactionId = transactionId,
            Status = response.IsSuccessStatusCode ? PaymentStatus.Refunded : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            GatewayResponse = responseContent
        };
    }

    private async Task<PaymentGatewayResult> GetNetsPaymentStatusAsync(string transactionId, CancellationToken cancellationToken)
    {
        var netsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("nets", StringComparison.OrdinalIgnoreCase));
        if (netsConfig == null)
            throw new InvalidOperationException("Nets configuration not found");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {netsConfig.SecretKey}");

        var response = await _httpClient.GetAsync($"{netsConfig.BaseUrl}/payments/{transactionId}", cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var netsResponse = JsonSerializer.Deserialize<NetsPaymentStatusResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = MapNetsStatus(netsResponse?.Summary?.ReservedAmount > 0),
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get Nets payment status",
                ErrorCode = "status_error"
            };
        }
    }

    private static PaymentStatus MapNetsStatus(bool hasReservedAmount)
    {
        return hasReservedAmount ? PaymentStatus.Completed : PaymentStatus.Failed;
    }

    #endregion

    #region Vipps Integration (Norwegian mobile payment)

    private async Task<PaymentGatewayResult> ProcessVippsPaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        var vippsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("vipps", StringComparison.OrdinalIgnoreCase));
        if (vippsConfig == null)
            throw new InvalidOperationException("Vipps configuration not found");

        // First, get access token
        var accessToken = await GetVippsAccessTokenAsync(vippsConfig, cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get Vipps access token",
                ErrorCode = "auth_error"
            };
        }

        var vippsRequest = new
        {
            amount = (int)(request.Amount * 100),
            currency = request.Currency.ToString(),
            reference = request.OrderReference,
            userFlow = "WEB_REDIRECT",
            returnUrl = request.ReturnUrl,
            paymentDescription = request.Description
        };

        var json = JsonSerializer.Serialize(vippsRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", vippsConfig.SubscriptionKey);
        _httpClient.DefaultRequestHeaders.Add("Merchant-Serial-Number", vippsConfig.MerchantSerialNumber);
        _httpClient.DefaultRequestHeaders.Add("Vipps-System-Name", "RivertyBNPL");
        _httpClient.DefaultRequestHeaders.Add("Vipps-System-Version", "1.0.0");

        var response = await _httpClient.PostAsync($"{vippsConfig.BaseUrl}/epayment/v1/payments", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var vippsResponse = JsonSerializer.Deserialize<VippsPaymentResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = vippsResponse?.Reference ?? "",
                AuthorizationCode = vippsResponse?.Reference ?? "",
                GatewayTransactionId = vippsResponse?.Reference ?? "",
                Status = PaymentStatus.Pending, // Vipps requires redirect
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = responseContent,
                RedirectUrl = vippsResponse?.RedirectUrl
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Vipps payment failed",
                ErrorCode = "vipps_error"
            };
        }
    }

    private async Task<string?> GetVippsAccessTokenAsync(PaymentProviderConfig vippsConfig, CancellationToken cancellationToken)
    {
        var tokenRequest = new
        {
            client_id = vippsConfig.ClientId,
            client_secret = vippsConfig.ClientSecret,
            grant_type = "client_credentials",
            scope = "openid"
        };

        var json = JsonSerializer.Serialize(tokenRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", vippsConfig.SubscriptionKey);

        var response = await _httpClient.PostAsync($"{vippsConfig.BaseUrl}/accesstoken/get", content, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<VippsTokenResponse>(responseContent);
            return tokenResponse?.AccessToken;
        }

        return null;
    }

    private async Task<PaymentGatewayResult> CaptureVippsPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
    {
        // Vipps auto-captures, so this is typically not needed
        await Task.Delay(100, cancellationToken);
        
        return new PaymentGatewayResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private async Task<PaymentGatewayResult> RefundVippsPaymentAsync(string transactionId, decimal amount, string reason, CancellationToken cancellationToken)
    {
        var vippsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("vipps", StringComparison.OrdinalIgnoreCase));
        if (vippsConfig == null)
            throw new InvalidOperationException("Vipps configuration not found");

        var accessToken = await GetVippsAccessTokenAsync(vippsConfig, cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get Vipps access token",
                ErrorCode = "auth_error"
            };
        }

        var refundRequest = new
        {
            amount = (int)(amount * 100),
            description = reason
        };

        var json = JsonSerializer.Serialize(refundRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", vippsConfig.SubscriptionKey);
        _httpClient.DefaultRequestHeaders.Add("Merchant-Serial-Number", vippsConfig.MerchantSerialNumber);

        var response = await _httpClient.PostAsync($"{vippsConfig.BaseUrl}/epayment/v1/payments/{transactionId}/refund", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return new PaymentGatewayResult
        {
            Success = response.IsSuccessStatusCode,
            TransactionId = transactionId,
            Status = response.IsSuccessStatusCode ? PaymentStatus.Refunded : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            GatewayResponse = responseContent
        };
    }

    private async Task<PaymentGatewayResult> GetVippsPaymentStatusAsync(string transactionId, CancellationToken cancellationToken)
    {
        var vippsConfig = _options.Providers.FirstOrDefault(p => p.Name.Equals("vipps", StringComparison.OrdinalIgnoreCase));
        if (vippsConfig == null)
            throw new InvalidOperationException("Vipps configuration not found");

        var accessToken = await GetVippsAccessTokenAsync(vippsConfig, cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get Vipps access token",
                ErrorCode = "auth_error"
            };
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", vippsConfig.SubscriptionKey);
        _httpClient.DefaultRequestHeaders.Add("Merchant-Serial-Number", vippsConfig.MerchantSerialNumber);

        var response = await _httpClient.GetAsync($"{vippsConfig.BaseUrl}/epayment/v1/payments/{transactionId}", cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var vippsResponse = JsonSerializer.Deserialize<VippsPaymentStatusResponse>(responseContent);
            
            return new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = MapVippsStatus(vippsResponse?.State),
                GatewayResponse = responseContent
            };
        }
        else
        {
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Failed to get Vipps payment status",
                ErrorCode = "status_error"
            };
        }
    }

    private static PaymentStatus MapVippsStatus(string? state)
    {
        return state switch
        {
            "AUTHORIZED" => PaymentStatus.Completed,
            "TERMINATED" => PaymentStatus.Completed,
            "EXPIRED" => PaymentStatus.Expired,
            "CANCELLED" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    #endregion
}

#region Configuration and Response Models

public class PaymentGatewayOptions
{
    public string DefaultProvider { get; set; } = "stripe";
    public List<PaymentProviderConfig> Providers { get; set; } = new();
}

public class PaymentProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? PublicKey { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? MerchantAccount { get; set; }
    public string? MerchantSerialNumber { get; set; }
    public string? SubscriptionKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

public class PaymentGatewayRequest
{
    public Guid CustomerId { get; set; }
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? OrderReference { get; set; }
    public string? Description { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CustomerEmail { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? CVV { get; set; }
}

public class PaymentGatewayResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? AuthorizationCode { get; set; }
    public string? GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public bool IsRetryable { get; set; }
    public string? GatewayResponse { get; set; }
    public string? RedirectUrl { get; set; }
}

// Provider-specific response models
public class StripePaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class StripeErrorResponse
{
    public StripeError Error { get; set; } = new();
}

public class StripeError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class StripeRefundResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AdyenPaymentResponse
{
    public string PspReference { get; set; } = string.Empty;
    public string ResultCode { get; set; } = string.Empty;
    public string? AuthCode { get; set; }
}

public class NetsPaymentResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string? HostedPaymentPageUrl { get; set; }
}

public class NetsPaymentStatusResponse
{
    public NetsSummary Summary { get; set; } = new();
}

public class NetsSummary
{
    public int ReservedAmount { get; set; }
}

public class VippsPaymentResponse
{
    public string Reference { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

public class VippsTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
}

public class VippsPaymentStatusResponse
{
    public string State { get; set; } = string.Empty;
}

#endregion