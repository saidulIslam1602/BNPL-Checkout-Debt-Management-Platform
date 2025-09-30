using YourCompanyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;
using YourCompanyBNPL.Notification.API.Models;

namespace YourCompanyBNPL.Notification.API.DTOs;

/// <summary>
/// Template render result
/// </summary>
public class TemplateRenderResult
{
    public string Subject { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }
}

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

/// <summary>
/// Webhook creation request
/// </summary>
public class CreateWebhookRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Secret { get; set; }

    [Required]
    public List<WebhookEvent> Events { get; set; } = new();

    public bool IsActive { get; set; } = true;

    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;

    [Range(1, 1440)]
    public int RetryDelayMinutes { get; set; } = 5;

    public Dictionary<string, string>? Headers { get; set; }

    public Guid? CustomerId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Webhook configuration response
/// </summary>
public class WebhookConfigResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<WebhookEvent> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public int MaxRetries { get; set; }
    public int RetryDelayMinutes { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Guid? CustomerId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Webhook delivery response
/// </summary>
public class WebhookDeliveryResponse
{
    public Guid Id { get; set; }
    public Guid WebhookConfigId { get; set; }
    public string WebhookName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<Models.WebhookEvent> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public Guid NotificationId { get; set; }
    public WebhookEvent Event { get; set; }
    public Common.Enums.WebhookDeliveryStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Schedule notification request
/// </summary>
public class ScheduleNotificationRequest
{
    [Required]
    public Guid NotificationId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    public bool RespectBusinessHours { get; set; } = false;
    public TimeSpan BusinessHoursStart { get; set; } = new(9, 0, 0);
    public TimeSpan BusinessHoursEnd { get; set; } = new(17, 0, 0);
    public bool SkipWeekends { get; set; } = false;
    public bool SkipHolidays { get; set; } = false;
    public List<DateTime>? Holidays { get; set; }
    public string? TimeZone { get; set; }
}

/// <summary>
/// Schedule notification response
/// </summary>
public class ScheduleNotificationResponse
{
    public string JobId { get; set; } = string.Empty;
    public Guid NotificationId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Schedule recurring notification request
/// </summary>
public class ScheduleRecurringNotificationRequest
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public Infrastructure.RecurringType RecurringType { get; set; }

    [Range(0, 23)]
    public int Hour { get; set; } = 9;

    [Range(0, 59)]
    public int Minute { get; set; } = 0;

    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;

    [Range(1, 31)]
    public int DayOfMonth { get; set; } = 1;

    [Range(1, 12)]
    public int Month { get; set; } = 1;

    public string? CronExpression { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Required]
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Schedule recurring notification response
/// </summary>
public class ScheduleRecurringNotificationResponse
{
    public string JobId { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public Infrastructure.RecurringType RecurringType { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Reschedule notification request
/// </summary>
public class RescheduleNotificationRequest
{
    [Required]
    public DateTime NewScheduledAt { get; set; }
}
// ABTest DTOs
public class CreateABTestExperimentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ABTestExperimentResponse  
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ABTestVariantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Template DTOs
public class NotificationTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CreateNotificationTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// Preference DTOs
public class NotificationPreferenceRequest
{
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
}


// Campaign DTOs

// Notification Preferences
public class NotificationPreferences
{
    public Guid UserId { get; set; }
    public Dictionary<string, bool> Preferences { get; set; } = new();
}
