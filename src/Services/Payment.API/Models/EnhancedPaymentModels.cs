using YourCompanyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Payment.API.Models;

/// <summary>
/// Payment token for secure storage of payment methods
/// </summary>
[Table("PaymentTokens")]
public class PaymentToken : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    public string EncryptedData { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string MaskedDetails { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsDefault { get; set; }

    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

/// <summary>
/// Webhook endpoint configuration for merchants
/// </summary>
[Table("WebhookEndpoints")]
public class WebhookEndpoint : AuditableEntity
{
    [Required]
    public Guid MerchantId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [Required]
    public List<string> Events { get; set; } = new();

    [Required]
    [MaxLength(100)]
    public string Secret { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    
    public Dictionary<string, string> Headers { get; set; } = new();

    // Navigation properties
    public virtual Merchant? Merchant { get; set; }
    public virtual ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}

/// <summary>
/// Webhook delivery attempt record
/// </summary>
[Table("WebhookDeliveries")]
public class WebhookDelivery : BaseEntity
{
    [Required]
    public Guid WebhookEndpointId { get; set; }

    [Required]
    public Guid PaymentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public int RetryCount { get; set; } = 0;

    public DateTime AttemptedAt { get; set; }

    public DateTime? NextRetryAt { get; set; }
    
    public DateTime? LastAttemptAt { get; set; }
    
    public int ResponseStatusCode { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public string? ErrorMessage { get; set; }

    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;

    public int AttemptCount { get; set; } = 0;

    public DateTime? NextAttemptAt { get; set; }

    // Navigation properties
    public virtual WebhookEndpoint WebhookEndpoint { get; set; } = null!;
    public virtual Payment Payment { get; set; } = null!;
}

/// <summary>
/// Webhook processing log
/// </summary>
[Table("WebhookLogs")]
public class WebhookLog : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    public Guid? PaymentId { get; set; }

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual Payment? Payment { get; set; }
}

/// <summary>
/// Idempotency record for preventing duplicate operations
/// </summary>
[Table("IdempotencyRecords")]
public class IdempotencyRecord : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    public string ResponseData { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Fraud assessment record
/// </summary>
[Table("FraudAssessments")]
public class FraudAssessment : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaymentAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public int RiskScore { get; set; }

    public RiskLevel RiskLevel { get; set; }

    [MaxLength(1000)]
    public string RiskFactors { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime AssessedAt { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
}

/// <summary>
/// Fraud report submitted by users or system
/// </summary>
[Table("FraudReports")]
public class FraudReport : BaseEntity
{
    public Guid? PaymentId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FraudType { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ReportedBy { get; set; } = string.Empty;

    public string? Evidence { get; set; }

    public DateTime ReportedAt { get; set; }

    public FraudReportStatus Status { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(100)]
    public string? ResolvedBy { get; set; }

    // Navigation properties
    public virtual Payment? Payment { get; set; }
    public virtual Customer Customer { get; set; } = null!;
}

/// <summary>
/// Fraud detection rules configuration
/// </summary>
[Table("FraudRules")]
public class FraudRule : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Condition { get; set; } = string.Empty;

    public int RiskScore { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Settlement batch for processing multiple payments
/// </summary>
[Table("SettlementBatches")]
public class SettlementBatch : AuditableEntity
{
    [Required]
    public Guid MerchantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string BatchReference { get; set; } = string.Empty;

    [Required]
    public Currency Currency { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFees { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    public int TransactionCount { get; set; }

    public DateTime SettlementDate { get; set; }

    public SettlementStatus Status { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(100)]
    public string? BankTransactionId { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    // Navigation properties
    public virtual Merchant Merchant { get; set; } = null!;
    public virtual ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();
}

/// <summary>
/// Individual item within a settlement batch
/// </summary>
[Table("SettlementItems")]
public class SettlementItem : BaseEntity
{
    [Required]
    public Guid SettlementBatchId { get; set; }

    public Guid? PaymentId { get; set; }

    public Guid? RefundId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Fees { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    public SettlementTransactionType TransactionType { get; set; }

    // Navigation properties
    public virtual SettlementBatch SettlementBatch { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
    public virtual PaymentRefund? Refund { get; set; }
}

// Note: PaymentRefund is defined in Payment.cs

// Add missing properties to existing Payment model
public partial class Payment
{
    public Guid? PaymentTokenId { get; set; }
    public SettlementStatus SettlementStatus { get; set; } = SettlementStatus.Pending;
    public DateTime? SettledAt { get; set; }
    public new Dictionary<string, object>? Metadata { get; set; }

    // Additional navigation properties
    public virtual PaymentToken? PaymentToken { get; set; }
}

// Add missing properties to existing Customer model
public partial class Customer
{
    public bool IsFraudulent { get; set; } = false;

    // Additional navigation properties
    public virtual ICollection<PaymentToken> PaymentTokens { get; set; } = new List<PaymentToken>();
    public virtual ICollection<FraudAssessment> FraudAssessments { get; set; } = new List<FraudAssessment>();
    public virtual ICollection<FraudReport> FraudReports { get; set; } = new List<FraudReport>();
}

// Add missing properties to existing Merchant model
public partial class Merchant
{
    public bool AutoSettlementEnabled { get; set; } = true;
    public int SettlementDelayDays { get; set; } = 2;

    // Additional navigation properties
    public virtual ICollection<WebhookEndpoint> WebhookEndpoints { get; set; } = new List<WebhookEndpoint>();
    public virtual ICollection<SettlementBatch> SettlementBatches { get; set; } = new List<SettlementBatch>();
}
