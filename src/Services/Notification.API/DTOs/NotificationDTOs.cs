using System.ComponentModel.DataAnnotations;
using RivertyBNPL.Notification.API.Models;

namespace RivertyBNPL.Notification.API.DTOs;

/// <summary>
/// Request to send a notification
/// </summary>
public class SendNotificationRequest
{
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(500)]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? TemplateId { get; set; }

    public Dictionary<string, object>? TemplateData { get; set; }

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public DateTime? ScheduledAt { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public List<string>? Tags { get; set; }

    [MaxLength(100)]
    public string? CampaignId { get; set; }
}

/// <summary>
/// Bulk notification request
/// </summary>
public class SendBulkNotificationRequest
{
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public List<BulkRecipient> Recipients { get; set; } = new();

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? TemplateId { get; set; }

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public DateTime? ScheduledAt { get; set; }

    [MaxLength(100)]
    public string? CampaignId { get; set; }

    public Dictionary<string, object>? GlobalTemplateData { get; set; }
}

public class BulkRecipient
{
    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public Dictionary<string, object>? TemplateData { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }
}

/// <summary>
/// Notification response
/// </summary>
public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }
    public List<string>? Tags { get; set; }
    public string? CampaignId { get; set; }
}

/// <summary>
/// Template request
/// </summary>
public class CreateTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = "en";

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string HtmlContent { get; set; } = string.Empty;

    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }

    public List<string>? Variables { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Template response
/// </summary>
public class TemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }
    public bool IsActive { get; set; }
    public List<string>? Variables { get; set; }
    public string? Description { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// Notification preferences request
/// </summary>
public class UpdatePreferencesRequest
{
    [Required]
    public List<PreferenceItem> Preferences { get; set; } = new();
}

public class PreferenceItem
{
    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    public bool IsEnabled { get; set; }

    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Notification preferences response
/// </summary>
public class PreferencesResponse
{
    public Guid CustomerId { get; set; }
    public List<PreferenceResponse> Preferences { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class PreferenceResponse
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
    public DateTime? OptInDate { get; set; }
    public DateTime? OptOutDate { get; set; }
    public string? OptOutReason { get; set; }
}

/// <summary>
/// Campaign request
/// </summary>
public class CreateCampaignRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public Guid TemplateId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public Dictionary<string, object>? TargetCriteria { get; set; }

    public Dictionary<string, object>? Settings { get; set; }
}

/// <summary>
/// Campaign response
/// </summary>
public class CampaignResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public NotificationChannel Channel { get; set; }
    public Guid TemplateId { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int OpenedCount { get; set; }
    public int ClickedCount { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Currency { get; set; }
}

/// <summary>
/// Notification analytics
/// </summary>
public class NotificationAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ClickRate { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "NOK";
    public Dictionary<string, int> ByChannel { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public List<DailyStats> DailyStats { get; set; } = new();
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
}

/// <summary>
/// Search request
/// </summary>
public class NotificationSearchRequest
{
    public string? Type { get; set; }
    public NotificationChannel? Channel { get; set; }
    public NotificationStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? CampaignId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}