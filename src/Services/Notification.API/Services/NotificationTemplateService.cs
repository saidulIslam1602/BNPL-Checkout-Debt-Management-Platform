using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Notification.API.Data;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Models;
using Microsoft.EntityFrameworkCore;

namespace YourCompanyBNPL.Notification.API.Services;

/// <summary>
/// Service for managing notification templates
/// </summary>
public interface INotificationTemplateService
{
    Task<NotificationTemplateResponse?> GetTemplateAsync(string templateId);
    Task<List<NotificationTemplateResponse>> GetAllTemplatesAsync();
    Task<NotificationTemplateResponse> CreateTemplateAsync(CreateNotificationTemplateRequest request);
}

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        NotificationDbContext context,
        ILogger<NotificationTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationTemplateResponse?> GetTemplateAsync(string templateId)
    {
        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Id == Guid.Parse(templateId));

        if (template == null) return null;

        return new NotificationTemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Content = template.Content,
            Subject = template.Subject
        };
    }

    public async Task<List<NotificationTemplateResponse>> GetAllTemplatesAsync()
    {
        var templates = await _context.NotificationTemplates.ToListAsync();
        
        return templates.Select(t => new NotificationTemplateResponse
        {
            Id = t.Id,
            Name = t.Name,
            Content = t.Content,
            Subject = t.Subject
        }).ToList();
    }

    public async Task<NotificationTemplateResponse> CreateTemplateAsync(CreateNotificationTemplateRequest request)
    {
        var template = new NotificationTemplate
        {
            Name = request.Name,
            Content = request.Content,
            Subject = request.Subject
        };

        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        return new NotificationTemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Content = template.Content,
            Subject = template.Subject
        };
    }
}