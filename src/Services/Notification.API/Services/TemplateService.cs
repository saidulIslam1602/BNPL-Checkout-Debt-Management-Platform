using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Notification.API.DTOs;

namespace YourCompanyBNPL.Notification.API.Services;

public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<TemplateRenderResult>> RenderTemplateAsync(Guid templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Rendering template {TemplateId}", templateId);
            
            // TODO: Implement actual template rendering logic
            await Task.CompletedTask;
            
            var result = new TemplateRenderResult
            {
                Subject = $"Subject for template {templateId}",
                HtmlContent = $"<html><body>Rendered HTML template {templateId}</body></html>",
                TextContent = $"Rendered text template {templateId}",
                SmsContent = $"SMS: {templateId}",
                PushContent = $"Push: {templateId}"
            };
            
            return ApiResponse<TemplateRenderResult>.SuccessResult(result, "Template rendered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateId}", templateId);
            return ApiResponse<TemplateRenderResult>.ErrorResult($"Failed to render template: {ex.Message}");
        }
    }
}
