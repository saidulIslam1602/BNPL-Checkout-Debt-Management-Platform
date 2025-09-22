using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using RivertyBNPL.Notification.API.Data;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Models;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// Template service implementation
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(NotificationDbContext context, ILogger<TemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<TemplateResponse>> CreateTemplateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if template with same name and language already exists
            var existingTemplate = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == request.Name && t.Language == request.Language, cancellationToken);

            if (existingTemplate != null)
            {
                return ApiResponse<TemplateResponse>.Failure($"Template with name '{request.Name}' and language '{request.Language}' already exists");
            }

            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Type = request.Type,
                Channel = request.Channel,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent,
                TextContent = request.TextContent,
                SmsContent = request.SmsContent,
                PushContent = request.PushContent,
                Language = request.Language,
                IsActive = request.IsActive,
                Version = 1,
                Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(template);
            return ApiResponse<TemplateResponse>.Success(response, "Template created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template");
            return ApiResponse<TemplateResponse>.Failure($"Failed to create template: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TemplateResponse>> UpdateTemplateAsync(Guid templateId, CreateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
            {
                return ApiResponse<TemplateResponse>.Failure("Template not found");
            }

            template.DisplayName = request.DisplayName;
            template.Description = request.Description;
            template.Type = request.Type;
            template.Channel = request.Channel;
            template.Subject = request.Subject;
            template.HtmlContent = request.HtmlContent;
            template.TextContent = request.TextContent;
            template.SmsContent = request.SmsContent;
            template.PushContent = request.PushContent;
            template.Language = request.Language;
            template.IsActive = request.IsActive;
            template.Version++;
            template.Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null;
            template.Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(template);
            return ApiResponse<TemplateResponse>.Success(response, "Template updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
            return ApiResponse<TemplateResponse>.Failure($"Failed to update template: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TemplateResponse>> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
            {
                return ApiResponse<TemplateResponse>.Failure("Template not found");
            }

            var response = MapToResponse(template);
            return ApiResponse<TemplateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", templateId);
            return ApiResponse<TemplateResponse>.Failure($"Failed to get template: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TemplateResponse>> GetTemplateByNameAsync(string name, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == name && t.Language == language && t.IsActive, cancellationToken);

            if (template == null)
            {
                return ApiResponse<TemplateResponse>.Failure($"Template '{name}' not found for language '{language}'");
            }

            var response = MapToResponse(template);
            return ApiResponse<TemplateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template by name {Name}", name);
            return ApiResponse<TemplateResponse>.Failure($"Failed to get template: {ex.Message}");
        }
    }

    public async Task<PagedApiResponse<TemplateResponse>> ListTemplatesAsync(string? type = null, string? language = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.NotificationTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.Type == type);

            if (!string.IsNullOrEmpty(language))
                query = query.Where(t => t.Language == language);

            if (isActive.HasValue)
                query = query.Where(t => t.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var templates = await query
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Language)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = templates.Select(MapToResponse).ToList();

            return PagedApiResponse<TemplateResponse>.Success(responses, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list templates");
            return PagedApiResponse<TemplateResponse>.Failure($"Failed to list templates: {ex.Message}");
        }
    }

    public async Task<ApiResponse<(string Subject, string HtmlContent, string? TextContent, string? SmsContent, string? PushContent)>> RenderTemplateAsync(Guid templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
            {
                return ApiResponse<(string, string, string?, string?, string?)>.Failure("Template not found");
            }

            var subject = RenderContent(template.Subject, data);
            var htmlContent = RenderContent(template.HtmlContent ?? string.Empty, data);
            var textContent = !string.IsNullOrEmpty(template.TextContent) ? RenderContent(template.TextContent, data) : null;
            var smsContent = !string.IsNullOrEmpty(template.SmsContent) ? RenderContent(template.SmsContent, data) : null;
            var pushContent = !string.IsNullOrEmpty(template.PushContent) ? RenderContent(template.PushContent, data) : null;

            return ApiResponse<(string, string, string?, string?, string?)>.Success(
                (subject, htmlContent, textContent, smsContent, pushContent), 
                "Template rendered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateId}", templateId);
            return ApiResponse<(string, string, string?, string?, string?)>.Failure($"Failed to render template: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template == null)
            {
                return ApiResponse.Failure("Template not found");
            }

            // Soft delete - mark as inactive
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success("Template deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", templateId);
            return ApiResponse.Failure($"Failed to delete template: {ex.Message}");
        }
    }

    private static string RenderContent(string content, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var result = content;

        // Replace placeholders in format {{variableName}}
        var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.IgnoreCase);
        var matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            var placeholder = match.Groups[0].Value;

            if (data.TryGetValue(variableName, out var value))
            {
                var stringValue = value?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, stringValue);
            }
            else
            {
                // Keep placeholder if variable not found
                result = result.Replace(placeholder, $"[{variableName}]");
            }
        }

        return result;
    }

    private static TemplateResponse MapToResponse(NotificationTemplate template)
    {
        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            DisplayName = template.DisplayName,
            Description = template.Description,
            Type = template.Type,
            Channel = template.Channel,
            Subject = template.Subject,
            HtmlContent = template.HtmlContent,
            TextContent = template.TextContent,
            SmsContent = template.SmsContent,
            PushContent = template.PushContent,
            Language = template.Language,
            IsActive = template.IsActive,
            Version = template.Version,
            Variables = !string.IsNullOrEmpty(template.Variables) 
                ? JsonSerializer.Deserialize<List<string>>(template.Variables) 
                : null,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}