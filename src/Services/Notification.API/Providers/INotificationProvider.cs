using RivertyBNPL.Services.Notification.API.Models;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Providers;

/// <summary>
/// Interface for notification providers
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// The notification channel this provider handles
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Send a notification
    /// </summary>
    Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating notification providers
/// </summary>
public interface INotificationProviderFactory
{
    /// <summary>
    /// Get provider for the specified channel
    /// </summary>
    INotificationProvider GetProvider(NotificationChannel channel);

    /// <summary>
    /// Get all available providers
    /// </summary>
    IEnumerable<INotificationProvider> GetAllProviders();
}

/// <summary>
/// Configuration for notification providers
/// </summary>
public class NotificationProviderOptions
{
    public EmailProviderOptions Email { get; set; } = new();
    public SmsProviderOptions SMS { get; set; } = new();
    public PushProviderOptions Push { get; set; } = new();
}

/// <summary>
/// Email provider configuration
/// </summary>
public class EmailProviderOptions
{
    public string Provider { get; set; } = "SendGrid";
    public SendGridOptions SendGrid { get; set; } = new();
}

/// <summary>
/// SendGrid configuration
/// </summary>
public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>
/// SMS provider configuration
/// </summary>
public class SmsProviderOptions
{
    public string Provider { get; set; } = "Twilio";
    public TwilioOptions Twilio { get; set; } = new();
}

/// <summary>
/// Twilio configuration
/// </summary>
public class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>
/// Push notification provider configuration
/// </summary>
public class PushProviderOptions
{
    public string Provider { get; set; } = "Firebase";
    public FirebaseOptions Firebase { get; set; } = new();
}

/// <summary>
/// Firebase configuration
/// </summary>
public class FirebaseOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}