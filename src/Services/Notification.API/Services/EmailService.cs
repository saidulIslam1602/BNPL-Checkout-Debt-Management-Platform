using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Notification.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, string? externalId, string? errorMessage)> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement actual email sending logic (SendGrid, SMTP, etc.)
            await Task.CompletedTask;
            
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            
            return (true, Guid.NewGuid().ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return (false, null, ex.Message);
        }
    }
}