using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for handling payment webhooks and notifications
/// </summary>
public interface IPaymentWebhookService
{
    Task<ApiResponse> ProcessWebhookAsync(string provider, string signature, string payload, CancellationToken cancellationToken = default);
    Task<ApiResponse> RegisterWebhookEndpointAsync(RegisterWebhookRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> SendWebhookAsync(Guid paymentId, Common.Enums.WebhookEventType eventType, CancellationToken cancellationToken = default);
    Task<ApiResponse> RetryFailedWebhooksAsync(CancellationToken cancellationToken = default);
}

public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly PaymentDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentWebhookService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentWebhookService(
        PaymentDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentWebhookService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse> ProcessWebhookAsync(string provider, string signature, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing webhook from provider {Provider}", provider);

            // Verify webhook signature
            if (!await VerifyWebhookSignatureAsync(provider, signature, payload))
            {
                _logger.LogWarning("Invalid webhook signature from provider {Provider}", provider);
                return ApiResponse.ErrorResult("Invalid webhook signature", 401);
            }

            // Parse webhook payload
            var webhookData = JsonSerializer.Deserialize<WebhookPayload>(payload);
            if (webhookData == null)
            {
                return ApiResponse.ErrorResult("Invalid webhook payload", 400);
            }

            // Process based on event type
            var result = webhookData.EventType switch
            {
                "payment.completed" => await HandlePaymentCompletedWebhookAsync(webhookData, cancellationToken),
                "payment.failed" => await HandlePaymentFailedWebhookAsync(webhookData, cancellationToken),
                "payment.refunded" => await HandlePaymentRefundedWebhookAsync(webhookData, cancellationToken),
                "payment.disputed" => await HandlePaymentDisputedWebhookAsync(webhookData, cancellationToken),
                _ => ApiResponse.SuccessResponse("Webhook event ignored")
            };

            // Log webhook event
            var webhookLog = new WebhookLog
            {
                Provider = provider,
                EventType = webhookData.EventType,
                PaymentId = webhookData.PaymentId,
                Payload = payload,
                ProcessedAt = DateTime.UtcNow,
                Success = result.Success,
                ErrorMessage = result.Success ? null : string.Join(", ", result.Errors ?? new List<string>())
            };

            _context.WebhookLogs.Add(webhookLog);
            await _context.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from provider {Provider}", provider);
            return ApiResponse.ErrorResult("Failed to process webhook", 500);
        }
    }

    public async Task<ApiResponse> RegisterWebhookEndpointAsync(RegisterWebhookRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == request.MerchantId, cancellationToken);

            if (merchant == null)
            {
                return ApiResponse.ErrorResult("Merchant not found", 404);
            }

            var webhookEndpoint = new WebhookEndpoint
            {
                MerchantId = request.MerchantId,
                Url = request.Url,
                Events = request.Events,
                Secret = GenerateWebhookSecret(),
                IsActive = true
            };

            _context.WebhookEndpoints.Add(webhookEndpoint);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Webhook endpoint registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook endpoint for merchant {MerchantId}", request.MerchantId);
            return ApiResponse.ErrorResult("Failed to register webhook endpoint", 500);
        }
    }

    public async Task<ApiResponse> SendWebhookAsync(Guid paymentId, Common.Enums.WebhookEventType eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment?.Merchant == null)
            {
                return ApiResponse.ErrorResult("Payment or merchant not found", 404);
            }

            var webhookEndpoints = await _context.WebhookEndpoints
                .Where(we => we.MerchantId == payment.MerchantId && 
                           we.IsActive && 
                           we.Events.Contains(eventType.ToString()))
                .ToListAsync(cancellationToken);

            var tasks = webhookEndpoints.Select(endpoint => 
                SendWebhookToEndpointAsync(endpoint, payment, eventType, cancellationToken));

            await Task.WhenAll(tasks);

            return ApiResponse.SuccessResponse("Webhooks sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhooks for payment {PaymentId}", paymentId);
            return ApiResponse.ErrorResult("Failed to send webhooks", 500);
        }
    }

    public async Task<ApiResponse> RetryFailedWebhooksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var failedWebhooks = await _context.WebhookDeliveries
                .Where(wd => !wd.Success && wd.RetryCount < 5 && wd.NextRetryAt <= DateTime.UtcNow)
                .Include(wd => wd.WebhookEndpoint)
                .ToListAsync(cancellationToken);

            var retryTasks = failedWebhooks.Select(webhook => RetryWebhookDeliveryAsync(webhook, cancellationToken));
            await Task.WhenAll(retryTasks);

            return ApiResponse.SuccessResponse($"Retried {failedWebhooks.Count} failed webhooks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed webhooks");
            return ApiResponse.ErrorResult("Failed to retry webhooks", 500);
        }
    }

    #region Private Methods

    private async Task<bool> VerifyWebhookSignatureAsync(string provider, string signature, string payload)
    {
        try
        {
            var secret = _configuration[$"Webhooks:{provider}:Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLower();
            
            return signature.Equals($"sha256={computedSignature}", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<ApiResponse> HandlePaymentCompletedWebhookAsync(WebhookPayload webhookData, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == webhookData.PaymentId, cancellationToken);

        if (payment == null)
        {
            return ApiResponse.ErrorResult("Payment not found", 404);
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Completed;
            payment.ProcessedAt = DateTime.UtcNow;
            payment.GatewayTransactionId = webhookData.TransactionId;
            
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResponse("Payment completed webhook processed");
    }

    private async Task<ApiResponse> HandlePaymentFailedWebhookAsync(WebhookPayload webhookData, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == webhookData.PaymentId, cancellationToken);

        if (payment == null)
        {
            return ApiResponse.ErrorResult("Payment not found", 404);
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = webhookData.FailureReason;
            payment.ErrorCode = webhookData.ErrorCode;
            
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResponse("Payment failed webhook processed");
    }

    private async Task<ApiResponse> HandlePaymentRefundedWebhookAsync(WebhookPayload webhookData, CancellationToken cancellationToken)
    {
        // Handle refund webhook logic
        return ApiResponse.SuccessResponse("Payment refunded webhook processed");
    }

    private async Task<ApiResponse> HandlePaymentDisputedWebhookAsync(WebhookPayload webhookData, CancellationToken cancellationToken)
    {
        // Handle dispute webhook logic
        return ApiResponse.SuccessResponse("Payment disputed webhook processed");
    }

    private async Task SendWebhookToEndpointAsync(WebhookEndpoint endpoint, Models.Payment payment, WebhookEventType eventType, CancellationToken cancellationToken)
    {
        try
        {
            var payload = CreateWebhookPayload(payment, eventType);
            var jsonPayload = JsonSerializer.Serialize(payload);
            var signature = GenerateWebhookSignature(jsonPayload, endpoint.Secret);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-YourCompany-Signature", signature);
            request.Headers.Add("X-YourCompany-Event", eventType.ToString());

            var delivery = new WebhookDelivery
            {
                WebhookEndpointId = endpoint.Id,
                PaymentId = payment.Id,
                EventType = eventType.ToString(),
                Payload = jsonPayload,
                AttemptedAt = DateTime.UtcNow
            };

            var response = await httpClient.SendAsync(request, cancellationToken);
            
            delivery.Success = response.IsSuccessStatusCode;
            delivery.StatusCode = (int)response.StatusCode;
            delivery.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!delivery.Success)
            {
                delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, delivery.RetryCount) * 5);
            }

            _context.WebhookDeliveries.Add(delivery);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to endpoint {EndpointUrl}", endpoint.Url);
        }
    }

    private async Task RetryWebhookDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(HttpMethod.Post, delivery.WebhookEndpoint.Url)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
            };

            var signature = GenerateWebhookSignature(delivery.Payload, delivery.WebhookEndpoint.Secret);
            request.Headers.Add("X-YourCompany-Signature", signature);
            request.Headers.Add("X-YourCompany-Event", delivery.EventType);

            var response = await httpClient.SendAsync(request, cancellationToken);
            
            delivery.Success = response.IsSuccessStatusCode;
            delivery.StatusCode = (int)response.StatusCode;
            delivery.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            delivery.RetryCount++;
            delivery.AttemptedAt = DateTime.UtcNow;

            if (!delivery.Success && delivery.RetryCount < 5)
            {
                delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, delivery.RetryCount) * 5);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry webhook delivery {DeliveryId}", delivery.Id);
        }
    }

    private static object CreateWebhookPayload(Models.Payment payment, WebhookEventType eventType)
    {
        return new
        {
            id = Guid.NewGuid(),
            type = eventType.ToString(),
            created = DateTime.UtcNow,
            data = new
            {
                payment = new
                {
                    id = payment.Id,
                    amount = payment.Amount,
                    currency = payment.Currency.ToString(),
                    status = payment.Status.ToString(),
                    payment_method = payment.PaymentMethod.ToString(),
                    transaction_id = payment.TransactionId,
                    order_reference = payment.OrderReference,
                    created_at = payment.CreatedAt,
                    processed_at = payment.ProcessedAt
                }
            }
        };
    }

    private static string GenerateWebhookSecret()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateWebhookSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLower()}";
    }

    #endregion
}

// Supporting DTOs and models
public class WebhookPayload
{
    public Guid PaymentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RegisterWebhookRequest
{
    public Guid MerchantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
}
