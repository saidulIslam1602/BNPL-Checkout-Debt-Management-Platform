using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Service for notification queue management
/// </summary>
public interface INotificationQueueService
{
    Task<ApiResponse> EnqueueNotificationAsync(object notification, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<object>>> DequeueNotificationsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GetQueueLengthAsync(CancellationToken cancellationToken = default);
}