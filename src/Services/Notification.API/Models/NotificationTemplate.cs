using System.ComponentModel.DataAnnotations;
using RivertyBNPL.Shared.Common.Models;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Models;

/// <summary>
/// Represents a notification template
/// </summary>
public class NotificationTemplate : BaseEntity
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
    public NotificationType Type { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    public string? HtmlTemplate { get; set; } // For email notifications

    [MaxLength(10)]
    public string Language { get; set; } = "nb-NO"; // Norwegian by default

    public bool IsActive { get; set; } = true;

    public int Version { get; set; } = 1;

    public string? Variables { get; set; } // JSON array of available template variables

    public string? Metadata { get; set; } // Additional template metadata

    // Navigation properties
    public virtual ICollection<NotificationTemplateVersion> Versions { get; set; } = new List<NotificationTemplateVersion>();
}

/// <summary>
/// Tracks template version history
/// </summary>
public class NotificationTemplateVersion : BaseEntity
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public int Version { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    public string? HtmlTemplate { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public string? ChangeNotes { get; set; }

    // Navigation properties
    public virtual NotificationTemplate Template { get; set; } = null!;
}

/// <summary>
/// Notification preferences for users
/// </summary>
public class NotificationPreference : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public NotificationType NotificationType { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    public bool IsEnabled { get; set; } = true;

    public TimeSpan? QuietHoursStart { get; set; }

    public TimeSpan? QuietHoursEnd { get; set; }

    [MaxLength(10)]
    public string? TimeZone { get; set; }

    public string? Metadata { get; set; }
}

/// <summary>
/// Notification queue for batch processing
/// </summary>
public class NotificationQueue : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string QueueName { get; set; } = string.Empty;

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public NotificationPriority Priority { get; set; }

    [Required]
    public string NotificationData { get; set; } = string.Empty; // JSON notification data

    public DateTime? ScheduledFor { get; set; }

    public NotificationQueueStatus Status { get; set; } = NotificationQueueStatus.Pending;

    public int RetryCount { get; set; } = 0;

    public string? ErrorMessage { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(100)]
    public string? BatchId { get; set; }
}

/// <summary>
/// Queue processing status
/// </summary>
public enum NotificationQueueStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}