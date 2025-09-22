using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// SMS service implementation using Twilio
/// </summary>
public class SmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly ILogger<SmsService> _logger;
    private readonly bool _isInitialized;

    public SmsService(IOptions<SmsSettings> settings, ILogger<SmsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.TwilioAccountSid) && !string.IsNullOrEmpty(_settings.TwilioAuthToken))
        {
            TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);
            _isInitialized = true;
        }
        else
        {
            _isInitialized = false;
        }
    }

    public async Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendSmsAsync(
        string to, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isInitialized)
            {
                // Console mode for development
                _logger.LogInformation("=== SMS (Console Mode) ===");
                _logger.LogInformation("To: {To}", to);
                _logger.LogInformation("Message: {Message}", message);
                _logger.LogInformation("=====================");
                return (true, Guid.NewGuid().ToString(), null);
            }

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_settings.FromNumber),
                to: new PhoneNumber(to)
            );

            if (messageResource.ErrorCode == null)
            {
                _logger.LogInformation("SMS sent successfully to {To}. Twilio SID: {Sid}", to, messageResource.Sid);
                return (true, messageResource.Sid, null);
            }
            else
            {
                _logger.LogError("Failed to send SMS via Twilio. Error: {ErrorCode} - {ErrorMessage}", 
                    messageResource.ErrorCode, messageResource.ErrorMessage);
                return (false, null, $"Twilio error: {messageResource.ErrorCode} - {messageResource.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", to);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkSmsAsync(
        List<(string To, string Message)> messages, 
        CancellationToken cancellationToken = default)
    {
        var externalIds = new List<string>();
        var errors = new List<string>();

        foreach (var sms in messages)
        {
            var result = await SendSmsAsync(sms.To, sms.Message, cancellationToken);
            if (result.Success && result.ExternalId != null)
            {
                externalIds.Add(result.ExternalId);
            }
            else if (result.ErrorMessage != null)
            {
                errors.Add($"{sms.To}: {result.ErrorMessage}");
            }
        }

        var success = errors.Count == 0;
        var errorMessage = errors.Count > 0 ? string.Join("; ", errors) : null;

        return (success, externalIds, errorMessage);
    }

    public Task<bool> ValidatePhoneNumberAsync(string phoneNumber)
    {
        try
        {
            // Basic phone number validation
            var regex = new Regex(@"^\+?[1-9]\d{1,14}$");
            return Task.FromResult(regex.IsMatch(phoneNumber));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// SMS service settings
/// </summary>
public class SmsSettings
{
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}