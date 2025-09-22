using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using RivertyBNPL.Services.Notification.API.Data;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Models;

namespace RivertyBNPL.Services.Notification.API.Services;

/// <summary>
/// Implementation of notification template service
/// </summary>
public class NotificationTemplateService : INotificationTemplateService
{
    private readonly NotificationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        NotificationDbContext context,
        IMapper mapper,
        ILogger<NotificationTemplateService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<NotificationTemplateDto>>(templates);
    }

    public async Task<NotificationTemplateDto?> GetTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == name && t.IsActive, cancellationToken);

        return template != null ? _mapper.Map<NotificationTemplateDto>(template) : null;
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        // Check if template with same name already exists
        var existingTemplate = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == request.Name, cancellationToken);

        if (existingTemplate != null)
        {
            throw new InvalidOperationException($"Template with name '{request.Name}' already exists");
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
            BodyTemplate = request.BodyTemplate,
            HtmlTemplate = request.HtmlTemplate,
            Language = request.Language,
            IsActive = true,
            Version = 1,
            Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplates.Add(template);

        // Create initial version
        var templateVersion = new NotificationTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Version = 1,
            Subject = request.Subject,
            BodyTemplate = request.BodyTemplate,
            HtmlTemplate = request.HtmlTemplate,
            IsActive = true,
            ChangeNotes = request.ChangeNotes ?? "Initial version",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplateVersions.Add(templateVersion);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new notification template: {TemplateName}", request.Name);

        return _mapper.Map<NotificationTemplateDto>(template);
    }

    public async Task<NotificationTemplateDto?> UpdateTemplateAsync(string name, CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

        if (template == null)
        {
            return null;
        }

        // Deactivate current version
        var currentVersion = template.Versions.FirstOrDefault(v => v.IsActive);
        if (currentVersion != null)
        {
            currentVersion.IsActive = false;
        }

        // Update template
        template.DisplayName = request.DisplayName;
        template.Description = request.Description;
        template.Type = request.Type;
        template.Channel = request.Channel;
        template.Subject = request.Subject;
        template.BodyTemplate = request.BodyTemplate;
        template.HtmlTemplate = request.HtmlTemplate;
        template.Language = request.Language;
        template.Version++;
        template.Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null;
        template.UpdatedAt = DateTime.UtcNow;

        // Create new version
        var newVersion = new NotificationTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Version = template.Version,
            Subject = request.Subject,
            BodyTemplate = request.BodyTemplate,
            HtmlTemplate = request.HtmlTemplate,
            IsActive = true,
            ChangeNotes = request.ChangeNotes ?? $"Version {template.Version}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationTemplateVersions.Add(newVersion);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notification template: {TemplateName} to version {Version}", name, template.Version);

        return _mapper.Map<NotificationTemplateDto>(template);
    }

    public async Task<bool> DeleteTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

        if (template == null)
        {
            return false;
        }

        // Soft delete - mark as inactive
        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted notification template: {TemplateName}", name);

        return true;
    }

    public async Task<(string Subject, string Body, string? HtmlBody)> RenderTemplateAsync(string templateName, Dictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        try
        {
            var subject = RenderTemplate(template.Subject, data);
            var body = RenderTemplate(template.BodyTemplate, data);
            var htmlBody = !string.IsNullOrEmpty(template.HtmlTemplate) 
                ? RenderTemplate(template.HtmlTemplate, data) 
                : null;

            return (subject, body, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);
            throw new InvalidOperationException($"Failed to render template '{templateName}': {ex.Message}", ex);
        }
    }

    private static string RenderTemplate(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = template;

        // Replace placeholders in format {{variableName}}
        var regex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.IgnoreCase);
        var matches = regex.Matches(template);

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
                // Or optionally replace with empty string or default value
                result = result.Replace(placeholder, $"[{variableName}]");
            }
        }

        return result;
    }
}

/// <summary>
/// Implementation of notification preference service
/// </summary>
public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly NotificationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationPreferenceService> _logger;

    public NotificationPreferenceService(
        NotificationDbContext context,
        IMapper mapper,
        ILogger<NotificationPreferenceService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<NotificationPreferenceDto>> GetUserPreferencesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var preferences = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<NotificationPreferenceDto>>(preferences);
    }

    public async Task<List<NotificationPreferenceDto>> UpdateUserPreferencesAsync(string userId, UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        // Get existing preferences
        var existingPreferences = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var preferenceUpdate in request.Preferences)
        {
            var existing = existingPreferences.FirstOrDefault(p => 
                p.NotificationType == preferenceUpdate.NotificationType && 
                p.Channel == preferenceUpdate.Channel);

            if (existing != null)
            {
                // Update existing preference
                existing.IsEnabled = preferenceUpdate.IsEnabled;
                existing.QuietHoursStart = preferenceUpdate.QuietHoursStart;
                existing.QuietHoursEnd = preferenceUpdate.QuietHoursEnd;
                existing.TimeZone = preferenceUpdate.TimeZone;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new preference
                var newPreference = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationType = preferenceUpdate.NotificationType,
                    Channel = preferenceUpdate.Channel,
                    IsEnabled = preferenceUpdate.IsEnabled,
                    QuietHoursStart = preferenceUpdate.QuietHoursStart,
                    QuietHoursEnd = preferenceUpdate.QuietHoursEnd,
                    TimeZone = preferenceUpdate.TimeZone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.NotificationPreferences.Add(newPreference);
                existingPreferences.Add(newPreference);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

        return _mapper.Map<List<NotificationPreferenceDto>>(existingPreferences);
    }

    public async Task<bool> IsNotificationAllowedAsync(string userId, Shared.Common.Enums.NotificationType type, Shared.Common.Enums.NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var preference = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, cancellationToken);

        // If no specific preference found, allow by default
        return preference?.IsEnabled ?? true;
    }

    public async Task<DateTime?> GetOptimalSendTimeAsync(string userId, DateTime requestedTime, CancellationToken cancellationToken = default)
    {
        var preferences = await _context.NotificationPreferences
            .Where(p => p.UserId == userId && p.QuietHoursStart.HasValue && p.QuietHoursEnd.HasValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (preferences?.QuietHoursStart == null || preferences.QuietHoursEnd == null)
        {
            return requestedTime; // No quiet hours configured
        }

        var userTimeZone = !string.IsNullOrEmpty(preferences.TimeZone) 
            ? TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZone)
            : TimeZoneInfo.Utc;

        var userTime = TimeZoneInfo.ConvertTimeFromUtc(requestedTime, userTimeZone);
        var currentTime = userTime.TimeOfDay;

        var quietStart = preferences.QuietHoursStart.Value;
        var quietEnd = preferences.QuietHoursEnd.Value;

        // Check if requested time falls within quiet hours
        bool isInQuietHours;
        if (quietStart < quietEnd)
        {
            // Quiet hours don't cross midnight (e.g., 22:00 - 08:00 next day)
            isInQuietHours = currentTime >= quietStart && currentTime <= quietEnd;
        }
        else
        {
            // Quiet hours cross midnight (e.g., 22:00 - 08:00 next day)
            isInQuietHours = currentTime >= quietStart || currentTime <= quietEnd;
        }

        if (!isInQuietHours)
        {
            return requestedTime; // Not in quiet hours, send at requested time
        }

        // Adjust to end of quiet hours
        var adjustedTime = userTime.Date.Add(quietEnd);
        if (quietStart > quietEnd && currentTime >= quietStart)
        {
            // Quiet hours cross midnight and we're after start time
            adjustedTime = adjustedTime.AddDays(1);
        }

        // Convert back to UTC
        return TimeZoneInfo.ConvertTimeToUtc(adjustedTime, userTimeZone);
    }
}