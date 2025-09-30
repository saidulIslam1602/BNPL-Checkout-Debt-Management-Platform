using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using YourCompanyBNPL.Functions.PaymentProcessor.Data;
using YourCompanyBNPL.Functions.PaymentProcessor.Configuration;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Functions.PaymentProcessor.Services;

/// <summary>
/// Service for processing BNPL installment payments using real Norwegian banking APIs
/// </summary>
public interface IInstallmentProcessorService
{
    Task<InstallmentProcessingResult> ProcessDueInstallmentsAsync(DateTime processingDate, string correlationId);
    Task<InstallmentRetryResult> RetryFailedInstallmentsAsync(string correlationId);
    Task<OverdueProcessingResult> ProcessOverdueInstallmentsAsync(string correlationId);
    Task<HealthStatus> GetHealthStatusAsync();
}

public class InstallmentProcessorService : IInstallmentProcessorService
{
    private readonly PaymentProcessorDbContext _context;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IRiskAssessmentService _riskAssessment;
    private readonly INotificationService _notificationService;
    private readonly IServiceBusService _serviceBus;
    private readonly ILogger<InstallmentProcessorService> _logger;
    private readonly PaymentProcessorOptions _options;

    public InstallmentProcessorService(
        PaymentProcessorDbContext context,
        IPaymentGatewayService paymentGateway,
        IRiskAssessmentService riskAssessment,
        INotificationService notificationService,
        IServiceBusService serviceBus,
        ILogger<InstallmentProcessorService> logger,
        IOptions<PaymentProcessorOptions> options)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _riskAssessment = riskAssessment;
        _notificationService = notificationService;
        _serviceBus = serviceBus;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<InstallmentProcessingResult> ProcessDueInstallmentsAsync(DateTime processingDate, string correlationId)
    {
        _logger.LogInformation("Starting installment processing for date {ProcessingDate}. CorrelationId: {CorrelationId}", 
            processingDate, correlationId);

        var result = new InstallmentProcessingResult
        {
            ProcessingDate = processingDate,
            CorrelationId = correlationId
        };

        try
        {
            // Get all installments due on the processing date
            var dueInstallments = await _context.Installments
                .Include(i => i.Payment)
                    .ThenInclude(p => p.Customer)
                .Where(i => i.DueDate.Date == processingDate.Date && 
                           i.Status == InstallmentStatus.PENDING)
                .OrderBy(i => i.Payment.CustomerId) // Group by customer for efficiency
                .ToListAsync();

            _logger.LogInformation("Found {InstallmentCount} installments due for processing. CorrelationId: {CorrelationId}", 
                dueInstallments.Count, correlationId);

            // Process installments in batches to avoid overwhelming payment gateways
            var batchSize = _options.InstallmentBatchSize;
            var batches = dueInstallments.Chunk(batchSize);

            foreach (var batch in batches)
            {
                var batchTasks = batch.Select(installment => ProcessSingleInstallmentAsync(installment, correlationId));
                var batchResults = await Task.WhenAll(batchTasks);

                // Aggregate batch results
                foreach (var batchResult in batchResults)
                {
                    if (batchResult.Success)
                    {
                        result.ProcessedCount++;
                        result.TotalAmount += batchResult.Amount;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedInstallments.Add(batchResult);
                    }
                }

                // Add delay between batches to respect rate limits
                if (_options.BatchDelayMilliseconds > 0)
                {
                    await Task.Delay(_options.BatchDelayMilliseconds);
                }
            }

            // Update processing statistics
            await UpdateProcessingStatisticsAsync(result, correlationId);

            _logger.LogInformation("Installment processing completed. Processed: {ProcessedCount}, Failed: {FailedCount}, Total: {TotalAmount} NOK. CorrelationId: {CorrelationId}",
                result.ProcessedCount, result.FailedCount, result.TotalAmount, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during installment processing. CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
    }

    private async Task<InstallmentResult> ProcessSingleInstallmentAsync(Installment installment, string correlationId)
    {
        var installmentResult = new InstallmentResult
        {
            InstallmentId = installment.Id,
            Amount = installment.Amount,
            CustomerId = installment.Payment.CustomerId
        };

        try
        {
            _logger.LogDebug("Processing installment {InstallmentId} for customer {CustomerId}. Amount: {Amount} NOK. CorrelationId: {CorrelationId}",
                installment.Id, installment.Payment.CustomerId, installment.Amount, correlationId);

            // Perform real-time risk assessment before processing
            var riskAssessment = await _riskAssessment.AssessInstallmentRiskAsync(new InstallmentRiskRequest
            {
                InstallmentId = installment.Id,
                CustomerId = installment.Payment.CustomerId,
                Amount = installment.Amount,
                AttemptCount = installment.AttemptCount + 1,
                PaymentHistory = await GetCustomerPaymentHistoryAsync(installment.Payment.CustomerId)
            });

            // Block processing if risk is too high
            if (riskAssessment.RiskLevel == RiskLevel.VERY_HIGH)
            {
                _logger.LogWarning("Installment {InstallmentId} blocked due to high risk. Risk Score: {RiskScore}. CorrelationId: {CorrelationId}",
                    installment.Id, riskAssessment.RiskScore, correlationId);

                installment.Status = InstallmentStatus.FAILED;
                installment.FailureReason = "High risk assessment - manual review required";
                await _context.SaveChangesAsync();

                installmentResult.Success = false;
                installmentResult.ErrorMessage = "Risk assessment failed";
                return installmentResult;
            }

            // Process payment through Norwegian payment gateway
            var paymentRequest = new PaymentGatewayRequest
            {
                CustomerId = installment.Payment.CustomerId,
                Amount = installment.Amount,
                Currency = Currency.NOK,
                PaymentMethod = installment.Payment.PaymentMethod,
                PaymentMethodId = installment.Payment.PaymentMethodId,
                OrderReference = $"INST-{installment.Id}",
                Description = $"BNPL Installment Payment - {installment.Payment.OrderReference}",
                CustomerEmail = installment.Payment.Customer.Email,
                MerchantId = installment.Payment.MerchantId
            };

            var paymentResult = await _paymentGateway.ProcessPaymentAsync(paymentRequest);

            if (paymentResult.Success)
            {
                // Update installment status
                installment.Status = InstallmentStatus.PAID;
                installment.PaidAt = DateTime.UtcNow;
                installment.GatewayTransactionId = paymentResult.GatewayTransactionId;
                installment.AttemptCount++;

                // Create payment event
                var paymentEvent = new PaymentEvent
                {
                    PaymentId = installment.PaymentId,
                    Type = PaymentEventType.INSTALLMENT_PAID,
                    Description = $"Installment {installment.Id} paid successfully",
                    Amount = installment.Amount,
                    Metadata = new Dictionary<string, object>
                    {
                        { "InstallmentId", installment.Id },
                        { "GatewayTransactionId", paymentResult.GatewayTransactionId ?? "" },
                        { "ProcessingDate", DateTime.UtcNow },
                        { "CorrelationId", correlationId }
                    },
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentEvents.Add(paymentEvent);
                await _context.SaveChangesAsync();

                // Send payment confirmation
                await _notificationService.SendPaymentConfirmationAsync(new PaymentConfirmationRequest
                {
                    CustomerId = installment.Payment.CustomerId,
                    PaymentId = installment.PaymentId,
                    InstallmentId = installment.Id,
                    Amount = installment.Amount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = installment.Payment.PaymentMethod.ToString()
                });

                // Publish success event to Service Bus
                await _serviceBus.PublishEventAsync("installment-paid", new InstallmentPaidEvent
                {
                    InstallmentId = installment.Id,
                    PaymentId = installment.PaymentId,
                    CustomerId = installment.Payment.CustomerId,
                    MerchantId = installment.Payment.MerchantId,
                    Amount = installment.Amount,
                    PaidAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                });

                installmentResult.Success = true;
                installmentResult.TransactionId = paymentResult.GatewayTransactionId;

                _logger.LogInformation("Installment {InstallmentId} processed successfully. Transaction: {TransactionId}. CorrelationId: {CorrelationId}",
                    installment.Id, paymentResult.GatewayTransactionId, correlationId);
            }
            else
            {
                // Handle payment failure
                installment.Status = InstallmentStatus.FAILED;
                installment.FailureReason = paymentResult.ErrorMessage;
                installment.AttemptCount++;
                installment.LastAttemptAt = DateTime.UtcNow;

                // Create failure event
                var failureEvent = new PaymentEvent
                {
                    PaymentId = installment.PaymentId,
                    Type = PaymentEventType.INSTALLMENT_FAILED,
                    Description = $"Installment {installment.Id} payment failed: {paymentResult.ErrorMessage}",
                    Amount = installment.Amount,
                    Metadata = new Dictionary<string, object>
                    {
                        { "InstallmentId", installment.Id },
                        { "ErrorCode", paymentResult.ErrorCode ?? "" },
                        { "ErrorMessage", paymentResult.ErrorMessage ?? "" },
                        { "AttemptCount", installment.AttemptCount },
                        { "IsRetryable", paymentResult.IsRetryable },
                        { "CorrelationId", correlationId }
                    },
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentEvents.Add(failureEvent);
                await _context.SaveChangesAsync();

                // Schedule retry if the error is retryable and under max attempts
                if (paymentResult.IsRetryable && installment.AttemptCount < _options.MaxRetryAttempts)
                {
                    await ScheduleInstallmentRetryAsync(installment, correlationId);
                }
                else
                {
                    // Send failure notification to customer
                    await _notificationService.SendPaymentFailureNotificationAsync(new PaymentFailureRequest
                    {
                        CustomerId = installment.Payment.CustomerId,
                        InstallmentId = installment.Id,
                        Amount = installment.Amount,
                        FailureReason = paymentResult.ErrorMessage,
                        NextAttemptDate = GetNextRetryDate(installment.AttemptCount)
                    });
                }

                installmentResult.Success = false;
                installmentResult.ErrorMessage = paymentResult.ErrorMessage;
                installmentResult.ErrorCode = paymentResult.ErrorCode;

                _logger.LogWarning("Installment {InstallmentId} payment failed. Error: {ErrorMessage}. Attempt: {AttemptCount}. CorrelationId: {CorrelationId}",
                    installment.Id, paymentResult.ErrorMessage, installment.AttemptCount, correlationId);
            }

            return installmentResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing installment {InstallmentId}. CorrelationId: {CorrelationId}", 
                installment.Id, correlationId);

            installment.Status = InstallmentStatus.FAILED;
            installment.FailureReason = $"System error: {ex.Message}";
            installment.AttemptCount++;
            installment.LastAttemptAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            installmentResult.Success = false;
            installmentResult.ErrorMessage = ex.Message;
            return installmentResult;
        }
    }

    public async Task<InstallmentRetryResult> RetryFailedInstallmentsAsync(string correlationId)
    {
        _logger.LogInformation("Starting retry of failed installments. CorrelationId: {CorrelationId}", correlationId);

        var result = new InstallmentRetryResult { CorrelationId = correlationId };

        try
        {
            // Get failed installments that are eligible for retry
            var retryableInstallments = await _context.Installments
                .Include(i => i.Payment)
                    .ThenInclude(p => p.Customer)
                .Where(i => i.Status == InstallmentStatus.FAILED &&
                           i.AttemptCount < _options.MaxRetryAttempts &&
                           i.LastAttemptAt.HasValue &&
                           i.LastAttemptAt.Value.AddHours(_options.RetryDelayHours) <= DateTime.UtcNow)
                .OrderBy(i => i.LastAttemptAt)
                .Take(_options.MaxRetryBatchSize)
                .ToListAsync();

            _logger.LogInformation("Found {RetryableCount} installments eligible for retry. CorrelationId: {CorrelationId}", 
                retryableInstallments.Count, correlationId);

            result.RetriedCount = retryableInstallments.Count;

            foreach (var installment in retryableInstallments)
            {
                var retryResult = await ProcessSingleInstallmentAsync(installment, correlationId);
                
                if (retryResult.Success)
                {
                    result.SucceededCount++;
                }
                else
                {
                    result.StillFailedCount++;
                }

                // Add delay between retries
                if (_options.RetryDelayMilliseconds > 0)
                {
                    await Task.Delay(_options.RetryDelayMilliseconds);
                }
            }

            _logger.LogInformation("Installment retry completed. Retried: {RetriedCount}, Succeeded: {SucceededCount}, Still Failed: {StillFailedCount}. CorrelationId: {CorrelationId}",
                result.RetriedCount, result.SucceededCount, result.StillFailedCount, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during installment retry. CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<OverdueProcessingResult> ProcessOverdueInstallmentsAsync(string correlationId)
    {
        _logger.LogInformation("Starting overdue installment processing. CorrelationId: {CorrelationId}", correlationId);

        var result = new OverdueProcessingResult { CorrelationId = correlationId };

        try
        {
            var overdueDate = DateTime.UtcNow.Date.AddDays(-_options.OverdueDays);

            // Get overdue installments
            var overdueInstallments = await _context.Installments
                .Include(i => i.Payment)
                    .ThenInclude(p => p.Customer)
                .Where(i => i.Status == InstallmentStatus.FAILED &&
                           i.DueDate.Date <= overdueDate &&
                           i.AttemptCount >= _options.MaxRetryAttempts)
                .ToListAsync();

            _logger.LogInformation("Found {OverdueCount} overdue installments. CorrelationId: {CorrelationId}", 
                overdueInstallments.Count, correlationId);

            foreach (var installment in overdueInstallments)
            {
                // Mark as overdue
                installment.Status = InstallmentStatus.OVERDUE;
                
                // Calculate overdue amount with late fees (Norwegian regulations)
                var lateFee = CalculateLateFee(installment.Amount, installment.DueDate);
                var totalOverdueAmount = installment.Amount + lateFee;

                result.ProcessedCount++;
                result.TotalOverdueAmount += totalOverdueAmount;

                // Send overdue notice
                await _notificationService.SendOverdueNoticeAsync(new OverdueNoticeRequest
                {
                    CustomerId = installment.Payment.CustomerId,
                    InstallmentId = installment.Id,
                    OriginalAmount = installment.Amount,
                    LateFee = lateFee,
                    TotalAmount = totalOverdueAmount,
                    DueDate = installment.DueDate,
                    DaysOverdue = (DateTime.UtcNow.Date - installment.DueDate.Date).Days
                });

                // Escalate to collections if severely overdue
                if ((DateTime.UtcNow.Date - installment.DueDate.Date).Days > _options.CollectionEscalationDays)
                {
                    await EscalateToCollectionsAsync(installment, correlationId);
                    result.EscalatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Overdue processing completed. Processed: {ProcessedCount}, Escalated: {EscalatedCount}, Total Overdue: {TotalOverdueAmount} NOK. CorrelationId: {CorrelationId}",
                result.ProcessedCount, result.EscalatedCount, result.TotalOverdueAmount, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during overdue processing. CorrelationId: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        try
        {
            var healthStatus = new HealthStatus { IsHealthy = true };

            // Check database connectivity
            var dbHealthy = await _context.Database.CanConnectAsync();
            healthStatus.Details.Add("Database", dbHealthy ? "Healthy" : "Unhealthy");

            if (!dbHealthy)
            {
                healthStatus.IsHealthy = false;
            }

            // Check recent processing statistics
            var recentFailures = await _context.Installments
                .Where(i => i.Status == InstallmentStatus.FAILED &&
                           i.LastAttemptAt.HasValue &&
                           i.LastAttemptAt.Value >= DateTime.UtcNow.AddHours(-24))
                .CountAsync();

            healthStatus.Details.Add("RecentFailures24h", recentFailures);

            if (recentFailures > _options.HealthCheckFailureThreshold)
            {
                healthStatus.IsHealthy = false;
            }

            return healthStatus;
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                IsHealthy = false,
                Details = new Dictionary<string, object> { { "Error", ex.Message } }
            };
        }
    }

    #region Private Helper Methods

    private async Task<List<PaymentHistoryItem>> GetCustomerPaymentHistoryAsync(Guid customerId)
    {
        return await _context.Installments
            .Where(i => i.Payment.CustomerId == customerId && i.Status == InstallmentStatus.PAID)
            .OrderByDescending(i => i.PaidAt)
            .Take(12) // Last 12 payments
            .Select(i => new PaymentHistoryItem
            {
                Amount = i.Amount,
                PaidAt = i.PaidAt!.Value,
                DueDate = i.DueDate,
                DaysLate = i.PaidAt!.Value.Date > i.DueDate.Date ? 
                          (i.PaidAt.Value.Date - i.DueDate.Date).Days : 0
            })
            .ToListAsync();
    }

    private async Task ScheduleInstallmentRetryAsync(Installment installment, string correlationId)
    {
        var retryDate = GetNextRetryDate(installment.AttemptCount);
        
        // Publish retry message to Service Bus with delay
        await _serviceBus.PublishEventAsync("installment-retry", new InstallmentRetryEvent
        {
            InstallmentId = installment.Id,
            ScheduledFor = retryDate,
            AttemptCount = installment.AttemptCount,
            CorrelationId = correlationId
        }, retryDate);

        _logger.LogInformation("Installment {InstallmentId} scheduled for retry at {RetryDate}. Attempt: {AttemptCount}. CorrelationId: {CorrelationId}",
            installment.Id, retryDate, installment.AttemptCount, correlationId);
    }

    private DateTime GetNextRetryDate(int attemptCount)
    {
        // Exponential backoff: 1 hour, 4 hours, 24 hours, 72 hours
        var delayHours = attemptCount switch
        {
            1 => 1,
            2 => 4,
            3 => 24,
            _ => 72
        };

        return DateTime.UtcNow.AddHours(delayHours);
    }

    private decimal CalculateLateFee(decimal amount, DateTime dueDate)
    {
        var daysOverdue = (DateTime.UtcNow.Date - dueDate.Date).Days;
        
        // Norwegian late fee structure: 47 NOK + 5% of amount (max 100 NOK total)
        var baseFee = 47m;
        var percentageFee = amount * 0.05m;
        var totalFee = baseFee + percentageFee;
        
        return Math.Min(totalFee, 100m); // Cap at 100 NOK per Norwegian regulations
    }

    private async Task EscalateToCollectionsAsync(Installment installment, string correlationId)
    {
        // Create collections case
        var collectionsCase = new CollectionsCase
        {
            InstallmentId = installment.Id,
            CustomerId = installment.Payment.CustomerId,
            OriginalAmount = installment.Amount,
            LateFees = CalculateLateFee(installment.Amount, installment.DueDate),
            Status = CollectionStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            AssignedAgent = null // Will be assigned by collections team
        };

        _context.CollectionsCases.Add(collectionsCase);

        // Publish escalation event
        await _serviceBus.PublishEventAsync("collections-escalation", new CollectionsEscalationEvent
        {
            CaseId = collectionsCase.Id,
            InstallmentId = installment.Id,
            CustomerId = installment.Payment.CustomerId,
            Amount = installment.Amount + collectionsCase.LateFees,
            DaysOverdue = (DateTime.UtcNow.Date - installment.DueDate.Date).Days,
            CorrelationId = correlationId
        });

        _logger.LogWarning("Installment {InstallmentId} escalated to collections. Case: {CaseId}. CorrelationId: {CorrelationId}",
            installment.Id, collectionsCase.Id, correlationId);
    }

    private async Task UpdateProcessingStatisticsAsync(InstallmentProcessingResult result, string correlationId)
    {
        var stats = new ProcessingStatistics
        {
            ProcessingDate = result.ProcessingDate,
            ProcessedCount = result.ProcessedCount,
            FailedCount = result.FailedCount,
            TotalAmount = result.TotalAmount,
            ProcessingDurationMs = (int)(DateTime.UtcNow - result.ProcessingDate).TotalMilliseconds,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProcessingStatistics.Add(stats);
        await _context.SaveChangesAsync();
    }

    #endregion
}

#region Result Models

public class InstallmentProcessingResult
{
    public DateTime ProcessingDate { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InstallmentResult> FailedInstallments { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
}

public class InstallmentRetryResult
{
    public int RetriedCount { get; set; }
    public int SucceededCount { get; set; }
    public int StillFailedCount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class OverdueProcessingResult
{
    public int ProcessedCount { get; set; }
    public int EscalatedCount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class InstallmentResult
{
    public Guid InstallmentId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

#endregion