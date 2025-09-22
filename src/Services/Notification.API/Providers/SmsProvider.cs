using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using RivertyBNPL.Services.Notification.API.Models;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Providers;

/// <summary>
/// SMS notification provider using Twilio
/// </summary>
public class SmsProvider : INotificationProvider
{
    private readonly SmsProviderOptions _options;
    private readonly ILogger<SmsProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.SMS;

    public SmsProvider(IOptions<NotificationProviderOptions> options, ILogger<SmsProvider> logger)
    {
        _options = options.Value.SMS;
        _logger = logger;

        if (_options.Provider == "Twilio" && 
            !string.IsNullOrEmpty(_options.Twilio.AccountSid) && 
            !string.IsNullOrEmpty(_options.Twilio.AuthToken))
        {
            TwilioClient.Init(_options.Twilio.AccountSid, _options.Twilio.AuthToken);
        }
    }

    public async Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.RecipientPhone))
            {
                _logger.LogError("No phone number provided for SMS notification {NotificationId}", notification.Id);
                return false;
            }

            if (_options.Provider == "Console" || string.IsNullOrEmpty(_options.Twilio.AccountSid))
            {
                return await SendConsoleSmsAsync(notification);
            }

            return await SendTwilioSmsAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS notification {NotificationId}", notification.Id);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Provider == "Console")
            return true;

        try
        {
            if (string.IsNullOrEmpty(_options.Twilio.AccountSid) || string.IsNullOrEmpty(_options.Twilio.AuthToken))
                return false;

            // Simple health check - verify account is accessible
            var account = await AccountResource.FetchAsync();
            return account != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS provider health check failed");
            return false;
        }
    }

    private async Task<bool> SendTwilioSmsAsync(Models.Notification notification)
    {
        try
        {
            var message = await MessageResource.CreateAsync(
                body: notification.Content,
                from: new PhoneNumber(_options.Twilio.FromNumber),
                to: new PhoneNumber(notification.RecipientPhone!)
            );

            if (message.ErrorCode == null)
            {
                notification.ExternalId = message.Sid;
                _logger.LogInformation("SMS sent successfully to {Phone} for notification {NotificationId}. Twilio SID: {Sid}", 
                    notification.RecipientPhone, notification.Id, message.Sid);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send SMS via Twilio. Error: {ErrorCode} - {ErrorMessage}", 
                    message.ErrorCode, message.ErrorMessage);
                notification.ErrorMessage = $"Twilio error: {message.ErrorCode} - {message.ErrorMessage}";
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS via Twilio");
            notification.ErrorMessage = $"Twilio exception: {ex.Message}";
            return false;
        }
    }

    private Task<bool> SendConsoleSmsAsync(Models.Notification notification)
    {
        _logger.LogInformation("=== CONSOLE SMS ===");
        _logger.LogInformation("To: {Phone}", notification.RecipientPhone);
        _logger.LogInformation("Content: {Content}", notification.Content);
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("==================");
        
        return Task.FromResult(true);
    }
}

/// <summary>
/// Console SMS provider for development
/// </summary>
public class ConsoleSmsProvider : INotificationProvider
{
    private readonly ILogger<ConsoleSmsProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.SMS;

    public ConsoleSmsProvider(ILogger<ConsoleSmsProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== CONSOLE SMS ===");
        _logger.LogInformation("To: {Phone}", notification.RecipientPhone);
        _logger.LogInformation("Content: {Content}", notification.Content);
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("==================");
        
        return Task.FromResult(true);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}