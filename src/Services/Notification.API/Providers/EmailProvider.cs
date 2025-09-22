using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using RivertyBNPL.Services.Notification.API.Models;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Providers;

/// <summary>
/// Email notification provider using SendGrid
/// </summary>
public class EmailProvider : INotificationProvider
{
    private readonly EmailProviderOptions _options;
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<EmailProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public EmailProvider(IOptions<NotificationProviderOptions> options, ILogger<EmailProvider> logger)
    {
        _options = options.Value.Email;
        _logger = logger;
        
        if (_options.Provider == "SendGrid" && !string.IsNullOrEmpty(_options.SendGrid.ApiKey))
        {
            _sendGridClient = new SendGridClient(_options.SendGrid.ApiKey);
        }
        else
        {
            // Use console provider for development
            _sendGridClient = null!;
        }
    }

    public async Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_options.Provider == "Console" || _sendGridClient == null)
            {
                return await SendConsoleEmailAsync(notification);
            }

            return await SendSendGridEmailAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification {NotificationId}", notification.Id);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Provider == "Console")
            return true;

        try
        {
            // Simple health check - verify API key is valid
            if (_sendGridClient == null || string.IsNullOrEmpty(_options.SendGrid.ApiKey))
                return false;

            // You could make a simple API call here to verify connectivity
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email provider health check failed");
            return false;
        }
    }

    private async Task<bool> SendSendGridEmailAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        var from = new EmailAddress(_options.SendGrid.FromEmail, _options.SendGrid.FromName);
        var to = new EmailAddress(notification.RecipientEmail);
        
        var msg = MailHelper.CreateSingleEmail(from, to, notification.Subject, notification.Content, notification.Content);
        
        // Add tracking
        msg.SetClickTracking(true, true);
        msg.SetOpenTracking(true);
        
        // Add custom args for tracking
        msg.AddCustomArg("notification_id", notification.Id.ToString());
        if (!string.IsNullOrEmpty(notification.CorrelationId))
        {
            msg.AddCustomArg("correlation_id", notification.CorrelationId);
        }

        var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            // Extract message ID from response headers for tracking
            if (response.Headers.TryGetValues("X-Message-Id", out var messageIds))
            {
                notification.ExternalId = messageIds.FirstOrDefault();
            }
            
            _logger.LogInformation("Email sent successfully to {Email} for notification {NotificationId}", 
                notification.RecipientEmail, notification.Id);
            return true;
        }
        else
        {
            var errorBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("Failed to send email via SendGrid. Status: {StatusCode}, Body: {Body}", 
                response.StatusCode, errorBody);
            notification.ErrorMessage = $"SendGrid error: {response.StatusCode} - {errorBody}";
            return false;
        }
    }

    private Task<bool> SendConsoleEmailAsync(Models.Notification notification)
    {
        _logger.LogInformation("=== CONSOLE EMAIL ===");
        _logger.LogInformation("To: {Email}", notification.RecipientEmail);
        _logger.LogInformation("Subject: {Subject}", notification.Subject);
        _logger.LogInformation("Content: {Content}", notification.Content);
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("===================");
        
        return Task.FromResult(true);
    }
}

/// <summary>
/// Console email provider for development
/// </summary>
public class ConsoleEmailProvider : INotificationProvider
{
    private readonly ILogger<ConsoleEmailProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public ConsoleEmailProvider(ILogger<ConsoleEmailProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== CONSOLE EMAIL ===");
        _logger.LogInformation("To: {Email}", notification.RecipientEmail);
        _logger.LogInformation("Subject: {Subject}", notification.Subject);
        _logger.LogInformation("Content: {Content}", notification.Content);
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("===================");
        
        return Task.FromResult(true);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}