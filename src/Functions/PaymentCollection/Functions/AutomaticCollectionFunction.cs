using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using YourCompanyBNPL.PaymentCollection.Functions.Services;

namespace YourCompanyBNPL.PaymentCollection.Functions.Functions;

/// <summary>
/// Azure Function for automatic payment collection
/// </summary>
public class AutomaticCollectionFunction
{
    private readonly ILogger<AutomaticCollectionFunction> _logger;
    private readonly IAutomaticCollectionService _collectionService;
    private readonly IOverdueProcessingService _overdueService;
    private readonly ISettlementProcessingService _settlementService;

    public AutomaticCollectionFunction(
        ILogger<AutomaticCollectionFunction> logger,
        IAutomaticCollectionService collectionService,
        IOverdueProcessingService overdueService,
        ISettlementProcessingService settlementService)
    {
        _logger = logger;
        _collectionService = collectionService;
        _overdueService = overdueService;
        _settlementService = settlementService;
    }

    /// <summary>
    /// Runs every day at 6:00 AM to process automatic collections
    /// </summary>
    [Function("DailyAutomaticCollection")]
    public async Task RunDailyCollection([TimerTrigger("0 0 6 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Daily automatic collection function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Process BNPL installments due today
            var bnplCollections = await _collectionService.ProcessBNPLInstallmentsAsync();
            _logger.LogInformation("Processed {Count} BNPL installment collections", bnplCollections.Data);

            // Process recurring payments due today
            var recurringCollections = await _collectionService.ProcessRecurringPaymentsAsync();
            _logger.LogInformation("Processed {Count} recurring payment collections", recurringCollections.Data);

            // Process direct debit collections
            var directDebitCollections = await _collectionService.ProcessDirectDebitCollectionsAsync();
            _logger.LogInformation("Processed {Count} direct debit collections", directDebitCollections.Data);

            _logger.LogInformation("Daily automatic collection function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during daily automatic collection");
            throw;
        }
    }

    /// <summary>
    /// Runs every day at 10:00 PM to process overdue payments
    /// </summary>
    [Function("DailyOverdueProcessing")]
    public async Task RunOverdueProcessing([TimerTrigger("0 0 22 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Daily overdue processing function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Mark payments as overdue
            var overduePayments = await _overdueService.MarkPaymentsAsOverdueAsync();
            _logger.LogInformation("Marked {Count} payments as overdue", overduePayments.Data);

            // Calculate and apply late fees
            var lateFees = await _overdueService.ApplyLateFeesAsync();
            _logger.LogInformation("Applied late fees to {Count} overdue payments", lateFees.Data);

            // Update customer risk profiles
            var riskUpdates = await _overdueService.UpdateCustomerRiskProfilesAsync();
            _logger.LogInformation("Updated risk profiles for {Count} customers", riskUpdates.Data);

            // Trigger collection workflows for severely overdue accounts
            var collectionWorkflows = await _overdueService.TriggerCollectionWorkflowsAsync();
            _logger.LogInformation("Triggered collection workflows for {Count} accounts", collectionWorkflows.Data);

            _logger.LogInformation("Daily overdue processing function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during daily overdue processing");
            throw;
        }
    }

    /// <summary>
    /// Runs every Monday at 8:00 AM to process weekly settlements
    /// </summary>
    [Function("WeeklySettlementProcessing")]
    public async Task RunWeeklySettlement([TimerTrigger("0 0 8 * * MON")] TimerInfo timer)
    {
        _logger.LogInformation("Weekly settlement processing function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Create settlements for the previous week
            var settlements = await _settlementService.CreateWeeklySettlementsAsync();
            _logger.LogInformation("Created {Count} weekly settlements", settlements.Data);

            // Process settlement payments to merchants
            var settlementPayments = await _settlementService.ProcessSettlementPaymentsAsync();
            _logger.LogInformation("Processed {Count} settlement payments", settlementPayments.Data);

            // Generate settlement reports
            var reports = await _settlementService.GenerateSettlementReportsAsync();
            _logger.LogInformation("Generated {Count} settlement reports", reports.Data);

            _logger.LogInformation("Weekly settlement processing function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during weekly settlement processing");
            throw;
        }
    }

    /// <summary>
    /// Runs every hour to retry failed collections
    /// </summary>
    [Function("RetryFailedCollections")]
    public async Task RunRetryFailedCollections([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Retry failed collections function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Retry failed BNPL collections
            var bnplRetries = await _collectionService.RetryFailedBNPLCollectionsAsync();
            _logger.LogInformation("Retried {Count} failed BNPL collections", bnplRetries.Data);

            // Retry failed recurring payment collections
            var recurringRetries = await _collectionService.RetryFailedRecurringCollectionsAsync();
            _logger.LogInformation("Retried {Count} failed recurring collections", recurringRetries.Data);

            // Retry failed direct debit collections
            var directDebitRetries = await _collectionService.RetryFailedDirectDebitCollectionsAsync();
            _logger.LogInformation("Retried {Count} failed direct debit collections", directDebitRetries.Data);

            _logger.LogInformation("Retry failed collections function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during retry failed collections");
            throw;
        }
    }

    /// <summary>
    /// Runs monthly on the 1st at 2:00 AM for monthly processing
    /// </summary>
    [Function("MonthlyProcessing")]
    public async Task RunMonthlyProcessing([TimerTrigger("0 0 2 1 * *")] TimerInfo timer)
    {
        _logger.LogInformation("Monthly processing function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Generate monthly collection reports
            var collectionReports = await _collectionService.GenerateMonthlyCollectionReportsAsync();
            _logger.LogInformation("Generated {Count} monthly collection reports", collectionReports.Data);

            // Update customer credit limits based on payment behavior
            var creditLimitUpdates = await _overdueService.UpdateCustomerCreditLimitsAsync();
            _logger.LogInformation("Updated credit limits for {Count} customers", creditLimitUpdates.Data);

            // Archive old payment data
            var archivedRecords = await _collectionService.ArchiveOldPaymentDataAsync();
            _logger.LogInformation("Archived {Count} old payment records", archivedRecords.Data);

            // Generate regulatory reports
            var regulatoryReports = await _collectionService.GenerateRegulatoryReportsAsync();
            _logger.LogInformation("Generated {Count} regulatory reports", regulatoryReports.Data);

            _logger.LogInformation("Monthly processing function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during monthly processing");
            throw;
        }
    }

    /// <summary>
    /// Manual trigger for collection processing (for testing or emergency use)
    /// </summary>
    [Function("ManualCollectionTrigger")]
    public async Task<string> RunManualCollection(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Manual collection trigger function started");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var collectionType = query["type"] ?? "bnpl";
            var forceRetry = bool.TryParse(query["forceRetry"], out var retry) && retry;

            CollectionResult result = collectionType.ToLower() switch
            {
                "bnpl" => new CollectionResult { ProcessedCount = (await _collectionService.ProcessBNPLInstallmentsAsync()).Data },
                "recurring" => new CollectionResult { ProcessedCount = (await _collectionService.ProcessRecurringPaymentsAsync()).Data },
                "directdebit" => new CollectionResult { ProcessedCount = (await _collectionService.ProcessDirectDebitCollectionsAsync()).Data },
                "overdue" => await ProcessOverdueManually(),
                "settlement" => await ProcessSettlementManually(),
                _ => new CollectionResult { ProcessedCount = (await _collectionService.ProcessBNPLInstallmentsAsync()).Data }
            };

            var response = $"Manual {collectionType} collection completed. Processed: {result.ProcessedCount}, Successful: {result.SuccessfulCount}, Failed: {result.FailedCount}";
            _logger.LogInformation(response);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in manual collection trigger");
            throw;
        }
    }

    private async Task<CollectionResult> ProcessOverdueManually()
    {
        var overduePayments = await _overdueService.MarkPaymentsAsOverdueAsync();
        var lateFees = await _overdueService.ApplyLateFeesAsync();
        
        return new CollectionResult
        {
            ProcessedCount = overduePayments.Data + lateFees.Data,
            SuccessfulCount = overduePayments.Data + lateFees.Data,
            FailedCount = 0
        };
    }

    private async Task<CollectionResult> ProcessSettlementManually()
    {
        var settlements = await _settlementService.CreateWeeklySettlementsAsync();
        var payments = await _settlementService.ProcessSettlementPaymentsAsync();
        
        return new CollectionResult
        {
            ProcessedCount = settlements.Data,
            SuccessfulCount = payments.Data,
            FailedCount = 0
        };
    }
}

public class CollectionResult
{
    public int ProcessedCount { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
}