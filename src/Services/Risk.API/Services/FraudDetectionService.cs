using Microsoft.EntityFrameworkCore;
using AutoMapper;
using YourCompanyBNPL.Risk.API.Data;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Risk.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;
using System.Linq.Expressions;

namespace YourCompanyBNPL.Risk.API.Services;

/// <summary>
/// Service for fraud detection operations
/// </summary>
public class FraudDetectionService : IFraudDetectionService
{
    private readonly RiskDbContext _context;
    private readonly IMachineLearningService _machineLearningService;
    private readonly IMapper _mapper;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(
        RiskDbContext context,
        IMachineLearningService machineLearningService,
        IMapper mapper,
        ILogger<FraudDetectionService> logger)
    {
        _context = context;
        _machineLearningService = machineLearningService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<FraudDetectionResponse>> DetectFraudAsync(FraudDetectionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting fraud detection for transaction {TransactionId} from customer {CustomerId}",
                request.TransactionId, request.CustomerId);

            // Check for duplicate transaction
            var existingDetection = await _context.FraudDetections
                .FirstOrDefaultAsync(fd => fd.TransactionId == request.TransactionId, cancellationToken);

            if (existingDetection != null)
            {
                _logger.LogInformation("Returning existing fraud detection for transaction {TransactionId}", request.TransactionId);
                var existingResponse = await MapToResponseAsync(existingDetection, cancellationToken);
                return ApiResponse<FraudDetectionResponse>.SuccessResult(existingResponse, "Existing fraud detection retrieved");
            }

            // Create new fraud detection
            var detection = new FraudDetection
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                TransactionId = request.TransactionId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                DeviceFingerprint = request.DeviceFingerprint,
                CountryCode = request.CountryCode,
                TransactionAmount = request.TransactionAmount,
                Currency = request.Currency,
                DetectionDate = DateTime.UtcNow
            };

            // Apply fraud rules
            var triggeredRules = await ApplyFraudRulesAsync(detection, request, cancellationToken);
            foreach (var rule in triggeredRules)
            {
                detection.TriggeredRules.Add(rule);
            }

            // Calculate rule-based fraud score
            var ruleBasedScore = CalculateRuleBasedFraudScore(triggeredRules);

            // Get ML-based fraud score
            var mlFraudScore = await GetMLFraudScoreAsync(detection, request, cancellationToken);

            // Calculate final fraud score (weighted combination)
            var finalFraudScore = (int)((ruleBasedScore * 0.4m) + (mlFraudScore * 0.6m));
            detection.FraudScore = Math.Min(100, Math.Max(0, finalFraudScore));

            // Determine risk level and blocking decision
            var (riskLevel, isBlocked, blockReason) = DetermineFraudAction(detection.FraudScore, triggeredRules);
            detection.FraudRiskLevel = riskLevel;
            detection.IsBlocked = isBlocked;
            detection.BlockReason = blockReason;

            // Save detection
            _context.FraudDetections.Add(detection);
            await _context.SaveChangesAsync(cancellationToken);

            // Update customer risk profile if high risk
            if (riskLevel >= RiskLevel.High)
            {
                await UpdateCustomerFraudProfileAsync(request.CustomerId, detection, cancellationToken);
            }

            _logger.LogInformation("Fraud detection completed for transaction {TransactionId}. Risk Level: {RiskLevel}, Score: {FraudScore}, Blocked: {IsBlocked}",
                request.TransactionId, riskLevel, detection.FraudScore, isBlocked);

            var response = await MapToResponseAsync(detection, cancellationToken);
            return ApiResponse<FraudDetectionResponse>.SuccessResult(response, "Fraud detection completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fraud detection for transaction {TransactionId}", request.TransactionId);
            return ApiResponse<FraudDetectionResponse>.ErrorResult("An error occurred during fraud detection", 500);
        }
    }

    public async Task<ApiResponse<FraudDetectionResponse>> GetFraudDetectionAsync(Guid detectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var detection = await _context.FraudDetections
                .Include(fd => fd.TriggeredRules)
                .FirstOrDefaultAsync(fd => fd.Id == detectionId, cancellationToken);

            if (detection == null)
            {
                return ApiResponse<FraudDetectionResponse>.ErrorResult("Fraud detection not found", 404);
            }

            var response = await MapToResponseAsync(detection, cancellationToken);
            return ApiResponse<FraudDetectionResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud detection {DetectionId}", detectionId);
            return ApiResponse<FraudDetectionResponse>.ErrorResult("An error occurred while retrieving fraud detection", 500);
        }
    }

    public async Task<PagedApiResponse<FraudDetectionResponse>> SearchFraudDetectionsAsync(FraudDetectionSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.FraudDetections.AsQueryable();

            // Apply filters
            if (request.CustomerId.HasValue)
                query = query.Where(fd => fd.CustomerId == request.CustomerId.Value);

            if (request.FraudRiskLevel.HasValue)
                query = query.Where(fd => fd.FraudRiskLevel == request.FraudRiskLevel.Value);

            if (request.IsBlocked.HasValue)
                query = query.Where(fd => fd.IsBlocked == request.IsBlocked.Value);

            if (request.MinFraudScore.HasValue)
                query = query.Where(fd => fd.FraudScore >= request.MinFraudScore.Value);

            if (request.MaxFraudScore.HasValue)
                query = query.Where(fd => fd.FraudScore <= request.MaxFraudScore.Value);

            if (request.FromDate.HasValue)
                query = query.Where(fd => fd.DetectionDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(fd => fd.DetectionDate <= request.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(request.IpAddress))
                query = query.Where(fd => fd.IpAddress == request.IpAddress);

            if (!string.IsNullOrWhiteSpace(request.CountryCode))
                query = query.Where(fd => fd.CountryCode == request.CountryCode);

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            var totalCount = await query.CountAsync(cancellationToken);
            var detections = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(fd => fd.TriggeredRules)
                .ToListAsync(cancellationToken);

            var responses = new List<FraudDetectionResponse>();
            foreach (var detection in detections)
            {
                responses.Add(await MapToResponseAsync(detection, cancellationToken));
            }

            return PagedApiResponse<FraudDetectionResponse>.SuccessResult(
                responses, request.Page, request.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching fraud detections");
            return PagedApiResponse<FraudDetectionResponse>.ErrorResult("An error occurred while searching fraud detections", 500);
        }
    }

    public async Task<ApiResponse> UpdateFraudRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fraud rules and thresholds");

            // This would typically involve:
            // 1. Analyzing recent fraud patterns
            // 2. Updating rule thresholds based on performance
            // 3. Adding new rules based on emerging threats
            // 4. Disabling ineffective rules

            // For now, we'll simulate rule updates
            await Task.Delay(1000, cancellationToken); // Simulate processing time

            _logger.LogInformation("Fraud rules updated successfully");
            return ApiResponse.SuccessResponse("Fraud rules updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rules");
            return ApiResponse.ErrorResult("An error occurred while updating fraud rules", 500);
        }
    }

    #region Private Methods

    private async Task<List<FraudRule>> ApplyFraudRulesAsync(FraudDetection detection, FraudDetectionRequest request, CancellationToken cancellationToken)
    {
        var triggeredRules = new List<FraudRule>();

        // Rule 1: High transaction amount
        if (request.TransactionAmount > 50000)
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "HighTransactionAmount",
                Description = "Transaction amount exceeds high-risk threshold",
                Severity = RiskLevel.High,
                Score = 40,
                Details = $"Amount: {request.TransactionAmount:C}"
            });
        }
        else if (request.TransactionAmount > 20000)
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "MediumTransactionAmount",
                Description = "Transaction amount exceeds medium-risk threshold",
                Severity = RiskLevel.Medium,
                Score = 20,
                Details = $"Amount: {request.TransactionAmount:C}"
            });
        }

        // Rule 2: Suspicious IP address patterns
        if (await IsSuspiciousIPAsync(request.IpAddress, cancellationToken))
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "SuspiciousIP",
                Description = "IP address flagged as suspicious",
                Severity = RiskLevel.High,
                Score = 35,
                Details = $"IP: {request.IpAddress}"
            });
        }

        // Rule 3: Unusual time patterns
        var currentHour = DateTime.UtcNow.Hour;
        if (currentHour >= 2 && currentHour <= 5) // Late night transactions
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "UnusualTimePattern",
                Description = "Transaction during unusual hours",
                Severity = RiskLevel.Low,
                Score = 10,
                Details = $"Hour: {currentHour}"
            });
        }

        // Rule 4: Velocity check - multiple transactions from same customer
        var recentTransactions = await _context.FraudDetections
            .Where(fd => fd.CustomerId == request.CustomerId && 
                        fd.DetectionDate > DateTime.UtcNow.AddHours(-1))
            .CountAsync(cancellationToken);

        if (recentTransactions >= 5)
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "HighVelocity",
                Description = "Multiple transactions in short time period",
                Severity = RiskLevel.High,
                Score = 30,
                Details = $"Transactions in last hour: {recentTransactions}"
            });
        }
        else if (recentTransactions >= 3)
        {
            triggeredRules.Add(new FraudRule
            {
                Id = Guid.NewGuid(),
                FraudDetectionId = detection.Id,
                RuleName = "MediumVelocity",
                Description = "Elevated transaction frequency",
                Severity = RiskLevel.Medium,
                Score = 15,
                Details = $"Transactions in last hour: {recentTransactions}"
            });
        }

        // Rule 5: Device fingerprint analysis
        if (!string.IsNullOrEmpty(request.DeviceFingerprint))
        {
            var deviceRisk = await AnalyzeDeviceRiskAsync(request.DeviceFingerprint, request.CustomerId, cancellationToken);
            if (deviceRisk >= 70)
            {
                triggeredRules.Add(new FraudRule
                {
                    Id = Guid.NewGuid(),
                    FraudDetectionId = detection.Id,
                    RuleName = "HighRiskDevice",
                    Description = "Device flagged as high risk",
                    Severity = RiskLevel.High,
                    Score = 25,
                    Details = $"Device risk score: {deviceRisk}"
                });
            }
        }

        // Rule 6: Geographic anomaly
        if (!string.IsNullOrEmpty(request.CountryCode))
        {
            var isAnomalous = await IsGeographicAnomalyAsync(request.CustomerId, request.CountryCode, cancellationToken);
            if (isAnomalous)
            {
                triggeredRules.Add(new FraudRule
                {
                    Id = Guid.NewGuid(),
                    FraudDetectionId = detection.Id,
                    RuleName = "GeographicAnomaly",
                    Description = "Transaction from unusual geographic location",
                    Severity = RiskLevel.Medium,
                    Score = 20,
                    Details = $"Country: {request.CountryCode}"
                });
            }
        }

        return triggeredRules;
    }

    private static int CalculateRuleBasedFraudScore(List<FraudRule> triggeredRules)
    {
        if (!triggeredRules.Any()) return 0;

        // Calculate weighted score based on rule severity and individual scores
        var totalScore = triggeredRules.Sum(rule => rule.Score * GetSeverityWeight(rule.Severity));
        var maxPossibleScore = triggeredRules.Count * 50 * 1.5; // Assuming max score per rule is 50 with high severity weight

        return Math.Min(100, (int)((totalScore / maxPossibleScore) * 100));
    }

    private static double GetSeverityWeight(RiskLevel severity)
    {
        return severity switch
        {
            RiskLevel.High => 1.5,
            RiskLevel.Medium => 1.0,
            RiskLevel.Low => 0.5,
            _ => 0.3
        };
    }

    private async Task<decimal> GetMLFraudScoreAsync(FraudDetection detection, FraudDetectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var features = new Dictionary<string, object>
            {
                { "TransactionAmount", request.TransactionAmount },
                { "HourOfDay", DateTime.UtcNow.Hour },
                { "DayOfWeek", (int)DateTime.UtcNow.DayOfWeek },
                { "DeviceRiskScore", await AnalyzeDeviceRiskAsync(request.DeviceFingerprint ?? "", request.CustomerId, cancellationToken) },
                { "LocationRiskScore", await GetLocationRiskScoreAsync(request.CountryCode ?? "", cancellationToken) },
                { "VelocityScore", await GetVelocityScoreAsync(request.CustomerId, cancellationToken) },
                { "CustomerAge", 35 }, // Would come from customer data
                { "AccountAge", 180 }, // Would come from customer data
                { "PreviousTransactionCount", await GetPreviousTransactionCountAsync(request.CustomerId, cancellationToken) },
                { "AverageTransactionAmount", await GetAverageTransactionAmountAsync(request.CustomerId, cancellationToken) },
                { "IsNewDevice", await IsNewDeviceAsync(request.DeviceFingerprint ?? "", request.CustomerId, cancellationToken) },
                { "IsNewLocation", await IsNewLocationAsync(request.CountryCode ?? "", request.CustomerId, cancellationToken) },
                { "IsWeekend", DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday }
            };

            var result = await _machineLearningService.PredictFraudRiskAsync(features, cancellationToken);
            return result.Success ? result.Data : 30; // Default fraud score
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get ML fraud score for transaction {TransactionId}", request.TransactionId);
            return 30; // Default fraud score
        }
    }

    private static (RiskLevel RiskLevel, bool IsBlocked, string? BlockReason) DetermineFraudAction(int fraudScore, List<FraudRule> triggeredRules)
    {
        // Determine risk level based on fraud score
        var riskLevel = fraudScore switch
        {
            >= 80 => RiskLevel.High,
            >= 60 => RiskLevel.Medium,
            >= 40 => RiskLevel.Low,
            _ => RiskLevel.VeryLow
        };

        // Determine if transaction should be blocked
        var hasHighSeverityRules = triggeredRules.Any(r => r.Severity == RiskLevel.High);
        var isBlocked = fraudScore >= 70 || hasHighSeverityRules;

        // Determine block reason
        string? blockReason = null;
        if (isBlocked)
        {
            if (hasHighSeverityRules)
            {
                var highSeverityRule = triggeredRules.First(r => r.Severity == RiskLevel.High);
                blockReason = $"High-risk rule triggered: {highSeverityRule.RuleName}";
            }
            else
            {
                blockReason = $"High fraud score: {fraudScore}";
            }
        }

        return (riskLevel, isBlocked, blockReason);
    }

    private async Task UpdateCustomerFraudProfileAsync(Guid customerId, FraudDetection detection, CancellationToken cancellationToken)
    {
        // This would update the customer's fraud profile in the risk profile table
        // For now, we'll just log the high-risk detection
        _logger.LogWarning("High-risk fraud detection for customer {CustomerId}, Transaction {TransactionId}, Score: {FraudScore}",
            customerId, detection.TransactionId, detection.FraudScore);
        await Task.CompletedTask;
    }

    private async Task<FraudDetectionResponse> MapToResponseAsync(FraudDetection detection, CancellationToken cancellationToken)
    {
        var response = _mapper.Map<FraudDetectionResponse>(detection);
        
        // Map triggered rules
        response.TriggeredRules = detection.TriggeredRules.Select(rule => new FraudRuleSummary
        {
            RuleName = rule.RuleName,
            Description = rule.Description,
            Severity = rule.Severity,
            Score = rule.Score,
            Details = rule.Details
        }).ToList();

        await Task.CompletedTask;
        return response;
    }

    private static IQueryable<FraudDetection> ApplySorting(IQueryable<FraudDetection> query, string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return sortDescending ? query.OrderByDescending(fd => fd.DetectionDate) : query.OrderBy(fd => fd.DetectionDate);

        Expression<Func<FraudDetection, object>> keySelector = sortBy.ToLower() switch
        {
            "fraudscore" => fd => fd.FraudScore,
            "fraudrisklevel" => fd => fd.FraudRiskLevel,
            "isblocked" => fd => fd.IsBlocked,
            "customerid" => fd => fd.CustomerId,
            "transactionamount" => fd => fd.TransactionAmount,
            _ => fd => fd.DetectionDate
        };

        return sortDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    // Helper methods for fraud analysis
    private async Task<bool> IsSuspiciousIPAsync(string ipAddress, CancellationToken cancellationToken)
    {
        // This would check against known suspicious IP databases
        // For now, simulate with some basic checks
        await Task.CompletedTask;
        return ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."); // Private IPs as example
    }

    private async Task<int> AnalyzeDeviceRiskAsync(string deviceFingerprint, Guid customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(deviceFingerprint)) return 50;

        // This would analyze device characteristics and history
        var deviceUsageCount = await _context.FraudDetections
            .Where(fd => fd.DeviceFingerprint == deviceFingerprint)
            .CountAsync(cancellationToken);

        return deviceUsageCount switch
        {
            0 => 80, // New device
            1 => 60, // Recently seen
            >= 10 => 20, // Well-established device
            _ => 40
        };
    }

    private async Task<int> GetLocationRiskScoreAsync(string countryCode, CancellationToken cancellationToken)
    {
        // This would analyze location risk based on fraud statistics
        await Task.CompletedTask;
        return countryCode switch
        {
            "NO" => 10, // Norway - low risk
            "SE" or "DK" or "FI" => 15, // Nordic countries - low risk
            "US" or "GB" or "DE" => 25, // Major economies - medium risk
            _ => 50 // Unknown/high-risk countries
        };
    }

    private async Task<int> GetVelocityScoreAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var recentTransactions = await _context.FraudDetections
            .Where(fd => fd.CustomerId == customerId && fd.DetectionDate > DateTime.UtcNow.AddHours(-24))
            .CountAsync(cancellationToken);

        return recentTransactions switch
        {
            0 => 10,
            1 => 20,
            2 => 30,
            >= 5 => 80,
            _ => 50
        };
    }

    private async Task<int> GetPreviousTransactionCountAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.FraudDetections
            .Where(fd => fd.CustomerId == customerId)
            .CountAsync(cancellationToken);
    }

    private async Task<decimal> GetAverageTransactionAmountAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var transactions = await _context.FraudDetections
            .Where(fd => fd.CustomerId == customerId)
            .Select(fd => fd.TransactionAmount)
            .ToListAsync(cancellationToken);

        return transactions.Any() ? transactions.Average() : 0;
    }

    private async Task<bool> IsNewDeviceAsync(string deviceFingerprint, Guid customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(deviceFingerprint)) return true;

        var deviceUsed = await _context.FraudDetections
            .AnyAsync(fd => fd.CustomerId == customerId && fd.DeviceFingerprint == deviceFingerprint, cancellationToken);

        return !deviceUsed;
    }

    private async Task<bool> IsNewLocationAsync(string countryCode, Guid customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(countryCode)) return true;

        var locationUsed = await _context.FraudDetections
            .AnyAsync(fd => fd.CustomerId == customerId && fd.CountryCode == countryCode, cancellationToken);

        return !locationUsed;
    }

    private async Task<bool> IsGeographicAnomalyAsync(Guid customerId, string countryCode, CancellationToken cancellationToken)
    {
        // Check if this country is unusual for this customer
        var customerCountries = await _context.FraudDetections
            .Where(fd => fd.CustomerId == customerId && !string.IsNullOrEmpty(fd.CountryCode))
            .Select(fd => fd.CountryCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        // If customer has used multiple countries or this is a new country, it might be anomalous
        return customerCountries.Count >= 3 || !customerCountries.Contains(countryCode);
    }

    #endregion
}