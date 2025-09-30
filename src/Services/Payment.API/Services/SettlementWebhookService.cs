using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Enums;
using System.Text.Json;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for handling settlement webhook notifications
/// </summary>
public interface ISettlementWebhookService
{
    Task SendSettlementCreatedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task SendSettlementProcessedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task SendSettlementFailedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default);
    Task SendSettlementCancelledWebhookAsync(Settlement settlement, string reason, CancellationToken cancellationToken = default);
}

public class SettlementWebhookService : ISettlementWebhookService
{
    private readonly PaymentDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SettlementWebhookService> _logger;
    private readonly IConfiguration _configuration;

    public SettlementWebhookService(
        PaymentDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<SettlementWebhookService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendSettlementCreatedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var webhookPayload = new SettlementWebhookPayload
        {
            EventType = "settlement.created",
            SettlementId = settlement.Id,
            MerchantId = settlement.MerchantId,
            Amount = settlement.NetAmount,
            Currency = settlement.Currency.ToString(),
            Status = settlement.Status.ToString(),
            SettlementDate = settlement.SettlementDate,
            TransactionCount = settlement.TransactionCount,
            CreatedAt = settlement.CreatedAt,
            Timestamp = DateTime.UtcNow
        };

        await SendWebhookAsync(settlement.MerchantId, webhookPayload, cancellationToken);
    }

    public async Task SendSettlementProcessedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var webhookPayload = new SettlementWebhookPayload
        {
            EventType = "settlement.processed",
            SettlementId = settlement.Id,
            MerchantId = settlement.MerchantId,
            Amount = settlement.NetAmount,
            Currency = settlement.Currency.ToString(),
            Status = settlement.Status.ToString(),
            SettlementDate = settlement.SettlementDate,
            TransactionCount = settlement.TransactionCount,
            BankTransactionId = settlement.BankTransactionId,
            ProcessedAt = settlement.ProcessedAt,
            CreatedAt = settlement.CreatedAt,
            Timestamp = DateTime.UtcNow
        };

        await SendWebhookAsync(settlement.MerchantId, webhookPayload, cancellationToken);
    }

    public async Task SendSettlementFailedWebhookAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var webhookPayload = new SettlementWebhookPayload
        {
            EventType = "settlement.failed",
            SettlementId = settlement.Id,
            MerchantId = settlement.MerchantId,
            Amount = settlement.NetAmount,
            Currency = settlement.Currency.ToString(),
            Status = settlement.Status.ToString(),
            SettlementDate = settlement.SettlementDate,
            TransactionCount = settlement.TransactionCount,
            FailureReason = settlement.FailureReason,
            RetryCount = settlement.RetryCount,
            NextRetryAt = settlement.NextRetryAt,
            CreatedAt = settlement.CreatedAt,
            Timestamp = DateTime.UtcNow
        };

        await SendWebhookAsync(settlement.MerchantId, webhookPayload, cancellationToken);
    }

    public async Task SendSettlementCancelledWebhookAsync(Settlement settlement, string reason, CancellationToken cancellationToken = default)
    {
        var webhookPayload = new SettlementWebhookPayload
        {
            EventType = "settlement.cancelled",
            SettlementId = settlement.Id,
            MerchantId = settlement.MerchantId,
            Amount = settlement.NetAmount,
            Currency = settlement.Currency.ToString(),
            Status = settlement.Status.ToString(),
            SettlementDate = settlement.SettlementDate,
            TransactionCount = settlement.TransactionCount,
            FailureReason = reason,
            CreatedAt = settlement.CreatedAt,
            Timestamp = DateTime.UtcNow
        };

        await SendWebhookAsync(settlement.MerchantId, webhookPayload, cancellationToken);
    }

    private async Task SendWebhookAsync(Guid merchantId, SettlementWebhookPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            // Get active webhook endpoints for the merchant
            var webhookEndpoints = await _context.WebhookEndpoints
                .Where(w => w.MerchantId == merchantId && 
                           w.IsActive && 
                           w.Events.Contains("settlement.*") || w.Events.Contains(payload.EventType))
                .ToListAsync(cancellationToken);

            if (!webhookEndpoints.Any())
            {
                _logger.LogInformation("No active webhook endpoints found for merchant {MerchantId} and event {EventType}", 
                    merchantId, payload.EventType);
                return;
            }

            var httpClient = _httpClientFactory.CreateClient("WebhookClient");
            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            foreach (var endpoint in webhookEndpoints)
            {
                try
                {
                    // Create webhook delivery record
                    var delivery = new WebhookDelivery
                    {
                        WebhookEndpointId = endpoint.Id,
                        EventType = payload.EventType,
                        Payload = jsonPayload,
                        Status = WebhookDeliveryStatus.Pending,
                        AttemptCount = 0,
                        NextAttemptAt = DateTime.UtcNow
                    };

                    _context.WebhookDeliveries.Add(delivery);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Send webhook
                    await SendWebhookToEndpointAsync(httpClient, endpoint, delivery, jsonPayload, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending webhook to endpoint {EndpointId} for merchant {MerchantId}", 
                        endpoint.Id, merchantId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhooks for merchant {MerchantId} and event {EventType}", 
                merchantId, payload.EventType);
        }
    }

    private async Task SendWebhookToEndpointAsync(HttpClient httpClient, WebhookEndpoint endpoint, 
        WebhookDelivery delivery, string jsonPayload, CancellationToken cancellationToken)
    {
        var maxAttempts = _configuration.GetValue<int>("Webhook:MaxAttempts", 3);
        var timeoutSeconds = _configuration.GetValue<int>("Webhook:TimeoutSeconds", 30);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                delivery.AttemptCount = attempt;
                delivery.LastAttemptAt = DateTime.UtcNow;

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
                request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                
                // Add webhook signature if secret is configured
                if (!string.IsNullOrEmpty(endpoint.Secret))
                {
                    var signature = GenerateWebhookSignature(jsonPayload, endpoint.Secret);
                    request.Headers.Add("X-Webhook-Signature", signature);
                }

                // Add custom headers
                if (endpoint.Headers != null)
                {
                    foreach (var header in endpoint.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                    }
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var response = await httpClient.SendAsync(request, combinedCts.Token);

                delivery.ResponseStatusCode = (int)response.StatusCode;
                delivery.ResponseBody = await response.Content.ReadAsStringAsync(combinedCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    delivery.Status = WebhookDeliveryStatus.Delivered;
                    delivery.DeliveredAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Webhook delivered successfully to {Url} for event {EventType} (attempt {Attempt})", 
                        endpoint.Url, delivery.EventType, attempt);
                    
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }
                else
                {
                    _logger.LogWarning("Webhook delivery failed to {Url} for event {EventType} (attempt {Attempt}): {StatusCode} {ReasonPhrase}", 
                        endpoint.Url, delivery.EventType, attempt, response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Webhook delivery cancelled for {Url} and event {EventType}", 
                    endpoint.Url, delivery.EventType);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook to {Url} for event {EventType} (attempt {Attempt})", 
                    endpoint.Url, delivery.EventType, attempt);
                
                delivery.ErrorMessage = ex.Message;
            }

            // Calculate next retry time with exponential backoff
            if (attempt < maxAttempts)
            {
                var delayMinutes = Math.Pow(2, attempt) * 5; // 5, 10, 20 minutes
                delivery.NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                
                _logger.LogInformation("Scheduling webhook retry for {Url} and event {EventType} in {DelayMinutes} minutes", 
                    endpoint.Url, delivery.EventType, delayMinutes);
            }
            else
            {
                delivery.Status = WebhookDeliveryStatus.Failed;
                delivery.NextAttemptAt = null;
                
                _logger.LogError("Webhook delivery failed permanently to {Url} for event {EventType} after {MaxAttempts} attempts", 
                    endpoint.Url, delivery.EventType, maxAttempts);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Wait before retry (except for the last attempt)
            if (attempt < maxAttempts)
            {
                var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 1000); // 2, 4, 8 seconds
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }

    private string GenerateWebhookSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLower();
    }
}

/// <summary>
/// Webhook payload for settlement events
/// </summary>
public class SettlementWebhookPayload
{
    public string EventType { get; set; } = string.Empty;
    public Guid SettlementId { get; set; }
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public int TransactionCount { get; set; }
    public string? BankTransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Timestamp { get; set; }
}