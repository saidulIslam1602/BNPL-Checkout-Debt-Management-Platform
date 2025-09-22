using RivertyBNPL.Payment.API.DTOs;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Payment.API.Services;

/// <summary>
/// Interface for payment processing operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a new payment transaction
    /// </summary>
    Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a payment transaction
    /// </summary>
    Task<ApiResponse<PaymentResponse>> ProcessPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets payment by ID
    /// </summary>
    Task<ApiResponse<PaymentResponse>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches payments with filtering and pagination
    /// </summary>
    Task<PagedApiResponse<PaymentResponse>> SearchPaymentsAsync(PaymentSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a pending payment
    /// </summary>
    Task<ApiResponse<PaymentResponse>> CancelPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a refund for a completed payment
    /// </summary>
    Task<ApiResponse<RefundResponse>> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets payment analytics for a merchant
    /// </summary>
    Task<ApiResponse<PaymentAnalytics>> GetPaymentAnalyticsAsync(Guid merchantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for BNPL-specific operations
/// </summary>
public interface IBNPLService
{
    /// <summary>
    /// Calculates BNPL payment plan options
    /// </summary>
    Task<ApiResponse<BNPLCalculationResponse>> CalculateBNPLPlanAsync(BNPLCalculationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a BNPL payment plan
    /// </summary>
    Task<ApiResponse<BNPLPlanSummary>> CreateBNPLPlanAsync(Guid paymentId, BNPLCalculationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets BNPL plan by ID
    /// </summary>
    Task<ApiResponse<BNPLPlanSummary>> GetBNPLPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets customer's BNPL plans
    /// </summary>
    Task<PagedApiResponse<BNPLPlanSummary>> GetCustomerBNPLPlansAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes an installment payment
    /// </summary>
    Task<ApiResponse<InstallmentResponse>> ProcessInstallmentAsync(ProcessInstallmentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets overdue installments for collection processing
    /// </summary>
    Task<PagedApiResponse<InstallmentResponse>> GetOverdueInstallmentsAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates installment status to overdue and calculates late fees
    /// </summary>
    Task<ApiResponse> ProcessOverdueInstallmentsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets individual installment by ID
    /// </summary>
    Task<ApiResponse<InstallmentResponse>> GetInstallmentAsync(Guid installmentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for settlement operations
/// </summary>
public interface ISettlementService
{
    /// <summary>
    /// Creates settlements for eligible transactions
    /// </summary>
    Task<ApiResponse<List<SettlementSummary>>> CreateSettlementsAsync(DateTime settlementDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets merchant settlements
    /// </summary>
    Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes pending settlements
    /// </summary>
    Task<ApiResponse> ProcessPendingSettlementsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement by ID
    /// </summary>
    Task<ApiResponse<SettlementSummary>> GetSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default);
}