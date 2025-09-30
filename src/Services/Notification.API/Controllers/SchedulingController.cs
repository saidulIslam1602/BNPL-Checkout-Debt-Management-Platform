using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using YourCompanyBNPL.Notification.API.Infrastructure;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for notification scheduling operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SchedulingController : ControllerBase
{
    private readonly INotificationScheduler _scheduler;
    private readonly IValidator<ScheduleNotificationRequest> _scheduleValidator;
    private readonly IValidator<ScheduleRecurringNotificationRequest> _recurringValidator;
    private readonly ILogger<SchedulingController> _logger;

    public SchedulingController(
        INotificationScheduler scheduler,
        IValidator<ScheduleNotificationRequest> scheduleValidator,
        IValidator<ScheduleRecurringNotificationRequest> recurringValidator,
        ILogger<SchedulingController> logger)
    {
        _scheduler = scheduler;
        _scheduleValidator = scheduleValidator;
        _recurringValidator = recurringValidator;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    [HttpPost("schedule")]
    public async Task<IActionResult> ScheduleNotification([FromBody] ScheduleNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _scheduleValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var options = new SchedulingOptions
            {
                RespectBusinessHours = request.RespectBusinessHours,
                BusinessHoursStart = request.BusinessHoursStart,
                BusinessHoursEnd = request.BusinessHoursEnd,
                SkipWeekends = request.SkipWeekends,
                SkipHolidays = request.SkipHolidays,
                Holidays = request.Holidays,
                TimeZone = request.TimeZone
            };

            var jobId = await _scheduler.ScheduleNotificationAsync(request.NotificationId, request.ScheduledAt, options, cancellationToken);

            var response = new ScheduleNotificationResponse
            {
                JobId = jobId,
                NotificationId = request.NotificationId,
                ScheduledAt = request.ScheduledAt,
                Message = "Notification scheduled successfully"
            };

            return Ok(ApiResponse<ScheduleNotificationResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule notification {NotificationId}", request.NotificationId);
            return BadRequest(ApiResponse.ErrorResult($"Failed to schedule notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Schedule a recurring notification
    /// </summary>
    [HttpPost("recurring")]
    public async Task<IActionResult> ScheduleRecurringNotification([FromBody] ScheduleRecurringNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _recurringValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var schedule = new RecurringSchedule
            {
                Type = request.RecurringType,
                Hour = request.Hour,
                Minute = request.Minute,
                DayOfWeek = request.DayOfWeek,
                DayOfMonth = request.DayOfMonth,
                Month = request.Month,
                CronExpression = request.CronExpression,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            var jobId = await _scheduler.ScheduleRecurringNotificationAsync(request.TemplateId, schedule, request.Data, cancellationToken);

            var response = new ScheduleRecurringNotificationResponse
            {
                JobId = jobId,
                TemplateId = request.TemplateId,
                RecurringType = request.RecurringType,
                Message = "Recurring notification scheduled successfully"
            };

            return Ok(ApiResponse<ScheduleRecurringNotificationResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring notification for template {TemplateId}", request.TemplateId);
            return BadRequest(ApiResponse.ErrorResult($"Failed to schedule recurring notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    [HttpDelete("{jobId}")]
    public async Task<IActionResult> CancelScheduledNotification(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _scheduler.CancelScheduledNotificationAsync(jobId, cancellationToken);
            
            if (result)
            {
                return Ok(ApiResponse.SuccessResponse("Scheduled notification cancelled successfully"));
            }
            else
            {
                return NotFound(ApiResponse.ErrorResult("Scheduled notification not found or already processed"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel scheduled notification {JobId}", jobId);
            return BadRequest(ApiResponse.ErrorResult($"Failed to cancel scheduled notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Reschedule a notification
    /// </summary>
    [HttpPut("{jobId}/reschedule")]
    public async Task<IActionResult> RescheduleNotification(string jobId, [FromBody] RescheduleNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _scheduler.RescheduleNotificationAsync(jobId, request.NewScheduledAt, cancellationToken);
            
            if (result)
            {
                return Ok(ApiResponse.SuccessResponse("Notification rescheduled successfully"));
            }
            else
            {
                return NotFound(ApiResponse.ErrorResult("Scheduled notification not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reschedule notification {JobId}", jobId);
            return BadRequest(ApiResponse.ErrorResult($"Failed to reschedule notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get scheduled notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetScheduledNotifications(
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _scheduler.GetScheduledNotificationsAsync(customerId, fromDate, toDate, cancellationToken);
            return Ok(ApiResponse<List<ScheduledNotificationInfo>>.SuccessResult(notifications));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled notifications");
            return BadRequest(ApiResponse.ErrorResult($"Failed to get scheduled notifications: {ex.Message}"));
        }
    }

    /// <summary>
    /// Process pending scheduled notifications (manual trigger)
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessScheduledNotifications(CancellationToken cancellationToken = default)
    {
        try
        {
            await _scheduler.ProcessScheduledNotificationsAsync(cancellationToken);
            return Ok(ApiResponse.SuccessResponse("Scheduled notifications processed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled notifications");
            return BadRequest(ApiResponse.ErrorResult($"Failed to process scheduled notifications: {ex.Message}"));
        }
    }
}