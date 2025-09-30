using YourCompanyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.DTOs;

/// <summary>
/// Request DTO for creating a new payment
/// </summary>
public class CreatePaymentRequest
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal Amount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    public BNPLPlanType? BNPLPlanType { get; set; }
    
    public int? InstallmentCount { get; set; }
    
    [MaxLength(100)]
    public string? OrderReference { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
    
    // BNPL specific fields
    public bool EnableBNPL { get; set; } = false;
    
    public decimal? DownPaymentAmount { get; set; }
    
    public DateTime? FirstInstallmentDate { get; set; }
}

/// <summary>
/// Response DTO for payment operations
/// </summary>
public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid MerchantId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionType TransactionType { get; set; }
    public string? OrderReference { get; set; }
    public string? Description { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Related data
    public CustomerSummary? Customer { get; set; }
    public MerchantSummary? Merchant { get; set; }
    public BNPLPlanSummary? BNPLPlan { get; set; }
}

/// <summary>
/// Request DTO for BNPL payment calculation
/// </summary>
public class BNPLCalculationRequest
{
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal Amount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public BNPLPlanType PlanType { get; set; }
    
    public int? CustomInstallmentCount { get; set; }
    
    public decimal? DownPaymentAmount { get; set; }
    
    public DateTime? FirstInstallmentDate { get; set; }
    
    public Guid? CustomerId { get; set; } // For personalized rates
}

/// <summary>
/// Response DTO for BNPL payment calculation
/// </summary>
public class BNPLCalculationResponse
{
    public decimal TotalAmount { get; set; }
    public Currency Currency { get; set; }
    public BNPLPlanType PlanType { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalFees { get; set; }
    public decimal DownPaymentAmount { get; set; }
    public DateTime FirstInstallmentDate { get; set; }
    public List<InstallmentCalculation> Installments { get; set; } = new();
    public bool IsEligible { get; set; }
    public string? IneligibilityReason { get; set; }
}

/// <summary>
/// Individual installment calculation
/// </summary>
public class InstallmentCalculation
{
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime DueDate { get; set; }
}

/// <summary>
/// Request DTO for processing installment payment
/// </summary>
public class ProcessInstallmentRequest
{
    [Required]
    public Guid InstallmentId { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response DTO for installment operations
/// </summary>
public class InstallmentResponse
{
    public Guid Id { get; set; }
    public Guid BNPLPlanId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysPastDue { get; set; }
    public decimal LateFee { get; set; }
}

/// <summary>
/// BNPL plan summary
/// </summary>
public class BNPLPlanSummary
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public Currency Currency { get; set; }
    public BNPLPlanType PlanType { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal RemainingBalance { get; set; }
    public int RemainingInstallments { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime FirstPaymentDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<InstallmentResponse> Installments { get; set; } = new();
}

/// <summary>
/// Customer summary for payment responses
/// </summary>
public class CustomerSummary
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public CreditRating CreditRating { get; set; }
    public CollectionStatus CollectionStatus { get; set; }
}

/// <summary>
/// Merchant summary for payment responses
/// </summary>
public class MerchantSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
}

/// <summary>
/// Request DTO for payment refund
/// </summary>
public class CreateRefundRequest
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response DTO for refund operations
/// </summary>
public class RefundResponse
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public string Reason { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? RefundTransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for payment search/filtering
/// </summary>
public class PaymentSearchRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? MerchantId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public Currency? Currency { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OrderReference { get; set; }
    public string? TransactionId { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Payment analytics summary
/// </summary>
public class PaymentAnalytics
{
    public decimal TotalVolume { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal SuccessRate { get; set; }
    public Dictionary<PaymentStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<PaymentMethod, decimal> VolumeByMethod { get; set; } = new();
    public Dictionary<Currency, decimal> VolumeByCurrency { get; set; } = new();
    public Dictionary<string, decimal> DailyVolume { get; set; } = new();
    public decimal BNPLVolume { get; set; }
    public int BNPLTransactions { get; set; }
    public decimal BNPLPercentage { get; set; }
}

/// <summary>
/// Settlement summary for merchants
/// </summary>
public class SettlementSummary
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public Currency Currency { get; set; }
    public SettlementStatus Status { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? BankTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Detailed settlement information including transactions
/// </summary>
public class SettlementDetails : SettlementSummary
{
    public MerchantSummary Merchant { get; set; } = new();
    public List<SettlementTransactionSummary> Transactions { get; set; } = new();
    public SettlementMetrics Metrics { get; set; } = new();
    public List<SettlementEvent> Events { get; set; } = new();
}

/// <summary>
/// Settlement transaction summary
/// </summary>
public class SettlementTransactionSummary
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}

/// <summary>
/// Settlement metrics and analytics
/// </summary>
public class SettlementMetrics
{
    public decimal AverageTransactionAmount { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalChargebacks { get; set; }
    public decimal EffectiveFeeRate { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public Dictionary<PaymentMethod, int> TransactionsByMethod { get; set; } = new();
    public Dictionary<string, decimal> FeeBreakdown { get; set; } = new();
}

/// <summary>
/// Settlement event for audit trail
/// </summary>
public class SettlementEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public SettlementStatus FromStatus { get; set; }
    public SettlementStatus ToStatus { get; set; }
    public string? Description { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, object>? EventData { get; set; }
}

/// <summary>
/// Request for creating settlements
/// </summary>
public class CreateSettlementRequest
{
    [Required]
    public DateTime SettlementDate { get; set; }
    
    public List<Guid>? MerchantIds { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal? MinimumAmount { get; set; }
    
    public bool ForceCreate { get; set; } = false;
    
    public bool ProcessImmediately { get; set; } = false;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Request for filtering settlements
/// </summary>
public class SettlementFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public List<SettlementStatus>? Statuses { get; set; }
    public List<Guid>? MerchantIds { get; set; }
    public List<Currency>? Currencies { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? MinAmount { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? MaxAmount { get; set; }
    
    public string? SearchTerm { get; set; }
    
    public SettlementSortBy SortBy { get; set; } = SettlementSortBy.SettlementDate;
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Request for processing settlements
/// </summary>
public class ProcessSettlementsRequest
{
    public List<Guid>? SettlementIds { get; set; }
    public List<Guid>? MerchantIds { get; set; }
    public DateTime? MaxSettlementDate { get; set; }
    public decimal? MinAmount { get; set; }
    public bool ProcessInBatches { get; set; } = true;
    public int BatchSize { get; set; } = 10;
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Settlement processing result
/// </summary>
public class SettlementProcessingResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SettlementProcessingError> Errors { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Settlement processing error
/// </summary>
public class SettlementProcessingError
{
    public Guid SettlementId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime ErrorTime { get; set; }
}

/// <summary>
/// Request for cancelling a settlement
/// </summary>
public class CancelSettlementRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Settlement analytics data
/// </summary>
public class SettlementAnalytics
{
    public SettlementAnalyticsSummary Summary { get; set; } = new();
    public List<SettlementTrendData> Trends { get; set; } = new();
    public List<MerchantSettlementAnalytics> TopMerchants { get; set; } = new();
    public Dictionary<SettlementStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<Currency, decimal> AmountByCurrency { get; set; } = new();
    public List<SettlementPerformanceMetric> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Settlement analytics summary
/// </summary>
public class SettlementAnalyticsSummary
{
    public int TotalSettlements { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal AverageSettlementAmount { get; set; }
    public decimal AverageProcessingTime { get; set; }
    public double SuccessRate { get; set; }
    public int TotalTransactions { get; set; }
}

/// <summary>
/// Settlement trend data
/// </summary>
public class SettlementTrendData
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal Fees { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Merchant settlement analytics
/// </summary>
public class MerchantSettlementAnalytics
{
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public int SettlementCount { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalFees { get; set; }
    public double AverageSettlementAmount { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastSettlementDate { get; set; }
}

/// <summary>
/// Settlement performance metric
/// </summary>
public class SettlementPerformanceMetric
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Request for settlement analytics
/// </summary>
public class SettlementAnalyticsRequest
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public List<Guid>? MerchantIds { get; set; }
    public List<Currency>? Currencies { get; set; }
    public List<SettlementStatus>? Statuses { get; set; }
    
    public AnalyticsGroupBy GroupBy { get; set; } = AnalyticsGroupBy.Day;
    public bool IncludeTrends { get; set; } = true;
    public bool IncludeTopMerchants { get; set; } = true;
    public int TopMerchantsLimit { get; set; } = 10;
}

/// <summary>
/// Settlement export request
/// </summary>
public class SettlementExportRequest
{
    [Required]
    public ExportFormat Format { get; set; }
    
    public SettlementFilterRequest Filter { get; set; } = new();
    
    public List<string> IncludeFields { get; set; } = new();
    
    public bool IncludeTransactions { get; set; } = false;
    
    [MaxLength(100)]
    public string? FileName { get; set; }
}

/// <summary>
/// Settlement reconciliation request
/// </summary>
public class SettlementReconciliationRequest
{
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public List<Guid>? MerchantIds { get; set; }
    
    public bool IncludeDiscrepancies { get; set; } = true;
    
    public decimal VarianceThreshold { get; set; } = 0.01m;
}

/// <summary>
/// Settlement reconciliation report
/// </summary>
public class SettlementReconciliationReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalSettlements { get; set; }
    public int ReconciledSettlements { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal TotalVariance { get; set; }
    public List<SettlementDiscrepancy> Discrepancies { get; set; } = new();
    public SettlementReconciliationSummary Summary { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Settlement discrepancy
/// </summary>
public class SettlementDiscrepancy
{
    public Guid SettlementId { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public string DiscrepancyType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public bool IsResolved { get; set; }
}

/// <summary>
/// Settlement reconciliation summary
/// </summary>
public class SettlementReconciliationSummary
{
    public decimal TotalExpectedAmount { get; set; }
    public decimal TotalActualAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public double ReconciliationRate { get; set; }
    public Dictionary<string, int> DiscrepancyTypeCount { get; set; } = new();
    public Dictionary<Currency, decimal> VarianceByCurrency { get; set; } = new();
}

/// <summary>
/// Settlement schedule configuration request
/// </summary>
public class SettlementScheduleConfigRequest
{
    [Required]
    public SettlementFrequency Frequency { get; set; }
    
    public int? DayOfWeek { get; set; } // 1-7 for weekly
    public int? DayOfMonth { get; set; } // 1-31 for monthly
    
    [Range(0, 23)]
    public int ProcessingHour { get; set; } = 9;
    
    [Range(0, 59)]
    public int ProcessingMinute { get; set; } = 0;
    
    [Range(0.01, double.MaxValue)]
    public decimal? MinimumAmount { get; set; }
    
    public bool AutoProcess { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Settlement schedule configuration
/// </summary>
public class SettlementScheduleConfig : SettlementScheduleConfigRequest
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public DateTime? NextScheduledDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Settlement sort options
/// </summary>

/// <summary>
/// Sort direction
/// </summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}

/// <summary>
/// Analytics grouping options
/// </summary>
public enum AnalyticsGroupBy
{
    Day = 0,
    Week = 1,
    Month = 2,
    Quarter = 3,
    Year = 4
}

/// <summary>
/// Export format options
/// </summary>
public enum ExportFormat
{
    CSV = 0,
    Excel = 1,
    PDF = 2,
    JSON = 3
}

/// <summary>
/// Settlement frequency options
/// </summary>

/// <summary>
/// Request for creating a settlement batch
/// </summary>
public class CreateSettlementBatchRequest
{
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    [Required]
    public DateTime SettlementDate { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal? MinimumAmount { get; set; }
    
    public bool AutoProcess { get; set; } = false;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Settlement batch response
/// </summary>
public class SettlementBatchResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string BatchReference { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime SettlementDate { get; set; }
    public int TotalTransactions { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal NetAmount { get; set; }
    public Currency Currency { get; set; }
    public SettlementStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Notes { get; set; }
    public List<SettlementSummary> Settlements { get; set; } = new();
}

/// <summary>
/// Settlement search request
/// </summary>
public class SettlementSearchRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public List<SettlementStatus>? Statuses { get; set; }
    
    public SettlementStatus? Status { get; set; }
    
    public Currency? Currency { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? MinAmount { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? MaxAmount { get; set; }
    
    public string? SearchTerm { get; set; }
    
    public SettlementSortBy SortBy { get; set; } = SettlementSortBy.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Settlement report request
/// </summary>
public class SettlementReportRequest
{
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public ReportFormat Format { get; set; } = ReportFormat.Summary;
    
    public bool IncludeTransactionDetails { get; set; } = false;
    
    public bool IncludeCharts { get; set; } = true;
    
    public List<string>? CustomFields { get; set; }
}

/// <summary>
/// Settlement report response
/// </summary>
public class SettlementReportResponse
{
    public Guid ReportId { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public ReportFormat Format { get; set; }
    
    // Summary data
    public SettlementReportSummary Summary { get; set; } = new();
    
    // Detailed data
    public List<SettlementSummary> Settlements { get; set; } = new();
    
    // Analytics data
    public SettlementReportAnalytics Analytics { get; set; } = new();
    
    // Chart data
    public List<SettlementChartData> Charts { get; set; } = new();
    
    // Direct properties for backward compatibility
    public int TotalSettlements => Summary.TotalSettlements;
    public decimal TotalGrossAmount => Summary.TotalGrossAmount;
    public decimal TotalFees => Summary.TotalFees;
    public decimal TotalNetAmount => Summary.TotalNetAmount;
    public int TotalTransactions => Summary.TotalTransactions;
    public Dictionary<SettlementStatus, int> SettlementsByStatus => Summary.StatusBreakdown;
    public Dictionary<Currency, decimal> SettlementsByCurrency => Summary.CurrencyBreakdown;
    public List<DailySettlementSummary> DailySettlements { get; set; } = new();
}

/// <summary>
/// Daily settlement summary
/// </summary>
public class DailySettlementSummary
{
    public DateTime Date { get; set; }
    public int SettlementCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// Settlement report summary
/// </summary>
public class SettlementReportSummary
{
    public int TotalSettlements { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal AverageSettlementAmount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<SettlementStatus, int> StatusBreakdown { get; set; } = new();
    public Dictionary<Currency, decimal> CurrencyBreakdown { get; set; } = new();
}

/// <summary>
/// Settlement report analytics
/// </summary>
public class SettlementReportAnalytics
{
    public List<SettlementTrendData> VolumeOverTime { get; set; } = new();
    public List<SettlementTrendData> SuccessRateOverTime { get; set; } = new();
    public Dictionary<string, decimal> FeeAnalysis { get; set; } = new();
    public List<SettlementPerformanceMetric> PerformanceMetrics { get; set; } = new();
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Settlement chart data
/// </summary>
public class SettlementChartData
{
    public string ChartType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public List<ChartDataSeries> Series { get; set; } = new();
}

/// <summary>
/// Chart data series
/// </summary>
public class ChartDataSeries
{
    public string Name { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Settlement forecast response
/// </summary>
public class SettlementForecastResponse
{
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public int ForecastDays { get; set; }
    
    public SettlementForecastSummary Summary { get; set; } = new();
    public List<SettlementForecastItem> DailyForecasts { get; set; } = new();
    public SettlementForecastConfidence Confidence { get; set; } = new();
    
    // Direct properties for backward compatibility
    public decimal EstimatedGrossAmount => Summary.EstimatedTotalAmount;
    public decimal EstimatedFees => Summary.EstimatedTotalFees;
    public decimal EstimatedNetAmount => Summary.EstimatedNetAmount;
    public int EstimatedTransactionCount => Summary.EstimatedTransactionCount;
    public List<DailySettlementForecast> DailyForecast => DailyForecasts.Select(f => new DailySettlementForecast
    {
        Date = f.Date,
        EstimatedAmount = f.EstimatedAmount,
        EstimatedFees = f.EstimatedFees,
        EstimatedNetAmount = f.EstimatedNetAmount,
        EstimatedTransactionCount = f.EstimatedTransactionCount,
        ConfidenceLevel = f.ConfidenceLevel
    }).ToList();
}

/// <summary>
/// Daily settlement forecast
/// </summary>
public class DailySettlementForecast
{
    public DateTime Date { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal EstimatedFees { get; set; }
    public decimal EstimatedNetAmount { get; set; }
    public int EstimatedTransactionCount { get; set; }
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Settlement forecast summary
/// </summary>
public class SettlementForecastSummary
{
    public decimal EstimatedTotalAmount { get; set; }
    public decimal EstimatedTotalFees { get; set; }
    public decimal EstimatedNetAmount { get; set; }
    public int EstimatedTransactionCount { get; set; }
    public int EstimatedSettlementCount { get; set; }
    public decimal DailyAverageAmount { get; set; }
}

/// <summary>
/// Settlement forecast item
/// </summary>
public class SettlementForecastItem
{
    public DateTime Date { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal EstimatedFees { get; set; }
    public decimal EstimatedNetAmount { get; set; }
    public int EstimatedTransactionCount { get; set; }
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Settlement forecast confidence
/// </summary>
public class SettlementForecastConfidence
{
    public double OverallConfidence { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty; // High, Medium, Low
    public List<string> FactorsConsidered { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public string Methodology { get; set; } = string.Empty;
}

/// <summary>
/// Report format options
/// </summary>
public enum ReportFormat
{
    Summary = 0,
    Detailed = 1,
    Analytics = 2,
    Executive = 3
}