using Microsoft.Extensions.Options;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// Push notification service implementation
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly PushSettings _settings;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IOptions<PushSettings> settings, ILogger<PushNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendPushNotificationAsync(
        string deviceToken, 
        string title, 
        string body, 
        Dictionary<string, object>? data = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Console mode for development (Firebase integration would go here in production)
            _logger.LogInformation("=== PUSH NOTIFICATION (Console Mode) ===");
            _logger.LogInformation("Device Token: {DeviceToken}", MaskDeviceToken(deviceToken));
            _logger.LogInformation("Title: {Title}", title);
            _logger.LogInformation("Body: {Body}", body);
            if (data != null && data.Count > 0)
            {
                _logger.LogInformation("Data: {Data}", System.Text.Json.JsonSerializer.Serialize(data));
            }
            _logger.LogInformation("=====================================");

            // Simulate async operation
            await Task.Delay(100, cancellationToken);

            return (true, Guid.NewGuid().ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to device token {DeviceToken}", MaskDeviceToken(deviceToken));
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkPushNotificationAsync(
        List<(string DeviceToken, string Title, string Body, Dictionary<string, object>? Data)> notifications, 
        CancellationToken cancellationToken = default)
    {
        var externalIds = new List<string>();
        var errors = new List<string>();

        foreach (var notification in notifications)
        {
            var result = await SendPushNotificationAsync(
                notification.DeviceToken, 
                notification.Title, 
                notification.Body, 
                notification.Data, 
                cancellationToken);
            
            if (result.Success && result.ExternalId != null)
            {
                externalIds.Add(result.ExternalId);
            }
            else if (result.ErrorMessage != null)
            {
                errors.Add($"{MaskDeviceToken(notification.DeviceToken)}: {result.ErrorMessage}");
            }
        }

        var success = errors.Count == 0;
        var errorMessage = errors.Count > 0 ? string.Join("; ", errors) : null;

        return (success, externalIds, errorMessage);
    }

    private static string MaskDeviceToken(string deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken) || deviceToken.Length < 10)
            return deviceToken;

        return deviceToken.Substring(0, 6) + "..." + deviceToken.Substring(deviceToken.Length - 4);
    }
}

/// <summary>
/// Push notification service settings
/// </summary>
public class PushSettings
{
    public string FirebaseProjectId { get; set; } = string.Empty;
    public string FirebaseCredentialsPath { get; set; } = string.Empty;
}