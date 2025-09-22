using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Models;

/// <summary>
/// Notification entity
/// </summary>
public class Notification : BaseEntity
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

    public string? TemplateData { get; set; }

    [Required]
    public NotificationStatus Status { get; set; }

    [Required]
    public NotificationPriority Priority { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 3;

    public string? ErrorMessage { get; set; }

    public string? ExternalId { get; set; }

    public string? ExternalResponse { get; set; }

    // Related entities
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InstallmentId { get; set; }

    // Metadata
    public string? Metadata { get; set; }

    public string? Tags { get; set; }

    [MaxLength(100)]
    public string? CampaignId { get; set; }

    [MaxLength(100)]
    public string? BatchId { get; set; }
}

/// <summary>
/// Notification template entity
/// </summary>
public class NotificationTemplate : BaseEntity
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

    public bool IsActive { get; set; } = true;

    public string? Variables { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Version { get; set; } = 1;

    public DateTime? LastUsedAt { get; set; }

    public int UsageCount { get; set; }
}

/// <summary>
/// Notification preferences for customers
/// </summary>
public class NotificationPreference : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Settings { get; set; }

    public DateTime? OptInDate { get; set; }

    public DateTime? OptOutDate { get; set; }

    [MaxLength(500)]
    public string? OptOutReason { get; set; }
}

/// <summary>
/// Notification delivery tracking
/// </summary>
public class NotificationDelivery : BaseEntity
{
    [Required]
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public NotificationDeliveryStatus Status { get; set; }

    public DateTime? AttemptedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ExternalId { get; set; }

    public string? ExternalResponse { get; set; }

    public decimal? Cost { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    public string? Metadata { get; set; }
}

/// <summary>
/// Notification campaign for bulk messaging
/// </summary>
public class NotificationCampaign : BaseEntity
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
    public NotificationTemplate Template { get; set; } = null!;

    [Required]
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

    [MaxLength(10)]
    public string? Currency { get; set; }

    public string? TargetCriteria { get; set; }

    public string? Settings { get; set; }
}

// Enums
public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3,
    InApp = 4,
    Webhook = 5
}

public enum NotificationStatus
{
    Pending = 1,
    Scheduled = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7,
    Read = 8
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Sending = 2,
    Delivered = 3,
    Failed = 4,
    Bounced = 5,
    Opened = 6,
    Clicked = 7
}

public enum CampaignStatus
{
    Draft = 1,
    Scheduled = 2,
    Running = 3,
    Paused = 4,
    Completed = 5,
    Cancelled = 6,
    Failed = 7
}