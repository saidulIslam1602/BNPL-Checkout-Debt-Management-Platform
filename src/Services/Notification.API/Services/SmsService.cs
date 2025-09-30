using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Notification.API.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, string? externalId, string? errorMessage)> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            // TODO: Implement actual SMS sending logic (Twilio, AWS SNS, etc.)
            await Task.CompletedTask;
            
            _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
            
            return (true, Guid.NewGuid().ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return (false, null, ex.Message);
        }
    }
}