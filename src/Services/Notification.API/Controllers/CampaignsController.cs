using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Services;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for campaign operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly IValidator<CreateCampaignRequest> _validator;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ICampaignService campaignService,
        IValidator<CreateCampaignRequest> validator,
        ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Create new campaign
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CampaignResponse>>> CreateCampaignAsync(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<CampaignResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _campaignService.CreateCampaignAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetCampaignAsync), new { id = result.Data?.Id }, result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Get campaign by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CampaignResponse>>> GetCampaignAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.GetCampaignAsync(id, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return NotFound(result);
        }
    }

    /// <summary>
    /// List campaigns
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<CampaignResponse>>> ListCampaignsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.ListCampaignsAsync(page, pageSize, cancellationToken);
        
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
    /// Start campaign execution
    /// </summary>
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ApiResponse>> StartCampaignAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.StartCampaignAsync(id, cancellationToken);
        
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
    /// Pause campaign execution
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    public async Task<ActionResult<ApiResponse>> PauseCampaignAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.PauseCampaignAsync(id, cancellationToken);
        
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
    /// Cancel campaign
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> CancelCampaignAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _campaignService.CancelCampaignAsync(id, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }
}