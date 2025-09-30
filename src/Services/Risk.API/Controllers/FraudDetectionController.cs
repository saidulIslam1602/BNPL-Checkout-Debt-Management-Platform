using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Risk.API.Controllers;

/// <summary>
/// Controller for fraud detection operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class FraudDetectionController : ControllerBase
{
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        IFraudDetectionService fraudDetectionService,
        ILogger<FraudDetectionController> logger)
    {
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Performs real-time fraud detection on transaction
    /// </summary>
    /// <param name="request">Fraud detection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fraud detection result</returns>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(ApiResponse<FraudDetectionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<FraudDetectionResponse>>> DetectFraud(
        [FromBody] FraudDetectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting fraud detection for transaction {TransactionId} from customer {CustomerId}",
                request.TransactionId, request.CustomerId);

            var result = await _fraudDetectionService.DetectFraudAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Fraud detection completed for transaction {TransactionId}. Risk Level: {RiskLevel}, Blocked: {IsBlocked}",
                    request.TransactionId, result.Data?.FraudRiskLevel, result.Data?.IsBlocked);
                return Ok(result);
            }

            _logger.LogWarning("Fraud detection failed for transaction {TransactionId}: {Error}", request.TransactionId, result.Message);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fraud detection for transaction {TransactionId}", request.TransactionId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred during fraud detection", 500));
        }
    }

    /// <summary>
    /// Gets fraud detection result by ID
    /// </summary>
    /// <param name="detectionId">Detection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fraud detection details</returns>
    [HttpGet("{detectionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FraudDetectionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<FraudDetectionResponse>>> GetFraudDetection(
        [FromRoute] Guid detectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving fraud detection {DetectionId}", detectionId);

            var result = await _fraudDetectionService.GetFraudDetectionAsync(detectionId, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud detection {DetectionId}", detectionId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving fraud detection", 500));
        }
    }

    /// <summary>
    /// Searches fraud detections with filtering
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedApiResponse<FraudDetectionResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<PagedApiResponse<FraudDetectionResponse>>> SearchFraudDetections(
        [FromBody] FraudDetectionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching fraud detections with criteria: {Criteria}",
                System.Text.Json.JsonSerializer.Serialize(request));

            var result = await _fraudDetectionService.SearchFraudDetectionsAsync(request, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching fraud detections");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while searching fraud detections", 500));
        }
    }

    /// <summary>
    /// Updates fraud rules and thresholds
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("rules/update")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> UpdateFraudRules(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating fraud rules and thresholds");

            var result = await _fraudDetectionService.UpdateFraudRulesAsync(cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Fraud rules updated successfully");
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rules");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while updating fraud rules", 500));
        }
    }

    /// <summary>
    /// Gets fraud detection statistics for a customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="days">Number of days to look back (default: 30, max: 365)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer fraud statistics</returns>
    [HttpGet("customer/{customerId:guid}/stats")]
    [ProducesResponseType(typeof(ApiResponse<CustomerFraudStats>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CustomerFraudStats>>> GetCustomerFraudStats(
        [FromRoute] Guid customerId,
        [FromQuery, Range(1, 365)] int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving fraud statistics for customer {CustomerId} for last {Days} days",
                customerId, days);

            // This would be implemented in the service
            var stats = new CustomerFraudStats
            {
                CustomerId = customerId,
                PeriodDays = days,
                TotalTransactions = 0,
                FraudChecks = 0,
                BlockedTransactions = 0,
                AverageFraudScore = 0,
                HighRiskTransactions = 0,
                LastFraudCheck = null
            };

            return Ok(ApiResponse<CustomerFraudStats>.SuccessResult(stats, "Customer fraud statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud statistics for customer {CustomerId}", customerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving fraud statistics", 500));
        }
    }

    /// <summary>
    /// Gets real-time fraud monitoring dashboard data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time fraud monitoring data</returns>
    [HttpGet("monitoring/dashboard")]
    [ProducesResponseType(typeof(ApiResponse<FraudMonitoringDashboard>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<FraudMonitoringDashboard>>> GetFraudMonitoringDashboard(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving fraud monitoring dashboard data");

            // This would be implemented in the service with real-time data
            var dashboard = new FraudMonitoringDashboard
            {
                LastUpdated = DateTime.UtcNow,
                TotalTransactionsToday = 0,
                BlockedTransactionsToday = 0,
                FraudRateToday = 0,
                AverageFraudScoreToday = 0,
                TopFraudRulesToday = new Dictionary<string, int>(),
                HourlyFraudTrends = new Dictionary<int, FraudHourlyStats>(),
                RecentHighRiskTransactions = new List<RecentHighRiskTransaction>()
            };

            return Ok(ApiResponse<FraudMonitoringDashboard>.SuccessResult(dashboard, "Fraud monitoring dashboard data retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraud monitoring dashboard data");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving dashboard data", 500));
        }
    }
}

/// <summary>
/// Customer fraud statistics
/// </summary>
public class CustomerFraudStats
{
    public Guid CustomerId { get; set; }
    public int PeriodDays { get; set; }
    public int TotalTransactions { get; set; }
    public int FraudChecks { get; set; }
    public int BlockedTransactions { get; set; }
    public decimal AverageFraudScore { get; set; }
    public int HighRiskTransactions { get; set; }
    public DateTime? LastFraudCheck { get; set; }
    public decimal FraudRate => TotalTransactions > 0 ? (decimal)BlockedTransactions / TotalTransactions * 100 : 0;
}

/// <summary>
/// Real-time fraud monitoring dashboard
/// </summary>
public class FraudMonitoringDashboard
{
    public DateTime LastUpdated { get; set; }
    public int TotalTransactionsToday { get; set; }
    public int BlockedTransactionsToday { get; set; }
    public decimal FraudRateToday { get; set; }
    public decimal AverageFraudScoreToday { get; set; }
    public Dictionary<string, int> TopFraudRulesToday { get; set; } = new();
    public Dictionary<int, FraudHourlyStats> HourlyFraudTrends { get; set; } = new();
    public List<RecentHighRiskTransaction> RecentHighRiskTransactions { get; set; } = new();
}

/// <summary>
/// Hourly fraud statistics
/// </summary>
public class FraudHourlyStats
{
    public int Hour { get; set; }
    public int TotalTransactions { get; set; }
    public int BlockedTransactions { get; set; }
    public decimal AverageFraudScore { get; set; }
}

/// <summary>
/// Recent high-risk transaction
/// </summary>
public class RecentHighRiskTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int FraudScore { get; set; }
    public DateTime DetectionTime { get; set; }
    public bool IsBlocked { get; set; }
    public List<string> TriggeredRules { get; set; } = new();
}