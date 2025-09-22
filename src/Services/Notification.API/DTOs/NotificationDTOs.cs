using System.ComponentModel.DataAnnotations;
using RivertyBNPL.Notification.API.Models;

namespace RivertyBNPL.Notification.API.DTOs;

/// <summary>
/// Request to send a single notification
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

    [MaxLength(200)]
    public string? Subject { get; set; }

    public string? Content { get; set; }

    public Guid? TemplateId { get; set; }

    public Dictionary<string, object>? TemplateData { get; set; }

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public DateTime? ScheduledAt { get; set; }

    // Related entities
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request to send bulk notifications
/// </summary>
public class SendBulkNotificationRequest
{
    [Required]
    public List<SendNotificationRequest> Notifications { get; set; } = new();

    [MaxLength(100)]
    public string? BatchId { get; set; }
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
    public string Content { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Notification search request
/// </summary>
public class NotificationSearchRequest
{
    public string? Type { get; set; }
    public NotificationChannel? Channel { get; set; }
    public NotificationStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BatchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Notification analytics response
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
    public Dictionary<NotificationChannel, int> ByChannel { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<NotificationStatus, int> ByStatus { get; set; } = new();
}

/// <summary>
/// Template creation request
/// </summary>
public class CreateTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string? HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }

    [MaxLength(10)]
    public string Language { get; set; } = "en";

    public bool IsActive { get; set; } = true;

    public List<string>? Variables { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Template response
/// </summary>
public class TemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public List<string>? Variables { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Preferences update request
/// </summary>
public class UpdatePreferencesRequest
{
    [Required]
    public Dictionary<string, Dictionary<NotificationChannel, bool>> Preferences { get; set; } = new();

    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
    public string? TimeZone { get; set; }
}

/// <summary>
/// Preferences response
/// </summary>
public class PreferencesResponse
{
    public Guid CustomerId { get; set; }
    public Dictionary<string, Dictionary<NotificationChannel, bool>> Preferences { get; set; } = new();
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
    public string? TimeZone { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Campaign creation request
/// </summary>
public class CreateCampaignRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public Guid TemplateId { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public string? TargetCriteria { get; set; }
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
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public Guid TemplateId { get; set; }
    public CampaignStatus Status { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}