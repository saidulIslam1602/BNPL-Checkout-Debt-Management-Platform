using RivertyBNPL.Common.Models;
using RivertyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RivertyBNPL.Payment.API.Models;

/// <summary>
/// Payment entity representing a financial transaction
/// </summary>
[Table("Payments")]
public class Payment : AuditableEntity
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    [Required]
    public TransactionType TransactionType { get; set; } = TransactionType.Payment;
    
    [MaxLength(100)]
    public string? OrderReference { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? TransactionId { get; set; }
    
    [MaxLength(100)]
    public string? GatewayTransactionId { get; set; }
    
    [MaxLength(50)]
    public string? AuthorizationCode { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? AuthorizedAt { get; set; }
    
    public DateTime? CapturedAt { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fees { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }
    
    [MaxLength(1000)]
    public string? FailureReason { get; set; }
    
    [MaxLength(50)]
    public string? ErrorCode { get; set; }
    
    public bool IsRetryable { get; set; } = false;
    
    public int RetryCount { get; set; } = 0;
    
    public DateTime? NextRetryAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Merchant? Merchant { get; set; }
    public virtual BNPLPlan? BNPLPlan { get; set; }
    public virtual ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
    public virtual ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}

/// <summary>
/// Customer entity
/// </summary>
[Table("Customers")]
public class Customer : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [MaxLength(11)]
    public string? SocialSecurityNumber { get; set; }
    
    [Required]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    
    public CreditRating CreditRating { get; set; } = CreditRating.NoHistory;
    
    public int CreditScore { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvailableCredit { get; set; } = 0;
    
    public CollectionStatus CollectionStatus { get; set; } = CollectionStatus.Current;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual CustomerAddress? Address { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<BNPLPlan> BNPLPlans { get; set; } = new List<BNPLPlan>();
}

/// <summary>
/// Customer address information
/// </summary>
[Table("CustomerAddresses")]
public class CustomerAddress : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = true;
    
    // Navigation properties
    public virtual Customer? Customer { get; set; }
}

/// <summary>
/// Merchant entity
/// </summary>
[Table("Merchants")]
public class Merchant : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(100)]
    public string? BusinessRegistrationNumber { get; set; }
    
    [MaxLength(100)]
    public string? VATNumber { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string MerchantCategory { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal CommissionRate { get; set; } = 0.0350m; // 3.5% default
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyVolume { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? OnboardedAt { get; set; }
    
    // Navigation properties
    public virtual MerchantAddress? Address { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}

/// <summary>
/// Merchant address information
/// </summary>
[Table("MerchantAddresses")]
public class MerchantAddress : BaseEntity
{
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Merchant? Merchant { get; set; }
}

/// <summary>
/// BNPL payment plan entity
/// </summary>
[Table("BNPLPlans")]
public class BNPLPlan : AuditableEntity
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public BNPLPlanType PlanType { get; set; }
    
    [Required]
    public int InstallmentCount { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal InstallmentAmount { get; set; }
    
    [Required]
    public DateTime FirstPaymentDate { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal InterestRate { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalInterest { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFees { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingBalance { get; set; }
    
    public int RemainingInstallments { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
    
    // Navigation properties
    public virtual Payment? Payment { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Merchant? Merchant { get; set; }
    public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();
}

/// <summary>
/// Individual installment in a BNPL plan
/// </summary>
[Table("Installments")]
public class Installment : AuditableEntity
{
    [Required]
    public Guid BNPLPlanId { get; set; }
    
    [Required]
    public int InstallmentNumber { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal FeeAmount { get; set; } = 0;
    
    [Required]
    public DateTime DueDate { get; set; }
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public DateTime? PaidAt { get; set; }
    
    [MaxLength(100)]
    public string? TransactionId { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }
    
    public bool IsOverdue { get; set; } = false;
    
    public int DaysPastDue { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal LateFee { get; set; } = 0;
    
    // Navigation properties
    public virtual BNPLPlan? BNPLPlan { get; set; }
}

/// <summary>
/// Payment refund entity
/// </summary>
[Table("PaymentRefunds")]
public class PaymentRefund : AuditableEntity
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    [MaxLength(100)]
    public string? RefundTransactionId { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    // Navigation properties
    public virtual Payment? Payment { get; set; }
}

/// <summary>
/// Payment event for audit trail
/// </summary>
[Table("PaymentEvents")]
public class PaymentEvent : BaseEntity
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public PaymentStatus FromStatus { get; set; }
    
    [Required]
    public PaymentStatus ToStatus { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public Dictionary<string, object>? EventData { get; set; }
    
    // Navigation properties
    public virtual Payment? Payment { get; set; }
}

/// <summary>
/// Settlement entity for merchant payouts
/// </summary>
[Table("Settlements")]
public class Settlement : AuditableEntity
{
    [Required]
    public Guid MerchantId { get; set; }
    
    [Required]
    public DateTime SettlementDate { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fees { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public SettlementStatus Status { get; set; } = SettlementStatus.Pending;
    
    [MaxLength(100)]
    public string? BankTransactionId { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    public int TransactionCount { get; set; }
    
    // Navigation properties
    public virtual Merchant? Merchant { get; set; }
    public virtual ICollection<SettlementTransaction> Transactions { get; set; } = new List<SettlementTransaction>();
}

/// <summary>
/// Individual transaction within a settlement
/// </summary>
[Table("SettlementTransactions")]
public class SettlementTransaction : BaseEntity
{
    [Required]
    public Guid SettlementId { get; set; }
    
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }
    
    // Navigation properties
    public virtual Settlement? Settlement { get; set; }
    public virtual Payment? Payment { get; set; }
}