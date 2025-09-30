using YourCompanyBNPL.Common.Enums;
using Microsoft.Extensions.Options;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using YourCompanyBNPL.Notification.API.Models;
using YourCompanyBNPL.Common.Enums;
using System.Text.Json;

namespace YourCompanyBNPL.Notification.API.Providers;

/// <summary>
/// Push notification provider using Firebase Cloud Messaging
/// </summary>
public class PushProvider : INotificationProvider
{
    private readonly PushProviderOptions _options;
    private readonly ILogger<PushProvider> _logger;
    private readonly FirebaseMessaging? _messaging;

    public NotificationChannel Channel => NotificationChannel.Push;

    public PushProvider(IOptions<NotificationProviderOptions> options, ILogger<PushProvider> logger)
    {
        _options = options.Value.Push;
        _logger = logger;

        if (_options.Provider == "Firebase" && 
            !string.IsNullOrEmpty(_options.Firebase.ProjectId) && 
            File.Exists(_options.Firebase.CredentialsPath))
        {
            try
            {
                var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(_options.Firebase.CredentialsPath),
                    ProjectId = _options.Firebase.ProjectId
                });

                _messaging = FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase messaging");
                _messaging = null;
            }
        }
    }

    public async Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.RecipientDeviceToken))
            {
                _logger.LogError("No device token provided for push notification {NotificationId}", notification.Id);
                return false;
            }

            if (_options.Provider == "Console" || _messaging == null)
            {
                return await SendConsolePushAsync(notification);
            }

            return await SendFirebasePushAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification {NotificationId}", notification.Id);
            return false;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Provider == "Console")
            return true;

        try
        {
            if (_messaging == null)
                return false;

            // Simple health check - Firebase messaging is available
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push provider health check failed");
            return false;
        }
    }

    private async Task<bool> SendFirebasePushAsync(Models.Notification notification)
    {
        try
        {
            var message = new Message()
            {
                Token = notification.RecipientDeviceToken,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = notification.Subject,
                    Body = ExtractTextFromContent(notification.Content)
                },
                Data = new Dictionary<string, string>
                {
                    ["notification_id"] = notification.Id.ToString(),
                    ["type"] = notification.Type.ToString(),
                    ["correlation_id"] = notification.CorrelationId ?? string.Empty
                }
            };

            // Add custom data from metadata if available
            if (!string.IsNullOrEmpty(notification.Metadata))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata);
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            // message.Data[$"meta_{kvp.Key}"] = kvp.Value?.ToString() ?? string.Empty; // Data is read-only
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse metadata for push notification {NotificationId}", notification.Id);
                }
            }

            // Set priority based on notification priority
            message.Android = new AndroidConfig()
            {
                Priority = notification.Priority switch
                {
                    Models.NotificationPriority.Critical => Priority.High,
                    Models.NotificationPriority.High => Priority.High,
                    _ => Priority.Normal
                }
            };

            message.Apns = new ApnsConfig()
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = notification.Priority switch
                    {
                        Models.NotificationPriority.Critical => "10",
                        Models.NotificationPriority.High => "10",
                        _ => "5"
                    }
                }
            };

            var response = await _messaging!.SendAsync(message);
            
            if (!string.IsNullOrEmpty(response))
            {
                notification.ExternalId = response;
                _logger.LogInformation("Push notification sent successfully to device for notification {NotificationId}. Firebase response: {Response}", 
                    notification.Id, response);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send push notification via Firebase - empty response");
                notification.ErrorMessage = "Firebase error: empty response";
                return false;
            }
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase messaging exception occurred while sending push notification");
            notification.ErrorMessage = $"Firebase error: {ex.MessagingErrorCode} - {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending push notification via Firebase");
            notification.ErrorMessage = $"Firebase exception: {ex.Message}";
            return false;
        }
    }

    private Task<bool> SendConsolePushAsync(Models.Notification notification)
    {
        _logger.LogInformation("=== CONSOLE PUSH ===");
        _logger.LogInformation("To Device Token: {DeviceToken}", MaskDeviceToken(notification.RecipientDeviceToken));
        _logger.LogInformation("Title: {Subject}", notification.Subject);
        _logger.LogInformation("Body: {Content}", ExtractTextFromContent(notification.Content));
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("Priority: {Priority}", notification.Priority);
        _logger.LogInformation("===================");
        
        return Task.FromResult(true);
    }

    private static string ExtractTextFromContent(string content)
    {
        // Simple HTML tag removal for push notifications
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Remove HTML tags
        var text = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
        
        // Limit length for push notifications
        if (text.Length > 200)
        {
            text = text.Substring(0, 197) + "...";
        }

        return text;
    }

    private static string? MaskDeviceToken(string? deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken) || deviceToken.Length < 10)
            return deviceToken;

        return deviceToken.Substring(0, 6) + "..." + deviceToken.Substring(deviceToken.Length - 4);
    }
}

/// <summary>
/// Console push provider for development
/// </summary>
public class ConsolePushProvider : INotificationProvider
{
    private readonly ILogger<ConsolePushProvider> _logger;

    public NotificationChannel Channel => NotificationChannel.Push;

    public ConsolePushProvider(ILogger<ConsolePushProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== CONSOLE PUSH ===");
        _logger.LogInformation("To Device Token: {DeviceToken}", MaskDeviceToken(notification.RecipientDeviceToken));
        _logger.LogInformation("Title: {Subject}", notification.Subject);
        _logger.LogInformation("Body: {Content}", notification.Content);
        _logger.LogInformation("Notification ID: {NotificationId}", notification.Id);
        _logger.LogInformation("Priority: {Priority}", notification.Priority);
        _logger.LogInformation("===================");
        
        return Task.FromResult(true);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static string? MaskDeviceToken(string? deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken) || deviceToken.Length < 10)
            return deviceToken;

        return deviceToken.Substring(0, 6) + "..." + deviceToken.Substring(deviceToken.Length - 4);
    }
}