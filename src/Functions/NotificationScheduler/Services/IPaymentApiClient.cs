using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Client for communicating with Payment API
/// </summary>
public interface IPaymentApiClient
{
    Task<ApiResponse<object>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetInstallmentAsync(Guid installmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<object>>> GetOverdueInstallmentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}