using YourCompanyBNPL.Common.Enums;
using System.ComponentModel.DataAnnotations;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Models;

/// <summary>
/// A/B test experiment entity
/// </summary>
public class ABTestExperiment : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public ABTestStatus Status { get; set; } = ABTestStatus.Draft;

    [Required]
    public ABTestType Type { get; set; } = ABTestType.Template;

    [Required]
    [Range(0.01, 1.0)]
    public double TrafficAllocation { get; set; } = 1.0; // Percentage of traffic to include in test

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(1, 100000)]
    public int? MinSampleSize { get; set; }

    [Range(0.01, 0.99)]
    public double? SignificanceLevel { get; set; } = 0.05; // 95% confidence

    [Required]
    [MaxLength(100)]
    public string WinningMetric { get; set; } = "conversion_rate"; // conversion_rate, open_rate, click_rate, etc.

    public bool AutoDeclareWinner { get; set; } = false;

    public Guid? WinnerVariantId { get; set; }
    public ABTestVariant? WinnerVariant { get; set; }

    public DateTime? WinnerDeclaredAt { get; set; }

    [MaxLength(500)]
    public string? WinnerReason { get; set; }

    // Navigation properties
    public List<ABTestVariant> Variants { get; set; } = new();
    public List<ABTestAssignment> Assignments { get; set; } = new();
    public List<ABTestResult> Results { get; set; } = new();

    // Metadata
    public string? Metadata { get; set; }
    public string? Tags { get; set; }

    // Related entities
    public Guid? CampaignId { get; set; }
    public NotificationCampaign? Campaign { get; set; }
}

/// <summary>
/// A/B test variant entity
/// </summary>
public class ABTestVariant : BaseEntity
{
    [Required]
    public Guid ExperimentId { get; set; }
    public ABTestExperiment Experiment { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Control, Variant A, Variant B, etc.

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.0, 1.0)]
    public double TrafficWeight { get; set; } = 0.5; // Percentage of test traffic

    public bool IsControl { get; set; } = false;

    // Template-related fields
    public Guid? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }

    // Content overrides for quick testing
    public string? SubjectOverride { get; set; }
    public string? ContentOverride { get; set; }

    // Timing-related fields
    public TimeSpan? SendDelayOverride { get; set; }
    public DayOfWeek? PreferredDayOverride { get; set; }
    public TimeSpan? PreferredTimeOverride { get; set; }

    // Channel-related fields
    public NotificationChannel? ChannelOverride { get; set; }

    // Navigation properties
    public List<ABTestAssignment> Assignments { get; set; } = new();
    public List<ABTestResult> Results { get; set; } = new();

    // Metadata
    public string? Metadata { get; set; }
}

/// <summary>
/// A/B test assignment entity (tracks which users got which variant)
/// </summary>
public class ABTestAssignment : BaseEntity
{
    [Required]
    public Guid ExperimentId { get; set; }
    public ABTestExperiment Experiment { get; set; } = null!;

    [Required]
    public Guid VariantId { get; set; }
    public ABTestVariant Variant { get; set; } = null!;

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Tracking fields
    public bool NotificationSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public bool NotificationDelivered { get; set; } = false;
    public DateTime? DeliveredAt { get; set; }
    public bool NotificationOpened { get; set; } = false;
    public DateTime? OpenedAt { get; set; }
    public bool NotificationClicked { get; set; } = false;
    public DateTime? ClickedAt { get; set; }
    public bool ConversionAchieved { get; set; } = false;
    public DateTime? ConvertedAt { get; set; }
    public decimal? ConversionValue { get; set; }

    // Metadata
    public string? Metadata { get; set; }
}

/// <summary>
/// A/B test results aggregation entity
/// </summary>
public class ABTestResult : BaseEntity
{
    [Required]
    public Guid ExperimentId { get; set; }
    public ABTestExperiment Experiment { get; set; } = null!;

    [Required]
    public Guid VariantId { get; set; }
    public ABTestVariant Variant { get; set; } = null!;

    [Required]
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Sample size
    public int TotalAssignments { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsDelivered { get; set; }
    public int NotificationsOpened { get; set; }
    public int NotificationsClicked { get; set; }
    public int Conversions { get; set; }

    // Rates
    public double DeliveryRate { get; set; }
    public double OpenRate { get; set; }
    public double ClickRate { get; set; }
    public double ConversionRate { get; set; }

    // Statistical significance
    public double? PValue { get; set; }
    public double? ConfidenceInterval { get; set; }
    public bool IsStatisticallySignificant { get; set; } = false;

    // Revenue metrics
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal RevenuePerUser { get; set; }

    // Metadata
    public string? Metadata { get; set; }
}

/// <summary>
/// A/B test status enumeration
/// </summary>
public enum ABTestStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Cancelled
}

/// <summary>
/// A/B test type enumeration
/// </summary>
public enum ABTestType
{
    Template,      // Testing different templates
    Subject,       // Testing different subject lines
    Content,       // Testing different content
    Timing,        // Testing different send times
    Channel,       // Testing different channels
    Frequency,     // Testing different frequencies
    Personalization // Testing different personalization levels
}

/// <summary>
/// A/B test statistical result
/// </summary>
public class ABTestStatistics
{
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public bool IsControl { get; set; }
    public int SampleSize { get; set; }
    public double ConversionRate { get; set; }
    public double StandardError { get; set; }
    public double ConfidenceInterval { get; set; }
    public double? PValue { get; set; }
    public double? Lift { get; set; } // Improvement over control
    public bool IsStatisticallySignificant { get; set; }
    public bool IsWinner { get; set; }
}

/// <summary>
/// A/B test recommendation
/// </summary>
public class ABTestRecommendation
{
    public Guid ExperimentId { get; set; }
    public string ExperimentName { get; set; } = string.Empty;
    public Guid? RecommendedVariantId { get; set; }
    public string? RecommendedVariantName { get; set; }
    public string Recommendation { get; set; } = string.Empty; // Continue, Stop, Declare Winner
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}