using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Service for payment reminder notifications
/// </summary>
public interface IPaymentReminderService
{
    Task<ApiResponse<int>> ProcessPaymentRemindersAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> SendReminderAsync(Guid installmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessOverdueNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SendPaymentRemindersAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SendOverdueNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SendBNPLInstallmentRemindersAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SendCustomerEngagementNotificationsAsync(CancellationToken cancellationToken = default);
}