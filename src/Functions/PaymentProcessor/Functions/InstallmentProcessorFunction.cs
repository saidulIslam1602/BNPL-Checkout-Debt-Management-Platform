using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RivertyBNPL.Functions.PaymentProcessor.Services;
using RivertyBNPL.Functions.PaymentProcessor.Configuration;

namespace RivertyBNPL.Functions.PaymentProcessor.Functions;

/// <summary>
/// Azure Function for processing BNPL installment payments
/// Runs daily to collect due installments from Norwegian customers
/// </summary>
public class InstallmentProcessorFunction
{
    private readonly ILogger<InstallmentProcessorFunction> _logger;
    private readonly IInstallmentProcessorService _installmentProcessor;
    private readonly PaymentProcessorOptions _options;

    public InstallmentProcessorFunction(
        ILogger<InstallmentProcessorFunction> logger,
        IInstallmentProcessorService installmentProcessor,
        IOptions<PaymentProcessorOptions> options)
    {
        _logger = logger;
        _installmentProcessor = installmentProcessor;
        _options = options.Value;
    }

    /// <summary>
    /// Daily installment processing - runs at 06:00 Norwegian time
    /// Processes all due installments for BNPL payments
    /// </summary>
    [Function("ProcessDailyInstallments")]
    public async Task ProcessDailyInstallments(
        [TimerTrigger("0 0 6 * * *", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting daily installment processing. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var processingDate = DateTime.UtcNow.Date;
            var result = await _installmentProcessor.ProcessDueInstallmentsAsync(processingDate, correlationId);

            _logger.LogInformation(
                "Daily installment processing completed. Processed: {ProcessedCount}, Failed: {FailedCount}, Total Amount: {TotalAmount} NOK. CorrelationId: {CorrelationId}",
                result.ProcessedCount,
                result.FailedCount,
                result.TotalAmount,
                correlationId);

            // Send summary notification to operations team
            if (result.FailedCount > 0)
            {
                _logger.LogWarning(
                    "Daily installment processing had {FailedCount} failures. Manual review required. CorrelationId: {CorrelationId}",
                    result.FailedCount,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Critical error during daily installment processing. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Azure Functions retry policy
        }
    }

    /// <summary>
    /// Retry failed installments - runs every 4 hours during business hours
    /// Attempts to process installments that failed during the daily run
    /// </summary>
    [Function("RetryFailedInstallments")]
    public async Task RetryFailedInstallments(
        [TimerTrigger("0 0 8,12,16 * * 1-5", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting retry of failed installments. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var result = await _installmentProcessor.RetryFailedInstallmentsAsync(correlationId);

            _logger.LogInformation(
                "Failed installment retry completed. Retried: {RetriedCount}, Succeeded: {SucceededCount}, Still Failed: {StillFailedCount}. CorrelationId: {CorrelationId}",
                result.RetriedCount,
                result.SucceededCount,
                result.StillFailedCount,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during failed installment retry. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Process overdue installments - runs weekly on Mondays
    /// Handles installments that are past due and initiates collection process
    /// </summary>
    [Function("ProcessOverdueInstallments")]
    public async Task ProcessOverdueInstallments(
        [TimerTrigger("0 0 9 * * 1", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting overdue installment processing. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var result = await _installmentProcessor.ProcessOverdueInstallmentsAsync(correlationId);

            _logger.LogInformation(
                "Overdue installment processing completed. Processed: {ProcessedCount}, Escalated: {EscalatedCount}, Total Overdue Amount: {TotalOverdueAmount} NOK. CorrelationId: {CorrelationId}",
                result.ProcessedCount,
                result.EscalatedCount,
                result.TotalOverdueAmount,
                correlationId);

            // Alert if overdue amounts exceed threshold
            if (result.TotalOverdueAmount > _options.OverdueAmountAlertThreshold)
            {
                _logger.LogWarning(
                    "Total overdue amount {TotalOverdueAmount} NOK exceeds alert threshold {Threshold} NOK. CorrelationId: {CorrelationId}",
                    result.TotalOverdueAmount,
                    _options.OverdueAmountAlertThreshold,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during overdue installment processing. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Manual installment processing trigger
    /// Allows operations team to manually trigger installment processing
    /// </summary>
    [Function("ManualInstallmentProcessing")]
    public async Task<string> ManualInstallmentProcessing(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "installments/process")] HttpRequestData req,
        FunctionContext context)
    {
        var correlationId = req.Headers.GetValues("X-Correlation-ID").FirstOrDefault() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Manual installment processing triggered. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            // Parse request body for specific date or use today
            var requestBody = await req.ReadAsStringAsync();
            var processingDate = DateTime.UtcNow.Date;

            if (!string.IsNullOrEmpty(requestBody))
            {
                var requestData = System.Text.Json.JsonSerializer.Deserialize<ManualProcessingRequest>(requestBody);
                if (requestData?.ProcessingDate.HasValue == true)
                {
                    processingDate = requestData.ProcessingDate.Value.Date;
                }
            }

            var result = await _installmentProcessor.ProcessDueInstallmentsAsync(processingDate, correlationId);

            var response = new
            {
                Success = true,
                ProcessingDate = processingDate,
                ProcessedCount = result.ProcessedCount,
                FailedCount = result.FailedCount,
                TotalAmount = result.TotalAmount,
                CorrelationId = correlationId
            };

            _logger.LogInformation(
                "Manual installment processing completed. Processed: {ProcessedCount}, Failed: {FailedCount}. CorrelationId: {CorrelationId}",
                result.ProcessedCount,
                result.FailedCount,
                correlationId);

            return System.Text.Json.JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during manual installment processing. CorrelationId: {CorrelationId}", 
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
    /// Health check endpoint for installment processor
    /// </summary>
    [Function("InstallmentProcessorHealthCheck")]
    public async Task<string> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "installments/health")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            var healthStatus = await _installmentProcessor.GetHealthStatusAsync();
            
            var response = new
            {
                Status = healthStatus.IsHealthy ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Details = healthStatus.Details,
                Version = typeof(InstallmentProcessorFunction).Assembly.GetName().Version?.ToString()
            };

            return System.Text.Json.JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            
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
/// Request model for manual processing
/// </summary>
public class ManualProcessingRequest
{
    public DateTime? ProcessingDate { get; set; }
    public bool ForceReprocessing { get; set; }
    public List<string>? SpecificInstallmentIds { get; set; }
}