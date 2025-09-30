using YourCompanyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Risk.API.DTOs;

/// <summary>
/// Request DTO for credit assessment
/// </summary>
public class CreditAssessmentRequest
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
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal RequestedAmount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    public BNPLPlanType PlanType { get; set; }
    
    [Range(0, 10000000.00)]
    public decimal AnnualIncome { get; set; }
    
    [Range(0, 10000000.00)]
    public decimal MonthlyExpenses { get; set; }
    
    [Range(0, 10000000.00)]
    public decimal ExistingDebt { get; set; }
    
    [Range(0, 50)]
    public int ExistingCreditAccounts { get; set; }
    
    [Range(0, 600)]
    public int PaymentHistoryMonths { get; set; }
    
    [Range(0, 12)]
    public int LatePaymentsLast12Months { get; set; }
    
    public bool HasBankruptcy { get; set; }
    
    public bool HasCollections { get; set; }
    
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Response DTO for credit assessment
/// </summary>
public class CreditAssessmentResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal RequestedAmount { get; set; }
    public Currency Currency { get; set; }
    public BNPLPlanType PlanType { get; set; }
    public int CreditScore { get; set; }
    public CreditRating CreditRating { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal RecommendedCreditLimit { get; set; }
    public bool IsApproved { get; set; }
    public string? DeclineReason { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime AssessmentDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<RiskFactorSummary> RiskFactors { get; set; } = new();
    public CreditBureauSummary? CreditBureauData { get; set; }
}

/// <summary>
/// Risk factor summary
/// </summary>
public class RiskFactorSummary
{
    public string FactorType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel Impact { get; set; }
    public int Score { get; set; }
    public decimal Weight { get; set; }
}

/// <summary>
/// Credit bureau data summary
/// </summary>
public class CreditBureauSummary
{
    public string BureauName { get; set; } = string.Empty;
    public int CreditScore { get; set; }
    public string? ScoreModel { get; set; }
    public DateTime ScoreDate { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal AvailableCredit { get; set; }
    public int NumberOfAccounts { get; set; }
    public int NumberOfInquiries { get; set; }
    public bool HasBankruptcy { get; set; }
    public bool HasCollections { get; set; }
    public bool HasLatePayments { get; set; }
}

/// <summary>
/// Request DTO for fraud detection
/// </summary>
public class FraudDetectionRequest
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
    
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal TransactionAmount { get; set; }
    
    [Required]
    public Currency Currency { get; set; }
    
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Response DTO for fraud detection
/// </summary>
public class FraudDetectionResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public RiskLevel FraudRiskLevel { get; set; }
    public int FraudScore { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime DetectionDate { get; set; }
    public List<FraudRuleSummary> TriggeredRules { get; set; } = new();
}

/// <summary>
/// Fraud rule summary
/// </summary>
public class FraudRuleSummary
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel Severity { get; set; }
    public int Score { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Customer risk profile response
/// </summary>
public class CustomerRiskProfileResponse
{
    public Guid CustomerId { get; set; }
    public RiskLevel CurrentRiskLevel { get; set; }
    public int CurrentCreditScore { get; set; }
    public CreditRating CurrentCreditRating { get; set; }
    public decimal TotalCreditLimit { get; set; }
    public decimal AvailableCreditLimit { get; set; }
    public decimal TotalOutstandingDebt { get; set; }
    public int TotalBNPLPlans { get; set; }
    public int ActiveBNPLPlans { get; set; }
    public decimal TotalBNPLDebt { get; set; }
    public int TotalPayments { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public int LatePayments { get; set; }
    public DateTime LastPaymentDate { get; set; }
    public DateTime LastAssessmentDate { get; set; }
    public DateTime NextReviewDate { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public decimal PaymentSuccessRate => TotalPayments > 0 ? (decimal)SuccessfulPayments / TotalPayments * 100 : 0;
    public decimal CreditUtilization => TotalCreditLimit > 0 ? TotalOutstandingDebt / TotalCreditLimit * 100 : 0;
}

/// <summary>
/// Risk assessment search request
/// </summary>
public class RiskAssessmentSearchRequest
{
    public Guid? CustomerId { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public CreditRating? CreditRating { get; set; }
    public bool? IsApproved { get; set; }
    public int? MinCreditScore { get; set; }
    public int? MaxCreditScore { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? Email { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; } = "AssessmentDate";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Fraud detection search request
/// </summary>
public class FraudDetectionSearchRequest
{
    public Guid? CustomerId { get; set; }
    public RiskLevel? FraudRiskLevel { get; set; }
    public bool? IsBlocked { get; set; }
    public int? MinFraudScore { get; set; }
    public int? MaxFraudScore { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public string? CountryCode { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; } = "DetectionDate";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Risk analytics summary
/// </summary>
public class RiskAnalytics
{
    public int TotalAssessments { get; set; }
    public int ApprovedAssessments { get; set; }
    public int DeclinedAssessments { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal AverageCreditScore { get; set; }
    public Dictionary<RiskLevel, int> RiskLevelDistribution { get; set; } = new();
    public Dictionary<CreditRating, int> CreditRatingDistribution { get; set; } = new();
    public Dictionary<string, int> DeclineReasons { get; set; } = new();
    public Dictionary<string, decimal> AssessmentsByDate { get; set; } = new();
    
    // Fraud analytics
    public int TotalFraudChecks { get; set; }
    public int BlockedTransactions { get; set; }
    public decimal FraudRate { get; set; }
    public Dictionary<RiskLevel, int> FraudRiskDistribution { get; set; } = new();
    public Dictionary<string, int> TopFraudRules { get; set; } = new();
}

/// <summary>
/// Credit bureau integration request
/// </summary>
public class CreditBureauRequest
{
    [Required]
    [MaxLength(11)]
    public string SocialSecurityNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string BureauName { get; set; } = string.Empty;
    
    public bool IncludeFullReport { get; set; } = false;
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelPerformanceMetrics
{
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public decimal AUC { get; set; }
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public DateTime LastEvaluated { get; set; }
    public Dictionary<string, decimal> FeatureImportance { get; set; } = new();
}