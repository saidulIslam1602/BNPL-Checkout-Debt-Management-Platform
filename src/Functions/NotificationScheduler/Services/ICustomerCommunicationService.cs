using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Service for customer communication management
/// </summary>
public interface ICustomerCommunicationService
{
    Task<ApiResponse> SendWelcomeMessageAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ApiResponse> SendPaymentConfirmationAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ApiResponse> SendAccountUpdateAsync(Guid customerId, string updateType, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> SendBulkCommunicationsAsync(List<Guid> customerIds, string messageType, CancellationToken cancellationToken = default);
}