using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Services;
using YourCompanyBNPL.Notification.API.Infrastructure;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Controllers;

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
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IValidator<SendNotificationRequest> sendValidator,
        IValidator<SendBulkNotificationRequest> bulkValidator,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _sendValidator = sendValidator;
        _bulkValidator = bulkValidator;
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
            return BadRequest(ApiResponse<NotificationResponse>.ErrorResult(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _notificationService.SendNotificationAsync(request, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Send bulk notifications
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<List<NotificationResponse>>>> SendBulkNotificationAsync(
        [FromBody] SendBulkNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _bulkValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<List<NotificationResponse>>.ErrorResult(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _notificationService.SendBulkNotificationAsync(request, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
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
        var result = await _notificationService.GetNotificationAsync(id, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return NotFound(result);
        }
    }

    /// <summary>
    /// Search notifications with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<NotificationResponse>>> SearchNotificationsAsync(
        [FromQuery] NotificationSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.SearchNotificationsAsync(request, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
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
        var result = await _notificationService.RetryNotificationAsync(id, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Cancel scheduled notification
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> CancelNotificationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.CancelNotificationAsync(id, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Get notification analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<NotificationAnalytics>>> GetAnalyticsAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            return BadRequest(ApiResponse<NotificationAnalytics>.ErrorResult("From date must be before to date"));
        }

        if ((toDate - fromDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse<NotificationAnalytics>.ErrorResult("Date range cannot exceed 365 days"));
        }

        var result = await _notificationService.GetAnalyticsAsync(fromDate, toDate, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Process scheduled notifications (admin endpoint)
    /// </summary>
    [HttpPost("process-scheduled")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> ProcessScheduledNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.ProcessScheduledNotificationsAsync(cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// Process retry notifications (admin endpoint)
    /// </summary>
    [HttpPost("process-retries")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> ProcessRetryNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.ProcessRetryNotificationsAsync(cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }
}