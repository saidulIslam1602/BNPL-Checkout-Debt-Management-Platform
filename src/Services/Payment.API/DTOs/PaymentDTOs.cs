using RivertyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace RivertyBNPL.Payment.API.DTOs;

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
    public DateTime SettlementDate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal NetAmount { get; set; }
    public Currency Currency { get; set; }
    public SettlementStatus Status { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}