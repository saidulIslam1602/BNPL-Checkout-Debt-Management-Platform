using System.ComponentModel.DataAnnotations;
using RivertyBNPL.Shared.Common.Models;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Models;

/// <summary>
/// Represents a notification in the system
/// </summary>
public class Notification : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string RecipientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string RecipientEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? RecipientPhone { get; set; }

    [MaxLength(500)]
    public string? RecipientDeviceToken { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? TemplateData { get; set; } // JSON data for template variables

    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public int RetryCount { get; set; } = 0;

    public int MaxRetries { get; set; } = 3;

    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; } // Provider-specific ID (SendGrid message ID, etc.)

    [MaxLength(50)]
    public string? BatchId { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public string? Metadata { get; set; } // Additional JSON metadata

    // Navigation properties
    public virtual ICollection<NotificationDeliveryAttempt> DeliveryAttempts { get; set; } = new List<NotificationDeliveryAttempt>();
    public virtual ICollection<NotificationEvent> Events { get; set; } = new List<NotificationEvent>();
}

/// <summary>
/// Tracks delivery attempts for notifications
/// </summary>
public class NotificationDeliveryAttempt : BaseEntity
{
    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    public DateTime AttemptedAt { get; set; }

    [Required]
    public NotificationDeliveryStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public string? Response { get; set; } // Provider response

    public TimeSpan? ResponseTime { get; set; }

    // Navigation properties
    public virtual Notification Notification { get; set; } = null!;
}

/// <summary>
/// Tracks notification lifecycle events
/// </summary>
public class NotificationEvent : BaseEntity
{
    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // sent, delivered, opened, clicked, bounced, etc.

    [Required]
    public DateTime EventTime { get; set; }

    public string? EventData { get; set; } // JSON event data

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    // Navigation properties
    public virtual Notification Notification { get; set; } = null!;
}

/// <summary>
/// Notification status enumeration
/// </summary>
public enum NotificationStatus
{
    Pending = 0,
    Queued = 1,
    Sending = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5,
    Cancelled = 6,
    Expired = 7
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Delivery attempt status
/// </summary>
public enum NotificationDeliveryStatus
{
    Attempting = 0,
    Success = 1,
    Failed = 2,
    Bounced = 3,
    Rejected = 4
}