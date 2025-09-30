using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Payment.API.Services;

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
    /// Processes an approved payment transaction
    /// </summary>
    Task<ApiResponse<PaymentResponse>> ProcessApprovedPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    
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
    Task<ApiResponse<List<SettlementSummary>>> CreateSettlementsAsync(CreateSettlementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets merchant settlements with filtering
    /// </summary>
    Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, SettlementFilterRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all settlements with advanced filtering
    /// </summary>
    Task<PagedApiResponse<SettlementSummary>> GetAllSettlementsAsync(SettlementFilterRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed settlement information
    /// </summary>
    Task<ApiResponse<SettlementDetails>> GetSettlementDetailsAsync(Guid settlementId, bool includeTransactions = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes pending settlements with options
    /// </summary>
    Task<ApiResponse<SettlementProcessingResult>> ProcessPendingSettlementsAsync(ProcessSettlementsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a specific settlement
    /// </summary>
    Task<ApiResponse<SettlementSummary>> ProcessSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a pending settlement
    /// </summary>
    Task<ApiResponse<SettlementSummary>> CancelSettlementAsync(Guid settlementId, CancelSettlementRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retries a failed settlement
    /// </summary>
    Task<ApiResponse<SettlementSummary>> RetrySettlementAsync(Guid settlementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement analytics and reports
    /// </summary>
    Task<ApiResponse<SettlementAnalytics>> GetSettlementAnalyticsAsync(SettlementAnalyticsRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports settlement data
    /// </summary>
    Task<ApiResponse<byte[]>> ExportSettlementsAsync(SettlementExportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement reconciliation report
    /// </summary>
    Task<ApiResponse<SettlementReconciliationReport>> GetReconciliationReportAsync(SettlementReconciliationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Configures settlement schedule for a merchant
    /// </summary>
    Task<ApiResponse<SettlementScheduleConfig>> ConfigureSettlementScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement schedule for a merchant
    /// </summary>
    Task<ApiResponse<SettlementScheduleConfig>> GetSettlementScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates settlement status (internal use)
    /// </summary>
    Task<ApiResponse> UpdateSettlementStatusAsync(Guid settlementId, SettlementStatus status, string? bankTransactionId = null, string? correlationId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates settlement eligibility
    /// </summary>
    Task<ApiResponse<bool>> ValidateSettlementEligibilityAsync(Guid merchantId, CancellationToken cancellationToken = default);
}