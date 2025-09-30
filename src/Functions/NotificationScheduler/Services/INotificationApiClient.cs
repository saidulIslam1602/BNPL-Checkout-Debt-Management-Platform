using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Client for communicating with Notification API
/// </summary>
public interface INotificationApiClient
{
    Task<ApiResponse> SendNotificationAsync(object notificationRequest, CancellationToken cancellationToken = default);
    Task<ApiResponse> SendBulkNotificationAsync(object bulkRequest, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetNotificationStatusAsync(Guid notificationId, CancellationToken cancellationToken = default);
}