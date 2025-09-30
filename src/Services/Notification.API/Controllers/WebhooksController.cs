using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Services;
using YourCompanyBNPL.Notification.API.Models;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for webhook management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IValidator<CreateWebhookRequest> _validator;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        IValidator<CreateWebhookRequest> validator,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new webhook configuration
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _webhookService.CreateWebhookAsync(request, cancellationToken);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetWebhook), new { id = result.Data!.Id }, result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Update an existing webhook configuration
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] CreateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _webhookService.UpdateWebhookAsync(id, request, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }

        return result.Errors.Any(e => e.Contains("not found")) ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Get a webhook configuration by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWebhook(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.GetWebhookAsync(id, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }

        return NotFound(result);
    }

    /// <summary>
    /// List webhook configurations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListWebhooks(
        [FromQuery] Guid? customerId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.ListWebhooksAsync(customerId, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a webhook configuration
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.DeleteWebhookAsync(id, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }

        return result.Errors.Any(e => e.Contains("not found")) ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Trigger webhook manually for testing
    /// </summary>
    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestWebhook(Guid id, [FromBody] TestWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.TriggerWebhookAsync(request.NotificationId, request.Event, request.AdditionalData, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get webhook delivery history
    /// </summary>
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetWebhookDeliveries(
        [FromQuery] Guid? webhookId = null,
        [FromQuery] Guid? notificationId = null,
        [FromQuery] Common.Enums.WebhookDeliveryStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.GetWebhookDeliveriesAsync(webhookId, notificationId, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retry a failed webhook delivery
    /// </summary>
    [HttpPost("deliveries/{deliveryId:guid}/retry")]
    public async Task<IActionResult> RetryWebhookDelivery(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.RetryWebhookDeliveryAsync(deliveryId, cancellationToken);
        
        if (result.Success)
        {
            return Ok(result);
        }

        return result.Errors.Any(e => e.Contains("not found")) ? NotFound(result) : BadRequest(result);
    }
}

/// <summary>
/// Test webhook request
/// </summary>
public class TestWebhookRequest
{
    public Guid NotificationId { get; set; }
    public WebhookEvent Event { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}