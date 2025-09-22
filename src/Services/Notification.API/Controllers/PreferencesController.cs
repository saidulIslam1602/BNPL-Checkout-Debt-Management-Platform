using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Services;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for notification preference operations
/// </summary>
[ApiController]
[Route("api/v1/customers/{customerId:guid}/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;
    private readonly IValidator<UpdatePreferencesRequest> _validator;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(
        IPreferenceService preferenceService,
        IValidator<UpdatePreferencesRequest> validator,
        ILogger<PreferencesController> logger)
    {
        _preferenceService = preferenceService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Get customer notification preferences
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PreferencesResponse>>> GetPreferencesAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var result = await _preferenceService.GetPreferencesAsync(customerId, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Update customer notification preferences
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<PreferencesResponse>>> UpdatePreferencesAsync(
        Guid customerId,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<PreferencesResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _preferenceService.UpdatePreferencesAsync(customerId, request, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Check if customer has opted in for specific notification type and channel
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<OptInCheckResult>>> CheckOptInAsync(
        Guid customerId,
        [FromQuery] string notificationType,
        [FromQuery] Models.NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        var isOptedIn = await _preferenceService.IsOptedInAsync(customerId, notificationType, channel, cancellationToken);
        
        var result = new OptInCheckResult
        {
            CustomerId = customerId,
            NotificationType = notificationType,
            Channel = channel,
            IsOptedIn = isOptedIn
        };
        
        return Ok(ApiResponse<OptInCheckResult>.Success(result));
    }
}

/// <summary>
/// Opt-in check result
/// </summary>
public class OptInCheckResult
{
    public Guid CustomerId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public Models.NotificationChannel Channel { get; set; }
    public bool IsOptedIn { get; set; }
}