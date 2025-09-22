using RivertyBNPL.Common.Models;
using RivertyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RivertyBNPL.Risk.API.Models;

/// <summary>
/// Credit assessment record for a customer
/// </summary>
[Table("CreditAssessments")]
public class CreditAssessment : AuditableEntity
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    [MaxLength(11)]
    public string SocialSecurityNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedAmount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public BNPLPlanType PlanType { get; set; }
    
    public int CreditScore { get; set; }
    
    public CreditRating CreditRating { get; set; }
    
    public RiskLevel RiskLevel { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RecommendedCreditLimit { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualIncome { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyExpenses { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExistingDebt { get; set; }
    
    public int ExistingCreditAccounts { get; set; }
    
    public int PaymentHistoryMonths { get; set; }
    
    public int LatePaymentsLast12Months { get; set; }
    
    public bool HasBankruptcy { get; set; }
    
    public bool HasCollections { get; set; }
    
    public bool IsApproved { get; set; }
    
    [MaxLength(500)]
    public string? DeclineReason { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal InterestRate { get; set; }
    
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    [MaxLength(100)]
    public string? ExternalReferenceId { get; set; }
    
    // Navigation properties
    public virtual ICollection<RiskFactor> RiskFactors { get; set; } = new List<RiskFactor>();
    public virtual ICollection<CreditBureauResponse> CreditBureauResponses { get; set; } = new List<CreditBureauResponse>();
}

/// <summary>
/// Individual risk factors identified during assessment
/// </summary>
[Table("RiskFactors")]
public class RiskFactor : BaseEntity
{
    [Required]
    public Guid CreditAssessmentId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FactorType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public RiskLevel Impact { get; set; }
    
    public int Score { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Weight { get; set; }
    
    // Navigation properties
    public virtual CreditAssessment? CreditAssessment { get; set; }
}

/// <summary>
/// Response from external credit bureau
/// </summary>
[Table("CreditBureauResponses")]
public class CreditBureauResponse : BaseEntity
{
    [Required]
    public Guid CreditAssessmentId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string BureauName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string RequestId { get; set; } = string.Empty;
    
    public int CreditScore { get; set; }
    
    [MaxLength(50)]
    public string? ScoreModel { get; set; }
    
    public DateTime ScoreDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDebt { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvailableCredit { get; set; }
    
    public int NumberOfAccounts { get; set; }
    
    public int NumberOfInquiries { get; set; }
    
    public bool HasBankruptcy { get; set; }
    
    public bool HasCollections { get; set; }
    
    public bool HasLatePayments { get; set; }
    
    public string? RawResponse { get; set; }
    
    public DateTime ResponseDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual CreditAssessment? CreditAssessment { get; set; }
}

/// <summary>
/// Fraud detection record
/// </summary>
[Table("FraudDetections")]
public class FraudDetection : AuditableEntity
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? DeviceFingerprint { get; set; }
    
    [MaxLength(2)]
    public string? CountryCode { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TransactionAmount { get; set; }
    
    public Currency Currency { get; set; }
    
    public RiskLevel FraudRiskLevel { get; set; }
    
    public int FraudScore { get; set; }
    
    public bool IsBlocked { get; set; }
    
    [MaxLength(500)]
    public string? BlockReason { get; set; }
    
    public DateTime DetectionDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<FraudRule> TriggeredRules { get; set; } = new List<FraudRule>();
}

/// <summary>
/// Fraud detection rules that were triggered
/// </summary>
[Table("FraudRules")]
public class FraudRule : BaseEntity
{
    [Required]
    public Guid FraudDetectionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public RiskLevel Severity { get; set; }
    
    public int Score { get; set; }
    
    [MaxLength(1000)]
    public string? Details { get; set; }
    
    // Navigation properties
    public virtual FraudDetection? FraudDetection { get; set; }
}

/// <summary>
/// Customer risk profile aggregated over time
/// </summary>
[Table("CustomerRiskProfiles")]
public class CustomerRiskProfile : AuditableEntity
{
    [Required]
    public Guid CustomerId { get; set; }
    
    public RiskLevel CurrentRiskLevel { get; set; }
    
    public int CurrentCreditScore { get; set; }
    
    public CreditRating CurrentCreditRating { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCreditLimit { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvailableCreditLimit { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalOutstandingDebt { get; set; }
    
    public int TotalBNPLPlans { get; set; }
    
    public int ActiveBNPLPlans { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBNPLDebt { get; set; }
    
    public int TotalPayments { get; set; }
    
    public int SuccessfulPayments { get; set; }
    
    public int FailedPayments { get; set; }
    
    public int LatePayments { get; set; }
    
    public DateTime LastPaymentDate { get; set; }
    
    public DateTime LastAssessmentDate { get; set; }
    
    public DateTime NextReviewDate { get; set; }
    
    public bool IsBlacklisted { get; set; }
    
    [MaxLength(500)]
    public string? BlacklistReason { get; set; }
    
    public DateTime? BlacklistedAt { get; set; }
}

/// <summary>
/// Risk assessment rules configuration
/// </summary>
[Table("RiskRules")]
public class RiskRule : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int Priority { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Weight { get; set; }
    
    public int MinScore { get; set; }
    
    public int MaxScore { get; set; }
    
    [MaxLength(1000)]
    public string? Conditions { get; set; }
    
    [MaxLength(500)]
    public string? Action { get; set; }
    
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// Machine learning model for risk scoring
/// </summary>
[Table("RiskModels")]
public class RiskModel : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ModelType { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime TrainedDate { get; set; }
    
    public DateTime DeployedDate { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Accuracy { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Precision { get; set; }
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Recall { get; set; }
    
    public int TrainingDataSize { get; set; }
    
    [MaxLength(500)]
    public string? ModelPath { get; set; }
    
    public string? Features { get; set; }
    
    public string? Hyperparameters { get; set; }
}