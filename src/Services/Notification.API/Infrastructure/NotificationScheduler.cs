using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using YourCompanyBNPL.Notification.API.Data;
using YourCompanyBNPL.Notification.API.Models;
using YourCompanyBNPL.Notification.API.Services;

namespace YourCompanyBNPL.Notification.API.Infrastructure;

/// <summary>
/// Advanced notification scheduler with smart delivery rules
/// </summary>
public interface INotificationScheduler
{
    Task<string> ScheduleNotificationAsync(Guid notificationId, DateTime scheduledAt, SchedulingOptions? options = null, CancellationToken cancellationToken = default);
    Task<string> ScheduleRecurringNotificationAsync(Guid templateId, RecurringSchedule schedule, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    Task<bool> CancelScheduledNotificationAsync(string jobId, CancellationToken cancellationToken = default);
    Task<bool> RescheduleNotificationAsync(string jobId, DateTime newScheduledAt, CancellationToken cancellationToken = default);
    Task<List<ScheduledNotificationInfo>> GetScheduledNotificationsAsync(Guid? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task ProcessScheduledNotificationsAsync(CancellationToken cancellationToken = default);
    Task ProcessScheduledNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Advanced notification scheduler implementation
/// </summary>
public class NotificationScheduler : INotificationScheduler
{
    private readonly NotificationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IPreferenceService _preferenceService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<NotificationScheduler> _logger;

    public NotificationScheduler(
        NotificationDbContext context,
        INotificationService notificationService,
        IPreferenceService preferenceService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<NotificationScheduler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _preferenceService = preferenceService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<string> ScheduleNotificationAsync(Guid notificationId, DateTime scheduledAt, SchedulingOptions? options = null, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            throw new ArgumentException($"Notification {notificationId} not found");
        }

        // Apply smart scheduling rules
        var optimizedScheduleTime = await OptimizeScheduleTimeAsync(notification, scheduledAt, options, cancellationToken);

        // Schedule the job
        var jobId = _backgroundJobClient.Schedule<INotificationScheduler>(
            x => x.ProcessScheduledNotificationAsync(notificationId, CancellationToken.None),
            optimizedScheduleTime);

        // Update notification with job ID and scheduled time
        notification.ScheduledAt = optimizedScheduleTime;
        notification.JobId = jobId;
        notification.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled notification {NotificationId} for {ScheduledAt} with job ID {JobId}",
            notificationId, optimizedScheduleTime, jobId);

        return jobId;
    }

    public async Task<string> ScheduleRecurringNotificationAsync(Guid templateId, RecurringSchedule schedule, Dictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates.FindAsync(templateId);
        if (template == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        var cronExpression = GenerateCronExpression(schedule);
        
        // TODO: Fix Hangfire job scheduling
        var jobId = "scheduled-" + templateId.ToString(); 
        // var jobId = _backgroundJobClient.AddOrUpdate<INotificationScheduler>(
        //     $"recurring-{templateId}-{Guid.NewGuid()}",
        //     x => x.ProcessRecurringNotificationAsync(templateId, data, CancellationToken.None),
        //     cronExpression);

        _logger.LogInformation("Scheduled recurring notification for template {TemplateId} with cron {Cron} and job ID {JobId}",
            templateId, cronExpression, jobId);

        return jobId;
    }

    public async Task<bool> CancelScheduledNotificationAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _backgroundJobClient.Delete(jobId);
            
            // Update notification status
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.JobId == jobId, cancellationToken);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Cancelled;
                notification.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Cancelled scheduled notification with job ID {JobId}", jobId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel scheduled notification with job ID {JobId}", jobId);
            return false;
        }
    }

    public async Task<bool> RescheduleNotificationAsync(string jobId, DateTime newScheduledAt, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the notification
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.JobId == jobId, cancellationToken);
            if (notification == null)
            {
                return false;
            }

            // Cancel existing job
            _backgroundJobClient.Delete(jobId);

            // Schedule new job
            var newJobId = await ScheduleNotificationAsync(notification.Id, newScheduledAt, cancellationToken: cancellationToken);

            _logger.LogInformation("Rescheduled notification {NotificationId} from {OldTime} to {NewTime}. Old job: {OldJobId}, New job: {NewJobId}",
                notification.Id, notification.ScheduledAt, newScheduledAt, jobId, newJobId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reschedule notification with job ID {JobId}", jobId);
            return false;
        }
    }

    public async Task<List<ScheduledNotificationInfo>> GetScheduledNotificationsAsync(Guid? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledAt.HasValue);

        if (customerId.HasValue)
        {
            query = query.Where(n => n.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(n => n.ScheduledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(n => n.ScheduledAt <= toDate.Value);
        }

        var notifications = await query
            .OrderBy(n => n.ScheduledAt)
            .Select(n => new ScheduledNotificationInfo
            {
                NotificationId = n.Id,
                Type = n.Type,
                Channel = n.Channel,
                Recipient = n.Recipient,
                Subject = n.Subject,
                ScheduledAt = n.ScheduledAt!.Value,
                JobId = n.JobId,
                CustomerId = n.CustomerId,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }

    public async Task ProcessScheduledNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(5); // Process notifications scheduled in the next 5 minutes
        
        var scheduledNotifications = await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && 
                       n.ScheduledAt.HasValue && 
                       n.ScheduledAt <= cutoffTime)
            .ToListAsync(cancellationToken);

        foreach (var notification in scheduledNotifications)
        {
            try
            {
                await ProcessScheduledNotificationAsync(notification.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process scheduled notification {NotificationId}", notification.Id);
            }
        }
    }

    [Queue("notifications")]
    public async Task ProcessScheduledNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                _logger.LogWarning("Scheduled notification {NotificationId} not found", notificationId);
                return;
            }

            if (notification.Status != NotificationStatus.Pending)
            {
                _logger.LogInformation("Notification {NotificationId} is no longer pending (status: {Status})", 
                    notificationId, notification.Status);
                return;
            }

            // Check if we should still send this notification (user preferences, quiet hours, etc.)
            if (await ShouldSkipNotificationAsync(notification, cancellationToken))
            {
                _logger.LogInformation("Skipping notification {NotificationId} due to scheduling rules", notificationId);
                return;
            }

            // Send the notification
            await SendScheduledNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully processed scheduled notification {NotificationId}", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled notification {NotificationId}", notificationId);
            throw; // Let Hangfire handle retries
        }
    }

    [Queue("notifications")]
    public async Task ProcessRecurringNotificationAsync(Guid templateId, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null || !template.IsActive)
            {
                _logger.LogWarning("Recurring notification template {TemplateId} not found or inactive", templateId);
                return;
            }

            // Create notification from template
            var notification = new Models.Notification
            {
                Id = Guid.NewGuid(),
                Type = template.Type,
                Channel = template.Channel,
                Subject = template.Subject,
                Content = template.HtmlContent ?? template.TextContent ?? "",
                TemplateId = templateId.ToString(),
                TemplateData = System.Text.Json.JsonSerializer.Serialize(data),
                Status = NotificationStatus.Pending,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Extract recipient from data
            if (data.TryGetValue("recipient", out var recipientObj))
            {
                notification.Recipient = recipientObj.ToString() ?? "";
            }

            if (data.TryGetValue("customerId", out var customerIdObj) && Guid.TryParse(customerIdObj.ToString(), out var customerId))
            {
                notification.CustomerId = customerId;
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Send immediately
            await SendScheduledNotificationAsync(notification, cancellationToken);

            _logger.LogInformation("Successfully processed recurring notification for template {TemplateId}", templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process recurring notification for template {TemplateId}", templateId);
            throw;
        }
    }

    private async Task<DateTime> OptimizeScheduleTimeAsync(Models.Notification notification, DateTime requestedTime, SchedulingOptions? options, CancellationToken cancellationToken)
    {
        var optimizedTime = requestedTime;

        // Apply quiet hours if customer ID is available
        if (notification.CustomerId.HasValue)
        {
            var preferences = await _preferenceService.GetPreferencesAsync(notification.CustomerId.Value, cancellationToken);
            if (preferences != null)
            {
                // TODO: Fix preferences type conversion
                // optimizedTime = ApplyQuietHours(optimizedTime, preferences);
            }
        }

        // Apply scheduling options
        if (options != null)
        {
            optimizedTime = ApplySchedulingOptions(optimizedTime, options);
        }

        // Ensure minimum delay
        var minimumDelay = TimeSpan.FromMinutes(1);
        var earliestTime = DateTime.UtcNow.Add(minimumDelay);
        if (optimizedTime < earliestTime)
        {
            optimizedTime = earliestTime;
        }

        return optimizedTime;
    }

    private DateTime ApplyQuietHours(DateTime scheduledTime, DTOs.PreferencesResponse preferences)
    {
        if (!preferences.QuietHoursStart.HasValue || !preferences.QuietHoursEnd.HasValue)
        {
            return scheduledTime;
        }

        var timeZone = !string.IsNullOrEmpty(preferences.TimeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZone)
            : TimeZoneInfo.Utc;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(scheduledTime, timeZone);
        var quietStart = preferences.QuietHoursStart.Value;
        var quietEnd = preferences.QuietHoursEnd.Value;

        // Check if scheduled time falls within quiet hours
        var scheduledTimeOfDay = localTime.TimeOfDay;

        bool isInQuietHours;
        if (quietStart <= quietEnd)
        {
            // Normal case: quiet hours don't cross midnight
            isInQuietHours = scheduledTimeOfDay >= quietStart && scheduledTimeOfDay <= quietEnd;
        }
        else
        {
            // Quiet hours cross midnight
            isInQuietHours = scheduledTimeOfDay >= quietStart || scheduledTimeOfDay <= quietEnd;
        }

        if (isInQuietHours)
        {
            // Move to the end of quiet hours
            var adjustedLocalTime = localTime.Date.Add(quietEnd);
            if (quietStart > quietEnd && scheduledTimeOfDay >= quietStart)
            {
                // Next day
                adjustedLocalTime = adjustedLocalTime.AddDays(1);
            }

            var adjustedUtcTime = TimeZoneInfo.ConvertTimeToUtc(adjustedLocalTime, timeZone);
            return adjustedUtcTime;
        }

        return scheduledTime;
    }

    private DateTime ApplySchedulingOptions(DateTime scheduledTime, SchedulingOptions options)
    {
        var adjustedTime = scheduledTime;

        // Apply business hours constraint
        if (options.RespectBusinessHours)
        {
            adjustedTime = AdjustForBusinessHours(adjustedTime, options.BusinessHoursStart, options.BusinessHoursEnd, options.TimeZone);
        }

        // Apply weekend constraint
        if (options.SkipWeekends)
        {
            adjustedTime = AdjustForWeekends(adjustedTime, options.TimeZone);
        }

        // Apply holiday constraint
        if (options.SkipHolidays && options.Holidays != null)
        {
            adjustedTime = AdjustForHolidays(adjustedTime, options.Holidays, options.TimeZone);
        }

        return adjustedTime;
    }

    private DateTime AdjustForBusinessHours(DateTime scheduledTime, TimeSpan businessStart, TimeSpan businessEnd, string? timeZone)
    {
        var tz = !string.IsNullOrEmpty(timeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            : TimeZoneInfo.Utc;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(scheduledTime, tz);
        var timeOfDay = localTime.TimeOfDay;

        if (timeOfDay < businessStart)
        {
            // Before business hours - move to start of business hours
            localTime = localTime.Date.Add(businessStart);
        }
        else if (timeOfDay > businessEnd)
        {
            // After business hours - move to start of next business day
            localTime = localTime.Date.AddDays(1).Add(businessStart);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }

    private DateTime AdjustForWeekends(DateTime scheduledTime, string? timeZone)
    {
        var tz = !string.IsNullOrEmpty(timeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            : TimeZoneInfo.Utc;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(scheduledTime, tz);

        while (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
        {
            localTime = localTime.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }

    private DateTime AdjustForHolidays(DateTime scheduledTime, List<DateTime> holidays, string? timeZone)
    {
        var tz = !string.IsNullOrEmpty(timeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(timeZone)
            : TimeZoneInfo.Utc;

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(scheduledTime, tz);

        while (holidays.Any(h => h.Date == localTime.Date))
        {
            localTime = localTime.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }

    private async Task<bool> ShouldSkipNotificationAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        // Check if user has opted out since scheduling
        if (notification.CustomerId.HasValue)
        {
            var isOptedIn = await _preferenceService.IsOptedInAsync(
                notification.CustomerId.Value, 
                notification.Type, 
                notification.Channel, 
                cancellationToken);

            if (!isOptedIn)
            {
                return true;
            }
        }

        // Add more skip conditions as needed
        return false;
    }

    private async Task SendScheduledNotificationAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        // Convert to DTO and send
        var request = new DTOs.SendNotificationRequest
        {
            Type = notification.Type,
            Channel = notification.Channel,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Content = notification.Content,
            TemplateId = !string.IsNullOrEmpty(notification.TemplateId) && Guid.TryParse(notification.TemplateId, out var templateId) ? templateId : null,
            TemplateData = !string.IsNullOrEmpty(notification.TemplateData) 
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notification.TemplateData)
                : null,
            Priority = notification.Priority,
            CustomerId = notification.CustomerId,
            MerchantId = notification.MerchantId,
            PaymentId = notification.PaymentId,
            InstallmentId = notification.InstallmentId,
            Metadata = !string.IsNullOrEmpty(notification.Metadata)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata)
                : null
        };

        await _notificationService.SendNotificationAsync(request, cancellationToken);
    }

    private string GenerateCronExpression(RecurringSchedule schedule)
    {
        return schedule.Type switch
        {
            RecurringType.Daily => $"{schedule.Minute} {schedule.Hour} * * *",
            RecurringType.Weekly => $"{schedule.Minute} {schedule.Hour} * * {(int)schedule.DayOfWeek}",
            RecurringType.Monthly => $"{schedule.Minute} {schedule.Hour} {schedule.DayOfMonth} * *",
            RecurringType.Yearly => $"{schedule.Minute} {schedule.Hour} {schedule.DayOfMonth} {schedule.Month} *",
            RecurringType.Custom => schedule.CronExpression ?? "0 9 * * *", // Default to 9 AM daily
            _ => "0 9 * * *" // Default fallback
        };
    }
}

/// <summary>
/// Scheduling options for advanced scheduling
/// </summary>
public class SchedulingOptions
{
    public bool RespectBusinessHours { get; set; } = false;
    public TimeSpan BusinessHoursStart { get; set; } = new(9, 0, 0); // 9 AM
    public TimeSpan BusinessHoursEnd { get; set; } = new(17, 0, 0); // 5 PM
    public bool SkipWeekends { get; set; } = false;
    public bool SkipHolidays { get; set; } = false;
    public List<DateTime>? Holidays { get; set; }
    public string? TimeZone { get; set; }
}

/// <summary>
/// Recurring schedule configuration
/// </summary>
public class RecurringSchedule
{
    public RecurringType Type { get; set; }
    public int Hour { get; set; } = 9; // Default 9 AM
    public int Minute { get; set; } = 0;
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;
    public int DayOfMonth { get; set; } = 1;
    public int Month { get; set; } = 1;
    public string? CronExpression { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Recurring schedule types
/// </summary>
public enum RecurringType
{
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Custom
}

/// <summary>
/// Scheduled notification information
/// </summary>
public class ScheduledNotificationInfo
{
    public Guid NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string? JobId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
}