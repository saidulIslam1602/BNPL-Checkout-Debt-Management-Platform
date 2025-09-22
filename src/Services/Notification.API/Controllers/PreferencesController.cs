using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Services;
using RivertyBNPL.Shared.Common.Models;

namespace RivertyBNPL.Services.Notification.API.Controllers;

/// <summary>
/// Controller for notification preference operations
/// </summary>
[ApiController]
[Route("api/v1/users/{userId}/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IValidator<UpdateNotificationPreferencesRequest> _validator;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(
        INotificationPreferenceService preferenceService,
        IValidator<UpdateNotificationPreferencesRequest> validator,
        ILogger<PreferencesController> logger)
    {
        _preferenceService = preferenceService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationPreferenceDto>>>> GetUserPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var preferences = await _preferenceService.GetUserPreferencesAsync(userId, cancellationToken);
            return Ok(ApiResponse<List<NotificationPreferenceDto>>.Success(preferences));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification preferences for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<NotificationPreferenceDto>>.Failure("Failed to get preferences", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<List<NotificationPreferenceDto>>>> UpdateUserPreferencesAsync(
        string userId,
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<List<NotificationPreferenceDto>>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var preferences = await _preferenceService.UpdateUserPreferencesAsync(userId, request, cancellationToken);
            
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);
            
            return Ok(ApiResponse<List<NotificationPreferenceDto>>.Success(preferences, "Preferences updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<NotificationPreferenceDto>>.Failure("Failed to update preferences", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Check if notification is allowed for user
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<NotificationAllowedResult>>> CheckNotificationAllowedAsync(
        string userId,
        [FromQuery] Shared.Common.Enums.NotificationType type,
        [FromQuery] Shared.Common.Enums.NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isAllowed = await _preferenceService.IsNotificationAllowedAsync(userId, type, channel, cancellationToken);
            
            var result = new NotificationAllowedResult
            {
                UserId = userId,
                NotificationType = type,
                Channel = channel,
                IsAllowed = isAllowed
            };
            
            return Ok(ApiResponse<NotificationAllowedResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification permission for user {UserId}", userId);
            return StatusCode(500, ApiResponse<NotificationAllowedResult>.Failure("Failed to check notification permission", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Get optimal send time for user
    /// </summary>
    [HttpGet("optimal-time")]
    public async Task<ActionResult<ApiResponse<OptimalSendTimeResult>>> GetOptimalSendTimeAsync(
        string userId,
        [FromQuery] DateTime requestedTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var optimalTime = await _preferenceService.GetOptimalSendTimeAsync(userId, requestedTime, cancellationToken);
            
            var result = new OptimalSendTimeResult
            {
                UserId = userId,
                RequestedTime = requestedTime,
                OptimalTime = optimalTime ?? requestedTime,
                IsAdjusted = optimalTime.HasValue && optimalTime.Value != requestedTime
            };
            
            return Ok(ApiResponse<OptimalSendTimeResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimal send time for user {UserId}", userId);
            return StatusCode(500, ApiResponse<OptimalSendTimeResult>.Failure("Failed to get optimal send time", new[] { ex.Message }));
        }
    }
}

/// <summary>
/// Result for notification allowed check
/// </summary>
public class NotificationAllowedResult
{
    public string UserId { get; set; } = string.Empty;
    public Shared.Common.Enums.NotificationType NotificationType { get; set; }
    public Shared.Common.Enums.NotificationChannel Channel { get; set; }
    public bool IsAllowed { get; set; }
}

/// <summary>
/// Result for optimal send time check
/// </summary>
public class OptimalSendTimeResult
{
    public string UserId { get; set; } = string.Empty;
    public DateTime RequestedTime { get; set; }
    public DateTime OptimalTime { get; set; }
    public bool IsAdjusted { get; set; }
}