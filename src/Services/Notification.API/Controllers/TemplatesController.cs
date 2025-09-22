using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Services;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for template operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly IValidator<CreateTemplateRequest> _validator;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        IValidator<CreateTemplateRequest> validator,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Create new template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> CreateTemplateAsync(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<TemplateResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _templateService.CreateTemplateAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetTemplateAsync), new { id = result.Data?.Id }, result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Update existing template
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> UpdateTemplateAsync(
        Guid id,
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<TemplateResponse>.Failure(
                "Validation failed",
                validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _templateService.UpdateTemplateAsync(id, request, cancellationToken);
        
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
    /// Get template by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> GetTemplateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.GetTemplateAsync(id, cancellationToken);
        
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
    /// Get template by name and language
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> GetTemplateByNameAsync(
        string name,
        [FromQuery] string language = "en",
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.GetTemplateByNameAsync(name, language, cancellationToken);
        
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
    /// List templates with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<TemplateResponse>>> ListTemplatesAsync(
        [FromQuery] string? type = null,
        [FromQuery] string? language = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.ListTemplatesAsync(type, language, isActive, page, pageSize, cancellationToken);
        
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
    /// Render template with data
    /// </summary>
    [HttpPost("{id:guid}/render")]
    public async Task<ActionResult<ApiResponse<TemplateRenderResult>>> RenderTemplateAsync(
        Guid id,
        [FromBody] Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.RenderTemplateAsync(id, data, cancellationToken);
        
        if (result.IsSuccess)
        {
            var renderResult = new TemplateRenderResult
            {
                Subject = result.Data.Subject,
                HtmlContent = result.Data.HtmlContent,
                TextContent = result.Data.TextContent,
                SmsContent = result.Data.SmsContent,
                PushContent = result.Data.PushContent
            };
            
            return Ok(ApiResponse<TemplateRenderResult>.Success(renderResult, "Template rendered successfully"));
        }
        else
        {
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Delete template
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteTemplateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.DeleteTemplateAsync(id, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return NotFound(result);
        }
    }
}

/// <summary>
/// Template render result
/// </summary>
public class TemplateRenderResult
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? SmsContent { get; set; }
    public string? PushContent { get; set; }
}