using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Services;
using RivertyBNPL.Shared.Common.Models;

namespace RivertyBNPL.Services.Notification.API.Controllers;

/// <summary>
/// Controller for notification operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<SendNotificationRequest> _sendValidator;
    private readonly IValidator<SendBulkNotificationRequest> _bulkValidator;
    private readonly IValidator<NotificationQueryParams> _queryValidator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IValidator<SendNotificationRequest> sendValidator,
        IValidator<SendBulkNotificationRequest> bulkValidator,
        IValidator<NotificationQueryParams> queryValidator,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _sendValidator = sendValidator;
        _bulkValidator = bulkValidator;
        _queryValidator = queryValidator;
        _logger = logger;
    }

    /// <summary>
    /// Send a single notification
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> SendNotificationAsync(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _sendValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<NotificationResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var result = await _notificationService.SendNotificationAsync(request, cancellationToken);
            
            _logger.LogInformation("Notification sent successfully: {NotificationId}", result.Id);
            
            return Ok(ApiResponse<NotificationResponse>.Success(result, "Notification sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return StatusCode(500, ApiResponse<NotificationResponse>.Failure("Failed to send notification", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Send bulk notifications
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<BulkNotificationResponse>>> SendBulkNotificationAsync(
        [FromBody] SendBulkNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _bulkValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<BulkNotificationResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var result = await _notificationService.SendBulkNotificationAsync(request, cancellationToken);
            
            _logger.LogInformation("Bulk notification processed: {TotalCount} total, {SuccessCount} successful, {FailedCount} failed", 
                result.TotalCount, result.SuccessCount, result.FailedCount);
            
            return Ok(ApiResponse<BulkNotificationResponse>.Success(result, "Bulk notification processed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notification");
            return StatusCode(500, ApiResponse<BulkNotificationResponse>.Failure("Failed to send bulk notification", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> GetNotificationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationService.GetNotificationAsync(id, cancellationToken);
            
            if (notification == null)
            {
                return NotFound(ApiResponse<NotificationResponse>.Failure("Notification not found"));
            }
            
            return Ok(ApiResponse<NotificationResponse>.Success(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<NotificationResponse>.Failure("Failed to get notification", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Get notifications with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationResponse>>>> GetNotificationsAsync(
        [FromQuery] NotificationQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _queryValidator.ValidateAsync(queryParams, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<PaginatedResponse<NotificationResponse>>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var (notifications, totalCount) = await _notificationService.GetNotificationsAsync(queryParams, cancellationToken);
            
            var response = new PaginatedResponse<NotificationResponse>
            {
                Data = notifications,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize)
            };
            
            return Ok(ApiResponse<PaginatedResponse<NotificationResponse>>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications");
            return StatusCode(500, ApiResponse<PaginatedResponse<NotificationResponse>>.Failure("Failed to get notifications", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Update notification status
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateNotificationStatusAsync(
        Guid id,
        [FromBody] UpdateNotificationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _notificationService.UpdateNotificationStatusAsync(id, request, cancellationToken);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Failure("Notification not found"));
            }
            
            return Ok(ApiResponse<bool>.Success(result, "Notification status updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification status {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Failure("Failed to update notification status", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Record delivery event for notification
    /// </summary>
    [HttpPost("{id:guid}/events")]
    public async Task<ActionResult<ApiResponse<bool>>> RecordDeliveryEventAsync(
        Guid id,
        [FromBody] NotificationDeliveryEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _notificationService.RecordDeliveryEventAsync(id, request, cancellationToken);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Failure("Notification not found"));
            }
            
            return Ok(ApiResponse<bool>.Success(result, "Delivery event recorded"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record delivery event for notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Failure("Failed to record delivery event", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Cancel scheduled notification
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelNotificationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _notificationService.CancelNotificationAsync(id, cancellationToken);
            
            if (!result)
            {
                return BadRequest(ApiResponse<bool>.Failure("Cannot cancel notification - it may not exist or is not in a cancellable state"));
            }
            
            return Ok(ApiResponse<bool>.Success(result, "Notification cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<bool>.Failure("Failed to cancel notification", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Retry failed notification
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> RetryNotificationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _notificationService.RetryNotificationAsync(id, cancellationToken);
            
            if (result == null)
            {
                return BadRequest(ApiResponse<NotificationResponse>.Failure("Cannot retry notification - it may not exist, is not failed, or has exceeded retry limits"));
            }
            
            return Ok(ApiResponse<NotificationResponse>.Success(result, "Notification retry initiated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry notification {NotificationId}", id);
            return StatusCode(500, ApiResponse<NotificationResponse>.Failure("Failed to retry notification", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetNotificationStatsAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest(ApiResponse<NotificationStatsDto>.Failure("From date must be before to date"));
        }

        if ((toDate - fromDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse<NotificationStatsDto>.Failure("Date range cannot exceed 365 days"));
        }

        try
        {
            var stats = await _notificationService.GetNotificationStatsAsync(fromDate, toDate, cancellationToken);
            
            return Ok(ApiResponse<NotificationStatsDto>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification statistics");
            return StatusCode(500, ApiResponse<NotificationStatsDto>.Failure("Failed to get notification statistics", new[] { ex.Message }));
        }
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}