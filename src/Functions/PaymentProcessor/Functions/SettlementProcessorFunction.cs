using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RivertyBNPL.Functions.PaymentProcessor.Services;
using RivertyBNPL.Functions.PaymentProcessor.Configuration;

namespace RivertyBNPL.Functions.PaymentProcessor.Functions;

/// <summary>
/// Azure Function for processing merchant settlements
/// Handles automated payouts to Norwegian merchants via DNB Open Banking
/// </summary>
public class SettlementProcessorFunction
{
    private readonly ILogger<SettlementProcessorFunction> _logger;
    private readonly ISettlementProcessorService _settlementProcessor;
    private readonly SettlementOptions _options;

    public SettlementProcessorFunction(
        ILogger<SettlementProcessorFunction> logger,
        ISettlementProcessorService settlementProcessor,
        IOptions<SettlementOptions> options)
    {
        _logger = logger;
        _settlementProcessor = settlementProcessor;
        _options = options.Value;
    }

    /// <summary>
    /// Daily settlement processing - runs at 14:00 Norwegian time (after banking hours)
    /// Processes all pending settlements for Norwegian merchants
    /// </summary>
    [Function("ProcessDailySettlements")]
    public async Task ProcessDailySettlements(
        [TimerTrigger("0 0 14 * * 1-5", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting daily settlement processing. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var settlementDate = DateTime.UtcNow.Date;
            var result = await _settlementProcessor.ProcessDailySettlementsAsync(settlementDate, correlationId);

            _logger.LogInformation(
                "Daily settlement processing completed. Merchants: {MerchantCount}, Settlements: {SettlementCount}, Total Amount: {TotalAmount} NOK, Failed: {FailedCount}. CorrelationId: {CorrelationId}",
                result.MerchantCount,
                result.SettlementCount,
                result.TotalAmount,
                result.FailedCount,
                correlationId);

            // Alert if settlement failures exceed threshold
            if (result.FailedCount > _options.FailureAlertThreshold)
            {
                _logger.LogWarning(
                    "Settlement failures {FailedCount} exceed threshold {Threshold}. Manual review required. CorrelationId: {CorrelationId}",
                    result.FailedCount,
                    _options.FailureAlertThreshold,
                    correlationId);
            }

            // Generate settlement reports
            await _settlementProcessor.GenerateSettlementReportsAsync(settlementDate, result, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Critical error during daily settlement processing. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Azure Functions retry policy
        }
    }

    /// <summary>
    /// Weekly settlement reconciliation - runs on Mondays at 09:00
    /// Reconciles settlements with bank statements and merchant reports
    /// </summary>
    [Function("ReconcileWeeklySettlements")]
    public async Task ReconcileWeeklySettlements(
        [TimerTrigger("0 0 9 * * 1", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting weekly settlement reconciliation. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var weekEndDate = DateTime.UtcNow.Date.AddDays(-1); // Previous Sunday
            var weekStartDate = weekEndDate.AddDays(-6); // Previous Monday

            var result = await _settlementProcessor.ReconcileSettlementsAsync(weekStartDate, weekEndDate, correlationId);

            _logger.LogInformation(
                "Weekly settlement reconciliation completed. Reconciled: {ReconciledCount}, Discrepancies: {DiscrepancyCount}, Total Variance: {TotalVariance} NOK. CorrelationId: {CorrelationId}",
                result.ReconciledCount,
                result.DiscrepancyCount,
                result.TotalVariance,
                correlationId);

            // Alert if discrepancies found
            if (result.DiscrepancyCount > 0 || Math.Abs(result.TotalVariance) > _options.VarianceAlertThreshold)
            {
                _logger.LogWarning(
                    "Settlement discrepancies detected. Count: {DiscrepancyCount}, Variance: {TotalVariance} NOK. CorrelationId: {CorrelationId}",
                    result.DiscrepancyCount,
                    result.TotalVariance,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during weekly settlement reconciliation. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Process instant settlements for high-value merchants
    /// Triggered by Service Bus message for immediate payouts
    /// </summary>
    [Function("ProcessInstantSettlement")]
    public async Task ProcessInstantSettlement(
        [ServiceBusTrigger("instant-settlements", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Processing instant settlement request. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var settlementRequest = System.Text.Json.JsonSerializer.Deserialize<InstantSettlementRequest>(message);
            if (settlementRequest == null)
            {
                _logger.LogWarning("Invalid instant settlement request format. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            // Validate merchant eligibility for instant settlements
            var isEligible = await _settlementProcessor.ValidateInstantSettlementEligibilityAsync(
                settlementRequest.MerchantId, correlationId);

            if (!isEligible)
            {
                _logger.LogWarning(
                    "Merchant {MerchantId} not eligible for instant settlement. CorrelationId: {CorrelationId}",
                    settlementRequest.MerchantId,
                    correlationId);
                return;
            }

            var result = await _settlementProcessor.ProcessInstantSettlementAsync(settlementRequest, correlationId);

            _logger.LogInformation(
                "Instant settlement processed for merchant {MerchantId}. Amount: {Amount} NOK, Success: {Success}. CorrelationId: {CorrelationId}",
                settlementRequest.MerchantId,
                settlementRequest.Amount,
                result.Success,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing instant settlement. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Service Bus retry
        }
    }

    /// <summary>
    /// Generate monthly settlement reports - runs on the 1st of each month at 15:00
    /// Creates comprehensive settlement reports for merchants and internal use
    /// </summary>
    [Function("GenerateMonthlySettlementReports")]
    public async Task GenerateMonthlySettlementReports(
        [TimerTrigger("0 0 15 1 * *", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting monthly settlement report generation. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            var monthStart = new DateTime(previousMonth.Year, previousMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var result = await _settlementProcessor.GenerateMonthlyReportsAsync(monthStart, monthEnd, correlationId);

            _logger.LogInformation(
                "Monthly settlement reports generated. Merchant Reports: {MerchantReportCount}, Internal Reports: {InternalReportCount}, Total Settlements: {TotalSettlements} NOK. CorrelationId: {CorrelationId}",
                result.MerchantReportCount,
                result.InternalReportCount,
                result.TotalSettlementAmount,
                correlationId);

            // Send reports to merchants and stakeholders
            await _settlementProcessor.DistributeMonthlyReportsAsync(result, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error generating monthly settlement reports. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Handle settlement failures and retries
    /// Processes failed settlements and attempts recovery
    /// </summary>
    [Function("HandleSettlementFailures")]
    public async Task HandleSettlementFailures(
        [ServiceBusTrigger("settlement-failures", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Processing settlement failure. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var failureMessage = System.Text.Json.JsonSerializer.Deserialize<SettlementFailureMessage>(message);
            if (failureMessage == null)
            {
                _logger.LogWarning("Invalid settlement failure message format. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            var result = await _settlementProcessor.HandleSettlementFailureAsync(failureMessage, correlationId);

            _logger.LogInformation(
                "Settlement failure handled for settlement {SettlementId}. Retry Scheduled: {RetryScheduled}, Escalated: {Escalated}. CorrelationId: {CorrelationId}",
                failureMessage.SettlementId,
                result.RetryScheduled,
                result.Escalated,
                correlationId);

            // Escalate to operations team if maximum retries exceeded
            if (result.Escalated)
            {
                _logger.LogWarning(
                    "Settlement {SettlementId} escalated to operations team after {RetryCount} failed attempts. CorrelationId: {CorrelationId}",
                    failureMessage.SettlementId,
                    failureMessage.RetryCount,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling settlement failure. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Service Bus retry
        }
    }

    /// <summary>
    /// Manual settlement processing for specific merchants
    /// Allows operations team to trigger settlements outside normal schedule
    /// </summary>
    [Function("ProcessManualSettlement")]
    public async Task<string> ProcessManualSettlement(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "settlements/process")] HttpRequestData req,
        FunctionContext context)
    {
        var correlationId = req.Headers.GetValues("X-Correlation-ID").FirstOrDefault() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Manual settlement processing requested. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var settlementRequest = System.Text.Json.JsonSerializer.Deserialize<ManualSettlementRequest>(requestBody);

            if (settlementRequest == null)
            {
                var errorResponse = new { Success = false, ErrorMessage = "Invalid request format" };
                return System.Text.Json.JsonSerializer.Serialize(errorResponse);
            }

            var result = await _settlementProcessor.ProcessManualSettlementAsync(settlementRequest, correlationId);

            var response = new
            {
                Success = result.Success,
                SettlementId = result.SettlementId,
                Amount = result.Amount,
                ProcessedAt = result.ProcessedAt,
                BankTransactionId = result.BankTransactionId,
                CorrelationId = correlationId
            };

            _logger.LogInformation(
                "Manual settlement processed for merchant {MerchantId}. Amount: {Amount} NOK, Success: {Success}. CorrelationId: {CorrelationId}",
                settlementRequest.MerchantId,
                result.Amount,
                result.Success,
                correlationId);

            return System.Text.Json.JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing manual settlement. CorrelationId: {CorrelationId}", 
                correlationId);

            var errorResponse = new
            {
                Success = false,
                ErrorMessage = ex.Message,
                CorrelationId = correlationId
            };

            return System.Text.Json.JsonSerializer.Serialize(errorResponse);
        }
    }

    /// <summary>
    /// Settlement health check and status endpoint
    /// </summary>
    [Function("SettlementHealthCheck")]
    public async Task<string> SettlementHealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settlements/health")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            var healthStatus = await _settlementProcessor.GetHealthStatusAsync();
            
            var response = new
            {
                Status = healthStatus.IsHealthy ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                BankConnectivity = healthStatus.BankConnectivity,
                PendingSettlements = healthStatus.PendingSettlementsCount,
                FailedSettlements = healthStatus.FailedSettlementsCount,
                LastSuccessfulRun = healthStatus.LastSuccessfulRun,
                Version = typeof(SettlementProcessorFunction).Assembly.GetName().Version?.ToString()
            };

            return System.Text.Json.JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during settlement health check");
            
            var errorResponse = new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };

            return System.Text.Json.JsonSerializer.Serialize(errorResponse);
        }
    }
}

/// <summary>
/// Message models for settlement processing
/// </summary>
public class InstantSettlementRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SettlementFailureMessage
{
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; }
}

public class ManualSettlementRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public DateTime? SettlementDate { get; set; }
    public bool ForceProcessing { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string>? SpecificTransactionIds { get; set; }
}