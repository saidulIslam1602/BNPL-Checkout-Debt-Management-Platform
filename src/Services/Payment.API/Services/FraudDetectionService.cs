using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for fraud detection and risk assessment
/// </summary>
public interface IFraudDetectionService
{
    Task<FraudAssessmentResult> AssessPaymentRiskAsync(CreatePaymentRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<FraudAssessmentResult> AssessCustomerRiskAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ApiResponse> ReportFraudulentActivityAsync(ReportFraudRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> UpdateFraudRulesAsync(List<FraudRule> rules, CancellationToken cancellationToken = default);
}

public class FraudDetectionService : IFraudDetectionService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<FraudDetectionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public FraudDetectionService(
        PaymentDbContext context,
        ILogger<FraudDetectionService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<FraudAssessmentResult> AssessPaymentRiskAsync(CreatePaymentRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Assessing fraud risk for payment request from customer {CustomerId}", request.CustomerId);

            var riskScore = 0;
            var riskFactors = new List<string>();
            var recommendations = new List<string>();

            // Get customer information
            var customer = await _context.Customers
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return new FraudAssessmentResult
                {
                    RiskLevel = RiskLevel.High,
                    RiskScore = 100,
                    RiskFactors = new List<string> { "Customer not found" },
                    Recommendations = new List<string> { "Reject payment" },
                    RequiresManualReview = true
                };
            }

            // 1. Velocity checks
            var velocityRisk = await AssessVelocityRiskAsync(customer, request.Amount, cancellationToken);
            riskScore += velocityRisk.Score;
            riskFactors.AddRange(velocityRisk.Factors);

            // 2. Amount-based risk assessment
            var amountRisk = AssessAmountRisk(request.Amount, customer);
            riskScore += amountRisk.Score;
            riskFactors.AddRange(amountRisk.Factors);

            // 3. Customer history assessment
            var historyRisk = AssessCustomerHistoryRisk(customer);
            riskScore += historyRisk.Score;
            riskFactors.AddRange(historyRisk.Factors);

            // 4. Geographic risk assessment
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var geoRisk = await AssessGeographicRiskAsync(ipAddress, customer, cancellationToken);
                riskScore += geoRisk.Score;
                riskFactors.AddRange(geoRisk.Factors);
            }

            // 5. Device fingerprinting (if available)
            var deviceRisk = await AssessDeviceRiskAsync(request.Metadata, customer, cancellationToken);
            riskScore += deviceRisk.Score;
            riskFactors.AddRange(deviceRisk.Factors);

            // 6. Time-based risk assessment
            var timeRisk = AssessTimeBasedRisk();
            riskScore += timeRisk.Score;
            riskFactors.AddRange(timeRisk.Factors);

            // 7. Payment method risk
            var methodRisk = AssessPaymentMethodRisk(request.PaymentMethod, customer);
            riskScore += methodRisk.Score;
            riskFactors.AddRange(methodRisk.Factors);

            // Determine overall risk level
            var riskLevel = riskScore switch
            {
                <= 20 => RiskLevel.Low,
                <= 50 => RiskLevel.Medium,
                <= 80 => RiskLevel.High,
                _ => RiskLevel.VeryHigh
            };

            // Generate recommendations
            recommendations = GenerateRecommendations(riskLevel, riskFactors);

            // Log fraud assessment
            var fraudAssessment = new FraudAssessment
            {
                CustomerId = request.CustomerId,
                PaymentAmount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                RiskFactors = string.Join("; ", riskFactors),
                IpAddress = ipAddress,
                AssessedAt = DateTime.UtcNow
            };

            _context.FraudAssessments.Add(fraudAssessment);
            await _context.SaveChangesAsync(cancellationToken);

            return new FraudAssessmentResult
            {
                RiskLevel = riskLevel,
                RiskScore = riskScore,
                RiskFactors = riskFactors,
                Recommendations = recommendations,
                RequiresManualReview = riskLevel >= RiskLevel.High,
                RequiresAdditionalAuthentication = riskLevel >= RiskLevel.Medium
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing fraud risk for customer {CustomerId}", request.CustomerId);
            
            // Return high risk on error to be safe
            return new FraudAssessmentResult
            {
                RiskLevel = RiskLevel.High,
                RiskScore = 100,
                RiskFactors = new List<string> { "Error during risk assessment" },
                Recommendations = new List<string> { "Manual review required" },
                RequiresManualReview = true
            };
        }
    }

    public async Task<FraudAssessmentResult> AssessCustomerRiskAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Payments)
                .Include(c => c.BNPLPlans)
                .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

            if (customer == null)
            {
                return new FraudAssessmentResult
                {
                    RiskLevel = RiskLevel.High,
                    RiskScore = 100,
                    RiskFactors = new List<string> { "Customer not found" },
                    RequiresManualReview = true
                };
            }

            var riskScore = 0;
            var riskFactors = new List<string>();

            // Assess based on payment history
            var paymentHistory = customer.Payments?.Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-12)).ToList() ?? new List<Models.Payment>();
            
            if (paymentHistory.Any())
            {
                var failureRate = (double)paymentHistory.Count(p => p.Status == PaymentStatus.Failed) / paymentHistory.Count;
                if (failureRate > 0.2)
                {
                    riskScore += 30;
                    riskFactors.Add($"High payment failure rate: {failureRate:P}");
                }

                var chargebackCount = paymentHistory.Count(p => p.Status == PaymentStatus.Disputed);
                if (chargebackCount > 0)
                {
                    riskScore += chargebackCount * 25;
                    riskFactors.Add($"Chargebacks in history: {chargebackCount}");
                }
            }

            // Assess BNPL performance
            var bnplPlans = customer.BNPLPlans?.Where(bp => bp.CreatedAt >= DateTime.UtcNow.AddMonths(-12)).ToList() ?? new List<BNPLPlan>();
            if (bnplPlans.Any())
            {
                var overdueInstallments = await _context.Installments
                    .Where(i => bnplPlans.Select(bp => bp.Id).Contains(i.BNPLPlanId) && i.IsOverdue)
                    .CountAsync(cancellationToken);

                if (overdueInstallments > 0)
                {
                    riskScore += overdueInstallments * 10;
                    riskFactors.Add($"Overdue installments: {overdueInstallments}");
                }
            }

            // Collection status assessment
            if (customer.CollectionStatus != CollectionStatus.Current)
            {
                riskScore += customer.CollectionStatus switch
                {
                    CollectionStatus.EarlyDelinquency => 20,
                    CollectionStatus.Delinquent => 40,
                    CollectionStatus.LateDelinquent => 60,
                    CollectionStatus.ChargeOff => 100,
                    _ => 0
                };
                riskFactors.Add($"Collection status: {customer.CollectionStatus}");
            }

            var riskLevel = riskScore switch
            {
                <= 20 => RiskLevel.Low,
                <= 50 => RiskLevel.Medium,
                <= 80 => RiskLevel.High,
                _ => RiskLevel.VeryHigh
            };

            return new FraudAssessmentResult
            {
                RiskLevel = riskLevel,
                RiskScore = riskScore,
                RiskFactors = riskFactors,
                Recommendations = GenerateRecommendations(riskLevel, riskFactors),
                RequiresManualReview = riskLevel >= RiskLevel.High
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing customer risk for {CustomerId}", customerId);
            return new FraudAssessmentResult
            {
                RiskLevel = RiskLevel.High,
                RiskScore = 100,
                RiskFactors = new List<string> { "Error during assessment" },
                RequiresManualReview = true
            };
        }
    }

    public async Task<ApiResponse> ReportFraudulentActivityAsync(ReportFraudRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var fraudReport = new FraudReport
            {
                PaymentId = request.PaymentId,
                CustomerId = request.CustomerId,
                FraudType = request.FraudType,
                Description = request.Description,
                ReportedBy = request.ReportedBy,
                Evidence = request.Evidence,
                ReportedAt = DateTime.UtcNow,
                Status = Common.Enums.FraudReportStatus.UnderReview
            };

            _context.FraudReports.Add(fraudReport);

            // Update customer risk level if confirmed fraud
            if (request.IsConfirmedFraud)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

                if (customer != null)
                {
                    customer.RiskLevel = RiskLevel.VeryHigh;
                    customer.IsFraudulent = true;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Fraud report submitted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting fraudulent activity");
            return ApiResponse.ErrorResult("Failed to report fraud", 500);
        }
    }

    public async Task<ApiResponse> UpdateFraudRulesAsync(List<FraudRule> rules, CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove existing rules
            var existingRules = await _context.FraudRules.ToListAsync(cancellationToken);
            _context.FraudRules.RemoveRange(existingRules);

            // Add new rules
            _context.FraudRules.AddRange(rules);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Fraud rules updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rules");
            return ApiResponse.ErrorResult("Failed to update fraud rules", 500);
        }
    }

    #region Private Methods

    private async Task<(int Score, List<string> Factors)> AssessVelocityRiskAsync(Customer customer, decimal amount, CancellationToken cancellationToken)
    {
        var score = 0;
        var factors = new List<string>();

        // Check payments in last 24 hours
        var recentPayments = await _context.Payments
            .Where(p => p.CustomerId == customer.Id && p.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);

        if (recentPayments.Count > 5)
        {
            score += 25;
            factors.Add($"High payment velocity: {recentPayments.Count} payments in 24h");
        }

        var totalAmount24h = recentPayments.Sum(p => p.Amount);
        if (totalAmount24h > 50000) // 50,000 NOK
        {
            score += 20;
            factors.Add($"High amount velocity: {totalAmount24h:C} in 24h");
        }

        return (score, factors);
    }

    private static (int Score, List<string> Factors) AssessAmountRisk(decimal amount, Customer customer)
    {
        var score = 0;
        var factors = new List<string>();

        // Large amount risk
        if (amount > 100000) // 100,000 NOK
        {
            score += 30;
            factors.Add($"Large transaction amount: {amount:C}");
        }
        else if (amount > 50000) // 50,000 NOK
        {
            score += 15;
            factors.Add($"High transaction amount: {amount:C}");
        }

        // Amount vs available credit
        if (amount > customer.AvailableCredit * 0.8m)
        {
            score += 20;
            factors.Add("Amount near credit limit");
        }

        return (score, factors);
    }

    private static (int Score, List<string> Factors) AssessCustomerHistoryRisk(Customer customer)
    {
        var score = 0;
        var factors = new List<string>();

        // New customer risk
        if (customer.CreatedAt >= DateTime.UtcNow.AddDays(-30))
        {
            score += 15;
            factors.Add("New customer (less than 30 days)");
        }

        // Credit rating risk
        score += customer.CreditRating switch
        {
            CreditRating.VeryPoor => 40,
            CreditRating.Poor => 25,
            CreditRating.Fair => 10,
            CreditRating.Good => 0,
            CreditRating.Excellent => -5,
            _ => 0
        };

        if (customer.CreditRating <= CreditRating.Poor)
        {
            factors.Add($"Poor credit rating: {customer.CreditRating}");
        }

        return (score, factors);
    }

    private async Task<(int Score, List<string> Factors)> AssessGeographicRiskAsync(string ipAddress, Customer customer, CancellationToken cancellationToken)
    {
        var score = 0;
        var factors = new List<string>();

        try
        {
            // Simulate IP geolocation check
            // In real implementation, use a service like MaxMind GeoIP
            var isHighRiskCountry = await IsHighRiskCountryAsync(ipAddress);
            if (isHighRiskCountry)
            {
                score += 25;
                factors.Add("High-risk geographic location");
            }

            // Check if IP location matches customer's registered country
            var locationMismatch = await CheckLocationMismatchAsync(ipAddress, customer);
            if (locationMismatch)
            {
                score += 15;
                factors.Add("IP location mismatch with registered address");
            }
        }
        catch (Exception)
        {
            // Ignore geolocation errors
        }

        return (score, factors);
    }

    private async Task<(int Score, List<string> Factors)> AssessDeviceRiskAsync(Dictionary<string, object>? metadata, Customer customer, CancellationToken cancellationToken)
    {
        var score = 0;
        var factors = new List<string>();

        if (metadata?.ContainsKey("device_fingerprint") == true)
        {
            var deviceFingerprint = metadata["device_fingerprint"].ToString();
            
            // Check if device has been used for fraud before
            var fraudulentDevice = await _context.FraudReports
                .AnyAsync(fr => fr.Evidence != null && fr.Evidence.Contains(deviceFingerprint!), cancellationToken);

            if (fraudulentDevice)
            {
                score += 50;
                factors.Add("Device associated with previous fraud");
            }
        }

        return (score, factors);
    }

    private static (int Score, List<string> Factors) AssessTimeBasedRisk()
    {
        var score = 0;
        var factors = new List<string>();

        var now = DateTime.UtcNow;
        
        // Unusual hours (late night/early morning)
        if (now.Hour < 6 || now.Hour > 23)
        {
            score += 10;
            factors.Add("Transaction during unusual hours");
        }

        return (score, factors);
    }

    private static (int Score, List<string> Factors) AssessPaymentMethodRisk(PaymentMethod paymentMethod, Customer customer)
    {
        var score = 0;
        var factors = new List<string>();

        // Higher risk for certain payment methods
        score += paymentMethod switch
        {
            PaymentMethod.CreditCard => 5,
            PaymentMethod.DebitCard => 0,
            PaymentMethod.BankTransfer => -5,
            PaymentMethod.BNPL => 10,
            _ => 0
        };

        return (score, factors);
    }

    private static List<string> GenerateRecommendations(RiskLevel riskLevel, List<string> riskFactors)
    {
        var recommendations = new List<string>();

        switch (riskLevel)
        {
            case RiskLevel.Low:
                recommendations.Add("Approve payment");
                break;
            case RiskLevel.Medium:
                recommendations.Add("Require additional authentication");
                recommendations.Add("Monitor transaction closely");
                break;
            case RiskLevel.High:
                recommendations.Add("Manual review required");
                recommendations.Add("Consider declining payment");
                recommendations.Add("Request additional documentation");
                break;
            case RiskLevel.VeryHigh:
                recommendations.Add("Decline payment");
                recommendations.Add("Flag for investigation");
                recommendations.Add("Consider account suspension");
                break;
        }

        return recommendations;
    }

    private async Task<bool> IsHighRiskCountryAsync(string ipAddress)
    {
        // Simulate high-risk country check
        // In real implementation, use IP geolocation service
        await Task.Delay(10);
        return false; // Placeholder
    }

    private async Task<bool> CheckLocationMismatchAsync(string ipAddress, Customer customer)
    {
        // Simulate location mismatch check
        await Task.Delay(10);
        return false; // Placeholder
    }

    #endregion
}

// Supporting classes
public class FraudAssessmentResult
{
    public RiskLevel RiskLevel { get; set; }
    public int RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public bool RequiresManualReview { get; set; }
    public bool RequiresAdditionalAuthentication { get; set; }
}

public class ReportFraudRequest
{
    public Guid? PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public string FraudType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public bool IsConfirmedFraud { get; set; }
}

public enum FraudReportStatus
{
    UnderInvestigation,
    Confirmed,
    Dismissed,
    Resolved
}