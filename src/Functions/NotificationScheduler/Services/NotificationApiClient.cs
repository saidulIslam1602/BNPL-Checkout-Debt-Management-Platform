using YourCompanyBNPL.Common.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Implementation of notification API client
/// </summary>
public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiClient> _logger;

    public NotificationApiClient(HttpClient httpClient, ILogger<NotificationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse> SendNotificationAsync(object notificationRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(notificationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/notifications", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Notification sent successfully");
                return ApiResponse.SuccessResponse("Notification sent successfully");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send notification. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return ApiResponse.ErrorResult($"Failed to send notification: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return ApiResponse.ErrorResult($"Error sending notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse> SendBulkNotificationAsync(object bulkRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(bulkRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/notifications/bulk", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bulk notifications sent successfully");
                return ApiResponse.SuccessResponse("Bulk notifications sent successfully");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send bulk notifications. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return ApiResponse.ErrorResult($"Failed to send bulk notifications: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
            return ApiResponse.ErrorResult($"Error sending bulk notifications: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> GetNotificationStatusAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/notifications/{notificationId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var notification = JsonSerializer.Deserialize<object>(content);
                
                _logger.LogInformation("Retrieved notification status for {NotificationId}", notificationId);
                return ApiResponse<object>.SuccessResult(notification, "Notification status retrieved");
            }
            else
            {
                _logger.LogError("Failed to get notification status. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<object>.ErrorResult($"Failed to get notification status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification status for {NotificationId}", notificationId);
            return ApiResponse<object>.ErrorResult($"Error getting notification status: {ex.Message}");
        }
    }
}