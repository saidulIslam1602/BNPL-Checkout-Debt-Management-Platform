using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Services;
using RivertyBNPL.Shared.Common.Models;

namespace RivertyBNPL.Services.Notification.API.Controllers;

/// <summary>
/// Controller for notification template operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly INotificationTemplateService _templateService;
    private readonly IValidator<CreateNotificationTemplateRequest> _validator;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        INotificationTemplateService templateService,
        IValidator<CreateNotificationTemplateRequest> validator,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Get all notification templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationTemplateDto>>>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(cancellationToken);
            return Ok(ApiResponse<List<NotificationTemplateDto>>.Success(templates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification templates");
            return StatusCode(500, ApiResponse<List<NotificationTemplateDto>>.Failure("Failed to get templates", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Get notification template by name
    /// </summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> GetTemplateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(name, cancellationToken);
            
            if (template == null)
            {
                return NotFound(ApiResponse<NotificationTemplateDto>.Failure("Template not found"));
            }
            
            return Ok(ApiResponse<NotificationTemplateDto>.Success(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification template {TemplateName}", name);
            return StatusCode(500, ApiResponse<NotificationTemplateDto>.Failure("Failed to get template", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Create new notification template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> CreateTemplateAsync(
        [FromBody] CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<NotificationTemplateDto>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var template = await _templateService.CreateTemplateAsync(request, cancellationToken);
            
            _logger.LogInformation("Created notification template: {TemplateName}", request.Name);
            
            return CreatedAtAction(
                nameof(GetTemplateAsync),
                new { name = template.Name },
                ApiResponse<NotificationTemplateDto>.Success(template, "Template created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<NotificationTemplateDto>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification template");
            return StatusCode(500, ApiResponse<NotificationTemplateDto>.Failure("Failed to create template", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Update existing notification template
    /// </summary>
    [HttpPut("{name}")]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> UpdateTemplateAsync(
        string name,
        [FromBody] CreateNotificationTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<NotificationTemplateDto>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var template = await _templateService.UpdateTemplateAsync(name, request, cancellationToken);
            
            if (template == null)
            {
                return NotFound(ApiResponse<NotificationTemplateDto>.Failure("Template not found"));
            }
            
            _logger.LogInformation("Updated notification template: {TemplateName}", name);
            
            return Ok(ApiResponse<NotificationTemplateDto>.Success(template, "Template updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification template {TemplateName}", name);
            return StatusCode(500, ApiResponse<NotificationTemplateDto>.Failure("Failed to update template", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Delete notification template
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _templateService.DeleteTemplateAsync(name, cancellationToken);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Failure("Template not found"));
            }
            
            _logger.LogInformation("Deleted notification template: {TemplateName}", name);
            
            return Ok(ApiResponse<bool>.Success(result, "Template deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification template {TemplateName}", name);
            return StatusCode(500, ApiResponse<bool>.Failure("Failed to delete template", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Render template with test data
    /// </summary>
    [HttpPost("{name}/render")]
    public async Task<ActionResult<ApiResponse<TemplateRenderResult>>> RenderTemplateAsync(
        string name,
        [FromBody] Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (subject, body, htmlBody) = await _templateService.RenderTemplateAsync(name, data, cancellationToken);
            
            var result = new TemplateRenderResult
            {
                Subject = subject,
                Body = body,
                HtmlBody = htmlBody
            };
            
            return Ok(ApiResponse<TemplateRenderResult>.Success(result, "Template rendered successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TemplateRenderResult>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", name);
            return StatusCode(500, ApiResponse<TemplateRenderResult>.Failure("Failed to render template", new[] { ex.Message }));
        }
    }
}

/// <summary>
/// Template render result
/// </summary>
public class TemplateRenderResult
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
}