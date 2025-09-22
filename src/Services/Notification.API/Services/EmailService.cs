using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// Email service implementation using SendGrid
/// </summary>
public class EmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        if (!string.IsNullOrEmpty(_settings.SendGridApiKey))
        {
            _sendGridClient = new SendGridClient(_settings.SendGridApiKey);
        }
        else
        {
            _sendGridClient = null!;
        }
    }

    public async Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendEmailAsync(
        string to, 
        string subject, 
        string htmlContent, 
        string? textContent = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sendGridClient == null)
            {
                // Console mode for development
                _logger.LogInformation("=== EMAIL (Console Mode) ===");
                _logger.LogInformation("To: {To}", to);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Content: {Content}", htmlContent);
                _logger.LogInformation("========================");
                return (true, Guid.NewGuid().ToString(), null);
            }

            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var toEmail = new EmailAddress(to);
            
            var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, textContent ?? htmlContent, htmlContent);
            
            // Add tracking
            msg.SetClickTracking(true, true);
            msg.SetOpenTracking(true);
            
            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values) 
                    ? values.FirstOrDefault() 
                    : null;
                
                _logger.LogInformation("Email sent successfully to {To}", to);
                return (true, messageId, null);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send email via SendGrid. Status: {StatusCode}, Body: {Body}", 
                    response.StatusCode, errorBody);
                return (false, null, $"SendGrid error: {response.StatusCode} - {errorBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkEmailAsync(
        List<(string To, string Subject, string HtmlContent, string? TextContent)> emails, 
        CancellationToken cancellationToken = default)
    {
        var externalIds = new List<string>();
        var errors = new List<string>();

        foreach (var email in emails)
        {
            var result = await SendEmailAsync(email.To, email.Subject, email.HtmlContent, email.TextContent, cancellationToken);
            if (result.Success && result.ExternalId != null)
            {
                externalIds.Add(result.ExternalId);
            }
            else if (result.ErrorMessage != null)
            {
                errors.Add($"{email.To}: {result.ErrorMessage}");
            }
        }

        var success = errors.Count == 0;
        var errorMessage = errors.Count > 0 ? string.Join("; ", errors) : null;

        return (success, externalIds, errorMessage);
    }

    public Task<bool> ValidateEmailAsync(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return Task.FromResult(addr.Address == email);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// Email service settings
/// </summary>
public class EmailSettings
{
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}