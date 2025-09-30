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
/// Service for risk assessment operations
/// </summary>
public class RiskAssessmentService : IRiskAssessmentService
{
    private readonly RiskDbContext _context;
    private readonly ICreditBureauService _creditBureauService;
    private readonly IMachineLearningService _machineLearningService;
    private readonly IMapper _mapper;
    private readonly ILogger<RiskAssessmentService> _logger;

    public RiskAssessmentService(
        RiskDbContext context,
        ICreditBureauService creditBureauService,
        IMachineLearningService machineLearningService,
        IMapper mapper,
        ILogger<RiskAssessmentService> logger)
    {
        _context = context;
        _creditBureauService = creditBureauService;
        _machineLearningService = machineLearningService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CreditAssessmentResponse>> AssessCreditRiskAsync(CreditAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting credit assessment for customer {CustomerId}", request.CustomerId);

            // Check if customer has recent assessment
            var recentAssessment = await _context.CreditAssessments
                .Where(ca => ca.CustomerId == request.CustomerId && 
                           ca.AssessmentDate > DateTime.UtcNow.AddHours(-24) &&
                           ca.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(ca => ca.AssessmentDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (recentAssessment != null)
            {
                _logger.LogInformation("Using recent assessment for customer {CustomerId}", request.CustomerId);
                var recentResponse = await MapToResponseAsync(recentAssessment, cancellationToken);
                return ApiResponse<CreditAssessmentResponse>.SuccessResult(recentResponse, "Recent credit assessment retrieved");
            }

            // Create new assessment
            var assessment = new CreditAssessment
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                SocialSecurityNumber = request.SocialSecurityNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                RequestedAmount = request.RequestedAmount,
                Currency = request.Currency,
                PlanType = request.PlanType,
                AnnualIncome = request.AnnualIncome,
                MonthlyExpenses = request.MonthlyExpenses,
                ExistingDebt = request.ExistingDebt,
                ExistingCreditAccounts = request.ExistingCreditAccounts,
                PaymentHistoryMonths = request.PaymentHistoryMonths,
                LatePaymentsLast12Months = request.LatePaymentsLast12Months,
                HasBankruptcy = request.HasBankruptcy,
                HasCollections = request.HasCollections,
                AssessmentDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Get credit bureau data
            var creditBureauData = await GetCreditBureauDataAsync(request, cancellationToken);
            if (creditBureauData != null)
            {
                assessment.CreditScore = creditBureauData.CreditScore;
                
                // Store credit bureau response
                var bureauResponse = new CreditBureauResponse
                {
                    Id = Guid.NewGuid(),
                    CreditAssessmentId = assessment.Id,
                    BureauName = creditBureauData.BureauName,
                    RequestId = Guid.NewGuid().ToString(),
                    ResponseDate = DateTime.UtcNow,
                    CreditScore = creditBureauData.CreditScore,
                    RawResponse = System.Text.Json.JsonSerializer.Serialize(creditBureauData)
                };
                assessment.CreditBureauResponses.Add(bureauResponse);
            }
            else
            {
                // Use estimated credit score if bureau data unavailable
                assessment.CreditScore = EstimateCreditScore(request);
            }

            // Apply risk rules and calculate risk factors
            var riskFactors = await CalculateRiskFactorsAsync(assessment, cancellationToken);
            foreach (var factor in riskFactors)
            {
                assessment.RiskFactors.Add(factor);
            }

            // Use ML model for risk prediction
            var mlRiskScore = await GetMLRiskScoreAsync(assessment, cancellationToken);

            // Calculate final risk assessment
            var riskAssessment = CalculateFinalRiskAssessment(assessment, riskFactors, mlRiskScore);
            assessment.RiskLevel = riskAssessment.RiskLevel;
            assessment.CreditRating = riskAssessment.CreditRating;
            assessment.RecommendedCreditLimit = riskAssessment.RecommendedCreditLimit;
            assessment.IsApproved = riskAssessment.IsApproved;
            assessment.DeclineReason = riskAssessment.DeclineReason;
            assessment.InterestRate = riskAssessment.InterestRate;

            // Save assessment
            _context.CreditAssessments.Add(assessment);
            await _context.SaveChangesAsync(cancellationToken);

            // Update customer risk profile
            await UpdateCustomerRiskProfileAsync(request.CustomerId, assessment, cancellationToken);

            _logger.LogInformation("Credit assessment completed for customer {CustomerId}. Approved: {IsApproved}, Score: {CreditScore}",
                request.CustomerId, assessment.IsApproved, assessment.CreditScore);

            var response = await MapToResponseAsync(assessment, cancellationToken);
            return ApiResponse<CreditAssessmentResponse>.SuccessResult(response, "Credit assessment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credit assessment for customer {CustomerId}", request.CustomerId);
            return ApiResponse<CreditAssessmentResponse>.ErrorResult("An error occurred during credit assessment", 500);
        }
    }

    public async Task<ApiResponse<CreditAssessmentResponse>> GetCreditAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var assessment = await _context.CreditAssessments
                .Include(ca => ca.RiskFactors)
                .Include(ca => ca.CreditBureauResponses)
                .FirstOrDefaultAsync(ca => ca.Id == assessmentId, cancellationToken);

            if (assessment == null)
            {
                return ApiResponse<CreditAssessmentResponse>.ErrorResult("Credit assessment not found", 404);
            }

            var response = await MapToResponseAsync(assessment, cancellationToken);
            return ApiResponse<CreditAssessmentResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit assessment {AssessmentId}", assessmentId);
            return ApiResponse<CreditAssessmentResponse>.ErrorResult("An error occurred while retrieving credit assessment", 500);
        }
    }

    public async Task<PagedApiResponse<CreditAssessmentResponse>> GetCustomerAssessmentsAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CreditAssessments
                .Where(ca => ca.CustomerId == customerId)
                .OrderByDescending(ca => ca.AssessmentDate);

            var totalCount = await query.CountAsync(cancellationToken);
            var assessments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(ca => ca.RiskFactors)
                .Include(ca => ca.CreditBureauResponses)
                .ToListAsync(cancellationToken);

            var responses = new List<CreditAssessmentResponse>();
            foreach (var assessment in assessments)
            {
                responses.Add(await MapToResponseAsync(assessment, cancellationToken));
            }

            return PagedApiResponse<CreditAssessmentResponse>.SuccessResult(
                responses, page, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit assessments for customer {CustomerId}", customerId);
            return PagedApiResponse<CreditAssessmentResponse>.ErrorResult("An error occurred while retrieving customer assessments", 500);
        }
    }

    public async Task<PagedApiResponse<CreditAssessmentResponse>> SearchAssessmentsAsync(RiskAssessmentSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CreditAssessments.AsQueryable();

            // Apply filters
            if (request.CustomerId.HasValue)
                query = query.Where(ca => ca.CustomerId == request.CustomerId.Value);

            if (request.RiskLevel.HasValue)
                query = query.Where(ca => ca.RiskLevel == request.RiskLevel.Value);

            if (request.CreditRating.HasValue)
                query = query.Where(ca => ca.CreditRating == request.CreditRating.Value);

            if (request.IsApproved.HasValue)
                query = query.Where(ca => ca.IsApproved == request.IsApproved.Value);

            if (request.MinCreditScore.HasValue)
                query = query.Where(ca => ca.CreditScore >= request.MinCreditScore.Value);

            if (request.MaxCreditScore.HasValue)
                query = query.Where(ca => ca.CreditScore <= request.MaxCreditScore.Value);

            if (request.FromDate.HasValue)
                query = query.Where(ca => ca.AssessmentDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(ca => ca.AssessmentDate <= request.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
                query = query.Where(ca => ca.SocialSecurityNumber == request.SocialSecurityNumber);

            if (!string.IsNullOrWhiteSpace(request.Email))
                query = query.Where(ca => ca.Email.Contains(request.Email));

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            var totalCount = await query.CountAsync(cancellationToken);
            var assessments = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(ca => ca.RiskFactors)
                .Include(ca => ca.CreditBureauResponses)
                .ToListAsync(cancellationToken);

            var responses = new List<CreditAssessmentResponse>();
            foreach (var assessment in assessments)
            {
                responses.Add(await MapToResponseAsync(assessment, cancellationToken));
            }

            return PagedApiResponse<CreditAssessmentResponse>.SuccessResult(
                responses, request.Page, request.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching credit assessments");
            return PagedApiResponse<CreditAssessmentResponse>.ErrorResult("An error occurred while searching assessments", 500);
        }
    }

    public async Task<ApiResponse<CustomerRiskProfileResponse>> UpdateRiskProfileAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _context.CustomerRiskProfiles
                .FirstOrDefaultAsync(crp => crp.CustomerId == customerId, cancellationToken);

            if (profile == null)
            {
                profile = new CustomerRiskProfile
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId
                };
                _context.CustomerRiskProfiles.Add(profile);
            }

            // Update profile with latest data
            await RecalculateRiskProfileAsync(profile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<CustomerRiskProfileResponse>(profile);
            return ApiResponse<CustomerRiskProfileResponse>.SuccessResult(response, "Risk profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk profile for customer {CustomerId}", customerId);
            return ApiResponse<CustomerRiskProfileResponse>.ErrorResult("An error occurred while updating risk profile", 500);
        }
    }

    public async Task<ApiResponse<CustomerRiskProfileResponse>> GetRiskProfileAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _context.CustomerRiskProfiles
                .FirstOrDefaultAsync(crp => crp.CustomerId == customerId, cancellationToken);

            if (profile == null)
            {
                return ApiResponse<CustomerRiskProfileResponse>.ErrorResult("Risk profile not found", 404);
            }

            var response = _mapper.Map<CustomerRiskProfileResponse>(profile);
            return ApiResponse<CustomerRiskProfileResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk profile for customer {CustomerId}", customerId);
            return ApiResponse<CustomerRiskProfileResponse>.ErrorResult("An error occurred while retrieving risk profile", 500);
        }
    }

    public async Task<ApiResponse<RiskAnalytics>> GetRiskAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var assessments = await _context.CreditAssessments
                .Where(ca => ca.AssessmentDate >= fromDate && ca.AssessmentDate <= toDate)
                .ToListAsync(cancellationToken);

            var fraudDetections = await _context.FraudDetections
                .Where(fd => fd.DetectionDate >= fromDate && fd.DetectionDate <= toDate)
                .ToListAsync(cancellationToken);

            var analytics = new RiskAnalytics
            {
                TotalAssessments = assessments.Count,
                ApprovedAssessments = assessments.Count(a => a.IsApproved),
                DeclinedAssessments = assessments.Count(a => !a.IsApproved),
                ApprovalRate = assessments.Count > 0 ? (decimal)assessments.Count(a => a.IsApproved) / assessments.Count * 100 : 0,
                AverageCreditScore = assessments.Count > 0 ? (decimal)assessments.Average(a => a.CreditScore) : 0,
                RiskLevelDistribution = assessments.GroupBy(a => a.RiskLevel).ToDictionary(g => g.Key, g => g.Count()),
                CreditRatingDistribution = assessments.GroupBy(a => a.CreditRating).ToDictionary(g => g.Key, g => g.Count()),
                DeclineReasons = assessments.Where(a => !string.IsNullOrEmpty(a.DeclineReason))
                    .GroupBy(a => a.DeclineReason!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AssessmentsByDate = assessments.GroupBy(a => a.AssessmentDate.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => (decimal)g.Count()),
                TotalFraudChecks = fraudDetections.Count,
                BlockedTransactions = fraudDetections.Count(f => f.IsBlocked),
                FraudRate = fraudDetections.Count > 0 ? (decimal)fraudDetections.Count(f => f.IsBlocked) / fraudDetections.Count * 100 : 0,
                FraudRiskDistribution = fraudDetections.GroupBy(f => f.FraudRiskLevel).ToDictionary(g => g.Key, g => g.Count()),
                TopFraudRules = new Dictionary<string, int>() // Would need to implement fraud rule tracking
            };

            return ApiResponse<RiskAnalytics>.SuccessResult(analytics, "Risk analytics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk analytics from {FromDate} to {ToDate}", fromDate, toDate);
            return ApiResponse<RiskAnalytics>.ErrorResult("An error occurred while retrieving risk analytics", 500);
        }
    }

    #region Private Methods

    private async Task<CreditBureauSummary?> GetCreditBureauDataAsync(CreditAssessmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var bureauRequest = new CreditBureauRequest
            {
                SocialSecurityNumber = request.SocialSecurityNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                BureauName = "Experian", // Default to Experian
                IncludeFullReport = false
            };

            var result = await _creditBureauService.GetCreditReportAsync(bureauRequest, cancellationToken);
            return result.Success ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get credit bureau data for customer {CustomerId}", request.CustomerId);
            return null;
        }
    }

    private static int EstimateCreditScore(CreditAssessmentRequest request)
    {
        // Simple credit score estimation based on available data
        var baseScore = 650;
        
        // Adjust based on income
        if (request.AnnualIncome > 800000) baseScore += 50;
        else if (request.AnnualIncome > 500000) baseScore += 30;
        else if (request.AnnualIncome < 300000) baseScore -= 30;

        // Adjust based on debt-to-income ratio
        var dtiRatio = request.AnnualIncome > 0 ? request.ExistingDebt / request.AnnualIncome : 1;
        if (dtiRatio > 0.5m) baseScore -= 50;
        else if (dtiRatio > 0.3m) baseScore -= 25;
        else if (dtiRatio < 0.1m) baseScore += 25;

        // Adjust based on payment history
        baseScore -= request.LatePaymentsLast12Months * 15;

        // Adjust based on negative factors
        if (request.HasBankruptcy) baseScore -= 100;
        if (request.HasCollections) baseScore -= 50;

        return Math.Max(300, Math.Min(850, baseScore));
    }

    private async Task<List<RiskFactor>> CalculateRiskFactorsAsync(CreditAssessment assessment, CancellationToken cancellationToken)
    {
        var riskFactors = new List<RiskFactor>();
        var activeRules = await _context.RiskRules.Where(r => r.IsActive).ToListAsync(cancellationToken);

        foreach (var rule in activeRules)
        {
            var factor = EvaluateRiskRule(assessment, rule);
            if (factor != null)
            {
                riskFactors.Add(factor);
            }
        }

        return riskFactors;
    }

    private static RiskFactor? EvaluateRiskRule(CreditAssessment assessment, RiskRule rule)
    {
        var isTriggered = rule.RuleName switch
        {
            "MinimumCreditScore" => assessment.CreditScore < 600,
            "MaximumDebtToIncomeRatio" => assessment.AnnualIncome > 0 && 
                (assessment.ExistingDebt + assessment.RequestedAmount) / assessment.AnnualIncome > 0.4m,
            "RecentBankruptcy" => assessment.HasBankruptcy,
            "RecentLatePayments" => assessment.LatePaymentsLast12Months > 2,
            _ => false
        };

        if (!isTriggered) return null;

        var impact = rule.RuleName switch
        {
            "RecentBankruptcy" => RiskLevel.High,
            "MinimumCreditScore" => RiskLevel.Medium,
            "MaximumDebtToIncomeRatio" => RiskLevel.Medium,
            "RecentLatePayments" => RiskLevel.Low,
            _ => RiskLevel.Low
        };

        return new RiskFactor
        {
            Id = Guid.NewGuid(),
            CreditAssessmentId = assessment.Id,
            FactorType = rule.Category,
            Description = rule.Description,
            Impact = impact,
            Score = rule.MaxScore,
            Weight = rule.Weight
        };
    }

    private async Task<decimal> GetMLRiskScoreAsync(CreditAssessment assessment, CancellationToken cancellationToken)
    {
        try
        {
            var features = new Dictionary<string, object>
            {
                { "CreditScore", assessment.CreditScore },
                { "AnnualIncome", assessment.AnnualIncome },
                { "ExistingDebt", assessment.ExistingDebt },
                { "PaymentHistoryMonths", assessment.PaymentHistoryMonths },
                { "LatePaymentsLast12Months", assessment.LatePaymentsLast12Months },
                { "ExistingCreditAccounts", assessment.ExistingCreditAccounts },
                { "RequestedAmount", assessment.RequestedAmount },
                { "Age", DateTime.UtcNow.Year - assessment.DateOfBirth.Year },
                { "EmploymentLength", 5 }, // Default value
                { "ResidentialStability", 3 }, // Default value
                { "DebtToIncomeRatio", assessment.AnnualIncome > 0 ? assessment.ExistingDebt / assessment.AnnualIncome : 0 },
                { "HasBankruptcy", assessment.HasBankruptcy },
                { "HasCollections", assessment.HasCollections }
            };

            var result = await _machineLearningService.PredictCreditRiskAsync(features, cancellationToken);
            return result.Success ? result.Data : 50; // Default risk score
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get ML risk score for assessment {AssessmentId}", assessment.Id);
            return 50; // Default risk score
        }
    }

    private static (RiskLevel RiskLevel, CreditRating CreditRating, decimal RecommendedCreditLimit, bool IsApproved, string? DeclineReason, decimal InterestRate) CalculateFinalRiskAssessment(
        CreditAssessment assessment, List<RiskFactor> riskFactors, decimal mlRiskScore)
    {
        // Calculate overall risk score
        var ruleBasedScore = riskFactors.Sum(rf => rf.Score * (double)rf.Weight);
        var combinedScore = (ruleBasedScore * 0.6) + ((double)mlRiskScore * 0.4);

        // Determine risk level
        var riskLevel = combinedScore switch
        {
            >= 80 => RiskLevel.High,
            >= 60 => RiskLevel.Medium,
            >= 40 => RiskLevel.Low,
            _ => RiskLevel.VeryLow
        };

        // Determine credit rating based on credit score
        var creditRating = assessment.CreditScore switch
        {
            >= 750 => CreditRating.Excellent,
            >= 700 => CreditRating.Good,
            >= 650 => CreditRating.Fair,
            >= 600 => CreditRating.Poor,
            _ => CreditRating.VeryPoor
        };

        // Calculate recommended credit limit
        var baseLimit = Math.Min(assessment.RequestedAmount * 1.2m, assessment.AnnualIncome * 0.3m);
        var riskAdjustment = riskLevel switch
        {
            RiskLevel.VeryLow => 1.0m,
            RiskLevel.Low => 0.8m,
            RiskLevel.Medium => 0.6m,
            RiskLevel.High => 0.3m,
            _ => 0.1m
        };
        var recommendedLimit = baseLimit * riskAdjustment;

        // Determine approval
        var isApproved = !assessment.HasBankruptcy && 
                        assessment.CreditScore >= 600 && 
                        riskLevel != RiskLevel.High &&
                        assessment.LatePaymentsLast12Months <= 2;

        // Determine decline reason
        string? declineReason = null;
        if (!isApproved)
        {
            if (assessment.HasBankruptcy)
                declineReason = "Recent bankruptcy on record";
            else if (assessment.CreditScore < 600)
                declineReason = "Credit score below minimum requirement";
            else if (riskLevel == RiskLevel.High)
                declineReason = "High risk assessment";
            else if (assessment.LatePaymentsLast12Months > 2)
                declineReason = "Too many recent late payments";
        }

        // Calculate interest rate
        var interestRate = creditRating switch
        {
            CreditRating.Excellent => 0.0599m,
            CreditRating.Good => 0.0799m,
            CreditRating.Fair => 0.0999m,
            CreditRating.Poor => 0.1299m,
            CreditRating.VeryPoor => 0.1599m,
            _ => 0.1599m
        };

        return (riskLevel, creditRating, recommendedLimit, isApproved, declineReason, interestRate);
    }

    private async Task UpdateCustomerRiskProfileAsync(Guid customerId, CreditAssessment assessment, CancellationToken cancellationToken)
    {
        var profile = await _context.CustomerRiskProfiles
            .FirstOrDefaultAsync(crp => crp.CustomerId == customerId, cancellationToken);

        if (profile == null)
        {
            profile = new CustomerRiskProfile
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId
            };
            _context.CustomerRiskProfiles.Add(profile);
        }

        profile.CurrentRiskLevel = assessment.RiskLevel;
        profile.CurrentCreditScore = assessment.CreditScore;
        profile.CurrentCreditRating = assessment.CreditRating;
        profile.LastAssessmentDate = assessment.AssessmentDate;
        profile.NextReviewDate = DateTime.UtcNow.AddDays(90);

        if (assessment.IsApproved)
        {
            profile.TotalCreditLimit += assessment.RecommendedCreditLimit;
            profile.AvailableCreditLimit += assessment.RecommendedCreditLimit;
        }
    }

    private async Task RecalculateRiskProfileAsync(CustomerRiskProfile profile, CancellationToken cancellationToken)
    {
        // This would typically integrate with payment data to update the profile
        // For now, we'll just update the review date
        profile.NextReviewDate = DateTime.UtcNow.AddDays(90);
        await Task.CompletedTask;
    }

    private async Task<CreditAssessmentResponse> MapToResponseAsync(CreditAssessment assessment, CancellationToken cancellationToken)
    {
        var response = _mapper.Map<CreditAssessmentResponse>(assessment);
        
        // Map risk factors
        response.RiskFactors = assessment.RiskFactors.Select(rf => new RiskFactorSummary
        {
            FactorType = rf.FactorType,
            Description = rf.Description,
            Impact = rf.Impact,
            Score = rf.Score,
            Weight = rf.Weight
        }).ToList();

        // Map credit bureau data
        var bureauResponse = assessment.CreditBureauResponses.FirstOrDefault();
        if (bureauResponse != null && !string.IsNullOrEmpty(bureauResponse.RawResponse))
        {
            try
            {
                response.CreditBureauData = System.Text.Json.JsonSerializer.Deserialize<CreditBureauSummary>(bureauResponse.RawResponse);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize credit bureau data for assessment {AssessmentId}", assessment.Id);
            }
        }

        await Task.CompletedTask;
        return response;
    }

    private static IQueryable<CreditAssessment> ApplySorting(IQueryable<CreditAssessment> query, string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return sortDescending ? query.OrderByDescending(ca => ca.AssessmentDate) : query.OrderBy(ca => ca.AssessmentDate);

        Expression<Func<CreditAssessment, object>> keySelector = sortBy.ToLower() switch
        {
            "creditScore" => ca => ca.CreditScore,
            "riskLevel" => ca => ca.RiskLevel,
            "isApproved" => ca => ca.IsApproved,
            "customerId" => ca => ca.CustomerId,
            _ => ca => ca.AssessmentDate
        };

        return sortDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    #endregion
}