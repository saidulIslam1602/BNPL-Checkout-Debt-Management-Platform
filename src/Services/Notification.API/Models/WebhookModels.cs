using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Models;

/// <summary>
/// Webhook configuration for delivery tracking
/// </summary>
public class WebhookConfig : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public List<WebhookEvent> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, string> Headers { get; set; } = new();
    public Guid? CustomerId { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Webhook delivery attempt
/// </summary>
public class WebhookDelivery : BaseEntity
{
    public Guid WebhookConfigId { get; set; }
    public WebhookConfig WebhookConfig { get; set; } = null!;
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public WebhookEvent Event { get; set; }
    public string Payload { get; set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ResponseBody { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ResponseTime { get; set; }
}

/// <summary>
/// Webhook events that can trigger callbacks
/// </summary>
public enum WebhookEvent
{
    NotificationSent,
    NotificationDelivered,
    NotificationFailed,
    NotificationOpened,
    NotificationClicked,
    NotificationBounced,
    NotificationComplained,
    NotificationUnsubscribed,
    CampaignStarted,
    CampaignCompleted,
    CampaignPaused,
    CampaignCancelled
}

/// <summary>
/// Webhook delivery status
/// </summary>
public enum WebhookDeliveryStatus
{
    Pending,
    Delivered,
    Failed,
    Retrying,
    Cancelled
}

/// <summary>
/// Webhook payload structure
/// </summary>
public class WebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public WebhookEvent EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid NotificationId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? ExternalId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? CampaignId { get; set; }
    public string? BatchId { get; set; }
}

/// <summary>
/// Webhook signature for security
/// </summary>
public static class WebhookSignature
{
    public static string GenerateSignature(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool ValidateSignature(string payload, string signature, string secret)
    {
        var expectedSignature = GenerateSignature(payload, secret);
        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }
}