using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Service for overdue notification handling
/// </summary>
public interface IOverdueNotificationService
{
    Task<ApiResponse<int>> ProcessOverdueNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> SendOverdueNotificationAsync(Guid installmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> EscalateOverdueAccountsAsync(CancellationToken cancellationToken = default);
}