using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.PaymentCollection.Functions.Services;

/// <summary>
/// Service for automatic payment collection
/// </summary>
public interface IAutomaticCollectionService
{
    Task<ApiResponse<int>> ProcessAutomaticCollectionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessOverduePaymentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> ProcessSinglePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessBNPLInstallmentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessRecurringPaymentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessDirectDebitCollectionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RetryFailedBNPLCollectionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RetryFailedRecurringCollectionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> RetryFailedDirectDebitCollectionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GenerateMonthlyCollectionReportsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ArchiveOldPaymentDataAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GenerateRegulatoryReportsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for processing overdue payments
/// </summary>
public interface IOverdueProcessingService
{
    Task<ApiResponse<int>> ProcessOverdueInstallmentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> SendOverdueNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> UpdateCollectionStatusAsync(Guid installmentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> MarkPaymentsAsOverdueAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ApplyLateFeesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> UpdateCustomerRiskProfilesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> TriggerCollectionWorkflowsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> UpdateCustomerCreditLimitsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for settlement processing
/// </summary>
public interface ISettlementProcessingService
{
    Task<ApiResponse<int>> ProcessPendingSettlementsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> ProcessMerchantSettlementAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<ApiResponse> ValidateSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> CreateWeeklySettlementsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessSettlementPaymentsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GenerateSettlementReportsAsync(CancellationToken cancellationToken = default);
}