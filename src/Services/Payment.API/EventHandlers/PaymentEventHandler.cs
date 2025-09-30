using YourCompanyBNPL.Common.Enums;
using Microsoft.Extensions.Logging;
using YourCompanyBNPL.Shared.Infrastructure.ServiceBus;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.Data;
using Microsoft.EntityFrameworkCore;

namespace YourCompanyBNPL.Payment.API.EventHandlers;

/// <summary>
/// Event handler for payment-related events in the Norwegian BNPL system
/// Processes real-time events from other microservices
/// </summary>

public class PaymentEventHandler
{
    private readonly ILogger<PaymentEventHandler> _logger;
    private readonly PaymentDbContext _context;
    private readonly IServiceBusService _serviceBus;
    private readonly IPaymentService _paymentService;
    private readonly ISettlementService _settlementService;

    public PaymentEventHandler(
        ILogger<PaymentEventHandler> logger,
        PaymentDbContext context,
        IServiceBusService serviceBus,
        IPaymentService paymentService,
        ISettlementService settlementService)
    {
        _logger = logger;
        _context = context;
        _serviceBus = serviceBus;
        _paymentService = paymentService;
        _settlementService = settlementService;
    }

    /// <summary>
    /// Handles risk assessment completion events
    /// Updates payment status based on risk assessment results
    /// </summary>
    public async Task HandleRiskAssessmentCompletedAsync(RiskAssessmentCompletedEvent riskEvent, CancellationToken cancellationToken = default)
    {
        var correlationId = riskEvent.CorrelationId;
        _logger.LogInformation("Processing risk assessment completed event. AssessmentId: {AssessmentId}, CustomerId: {CustomerId}, Approved: {IsApproved}. CorrelationId: {CorrelationId}",
            riskEvent.AssessmentId, riskEvent.CustomerId, riskEvent.IsApproved, correlationId);

        try
        {
            // Find pending payments for this customer and amount
            var pendingPayments = await _context.Payments
                .Where(p => p.CustomerId == Guid.Parse(riskEvent.CustomerId) &&
                           p.Amount == riskEvent.RequestedAmount &&
                           p.Status == Common.Enums.PaymentStatus.Pending)
                .ToListAsync();

            foreach (var payment in pendingPayments)
            {
                if (riskEvent.IsApproved)
                {
                    // Risk assessment passed - proceed with payment processing
                    _logger.LogInformation("Risk assessment approved for payment {PaymentId}. Proceeding with processing. CorrelationId: {CorrelationId}",
                        payment.Id, correlationId);

                    // Update payment with risk assessment data
                    payment.RiskScore = riskEvent.CreditScore;
                    payment.RiskLevel = Enum.Parse<Common.Enums.RiskLevel>(riskEvent.RiskLevel);
                    
                    // Create payment event
                    var paymentEvent = new Models.PaymentEvent
                    {
                        PaymentId = payment.Id,
                        Type = Common.Enums.PaymentEventType.RISK_ASSESSMENT_COMPLETED,
                        Description = $"Risk assessment completed. Score: {riskEvent.CreditScore}, Level: {riskEvent.RiskLevel}",
                        Metadata = new Dictionary<string, object>
                        {
                            { "AssessmentId", riskEvent.AssessmentId },
                            { "CreditScore", riskEvent.CreditScore },
                            { "RiskLevel", riskEvent.RiskLevel },
                            { "RiskFactors", riskEvent.RiskFactors },
                            { "CorrelationId", correlationId }
                        },
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PaymentEvents.Add(paymentEvent);

                    // Process the payment
                    await _paymentService.ProcessApprovedPaymentAsync(payment.Id, cancellationToken);
                }
                else
                {
                    // Risk assessment failed - decline payment
                    _logger.LogWarning("Risk assessment declined for payment {PaymentId}. Risk factors: {RiskFactors}. CorrelationId: {CorrelationId}",
                        payment.Id, string.Join(", ", riskEvent.RiskFactors), correlationId);

                    payment.Status = Common.Enums.PaymentStatus.Failed;
                    payment.FailureReason = $"Risk assessment declined. Risk factors: {string.Join(", ", riskEvent.RiskFactors)}";
                    payment.RiskScore = riskEvent.CreditScore;
                    payment.RiskLevel = Enum.Parse<Common.Enums.RiskLevel>(riskEvent.RiskLevel);

                    // Create decline event
                    var declineEvent = new Models.PaymentEvent
                    {
                        PaymentId = payment.Id,
                        Type = Common.Enums.PaymentEventType.PAYMENT_FAILED,
                        Description = $"Payment declined due to risk assessment. Score: {riskEvent.CreditScore}",
                        Metadata = new Dictionary<string, object>
                        {
                            { "AssessmentId", riskEvent.AssessmentId },
                            { "CreditScore", riskEvent.CreditScore },
                            { "RiskLevel", riskEvent.RiskLevel },
                            { "RiskFactors", riskEvent.RiskFactors },
                            { "DeclineReason", "Risk assessment failed" },
                            { "CorrelationId", correlationId }
                        },
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PaymentEvents.Add(declineEvent);

                    // Notify customer of decline
                    await _serviceBus.PublishEventAsync("payment-declined", new PaymentDeclinedEvent
                    {
                        PaymentId = payment.Id.ToString(),
                        CustomerId = payment.CustomerId.ToString(),
                        Amount = payment.Amount,
                        DeclineReason = "Risk assessment failed",
                        RiskScore = riskEvent.CreditScore,
                        CorrelationId = correlationId
                    });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Risk assessment event processed successfully. Processed {PaymentCount} payments. CorrelationId: {CorrelationId}",
                pendingPayments.Count, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing risk assessment completed event. AssessmentId: {AssessmentId}. CorrelationId: {CorrelationId}",
                riskEvent.AssessmentId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Handles fraud detection events
    /// Takes immediate action when fraud is detected
    /// </summary>
    public async Task HandleFraudDetectedAsync(FraudDetectedEvent fraudEvent)
    {
        var correlationId = fraudEvent.CorrelationId;
        _logger.LogWarning("Processing fraud detection event. TransactionId: {TransactionId}, FraudScore: {FraudScore}, Action: {Action}. CorrelationId: {CorrelationId}",
            fraudEvent.TransactionId, fraudEvent.FraudScore, fraudEvent.Action, correlationId);

        try
        {
            // Find the payment/transaction
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == Guid.Parse(fraudEvent.TransactionId));

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for fraud detection event. TransactionId: {TransactionId}. CorrelationId: {CorrelationId}",
                    fraudEvent.TransactionId, correlationId);
                return;
            }

            // Take action based on fraud detection result
            switch (fraudEvent.Action.ToUpper())
            {
                case "BLOCK":
                    await BlockPaymentForFraudAsync(payment, fraudEvent, correlationId);
                    break;
                
                case "REVIEW":
                    await FlagPaymentForReviewAsync(payment, fraudEvent, correlationId);
                    break;
                
                case "ALLOW":
                    await LogFraudDetectionAsync(payment, fraudEvent, correlationId);
                    break;
                
                default:
                    _logger.LogWarning("Unknown fraud action: {Action}. TransactionId: {TransactionId}. CorrelationId: {CorrelationId}",
                        fraudEvent.Action, fraudEvent.TransactionId, correlationId);
                    break;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Fraud detection event processed successfully. TransactionId: {TransactionId}, Action: {Action}. CorrelationId: {CorrelationId}",
                fraudEvent.TransactionId, fraudEvent.Action, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fraud detection event. TransactionId: {TransactionId}. CorrelationId: {CorrelationId}",
                fraudEvent.TransactionId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Handles installment paid events
    /// Updates payment status and triggers settlement processing
    /// </summary>
    public async Task HandleInstallmentPaidAsync(InstallmentPaidEvent installmentEvent)
    {
        var correlationId = installmentEvent.CorrelationId;
        _logger.LogInformation("Processing installment paid event. InstallmentId: {InstallmentId}, PaymentId: {PaymentId}, Amount: {Amount} NOK. CorrelationId: {CorrelationId}",
            installmentEvent.InstallmentId, installmentEvent.PaymentId, installmentEvent.Amount, correlationId);

        try
        {
            var payment = await _context.Payments
                .Include(p => p.Installments)
                .FirstOrDefaultAsync(p => p.Id == Guid.Parse(installmentEvent.PaymentId));

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for installment paid event. PaymentId: {PaymentId}. CorrelationId: {CorrelationId}",
                    installmentEvent.PaymentId, correlationId);
                return;
            }

            // Create payment event
            var paymentEvent = new Models.PaymentEvent
            {
                PaymentId = payment.Id,
                Type = Common.Enums.PaymentEventType.INSTALLMENT_PAID,
                Description = $"Installment {installmentEvent.InstallmentId} paid successfully",
                Amount = installmentEvent.Amount,
                Metadata = new Dictionary<string, object>
                {
                    { "InstallmentId", installmentEvent.InstallmentId },
                    { "PaidAt", installmentEvent.PaidAt },
                    { "PaymentMethod", installmentEvent.PaymentMethod },
                    { "GatewayTransactionId", installmentEvent.GatewayTransactionId },
                    { "CorrelationId", correlationId }
                },
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentEvents.Add(paymentEvent);

            // Check if all installments are paid
            var allInstallmentsPaid = payment.Installments.All(i => i.Status == Common.Enums.PaymentStatus.Completed);
            
            if (allInstallmentsPaid)
            {
                payment.Status = Common.Enums.PaymentStatus.Completed;

                _logger.LogInformation("All installments paid for payment {PaymentId}. Marking as completed. CorrelationId: {CorrelationId}",
                    payment.Id, correlationId);

                // Trigger settlement processing
                await _serviceBus.PublishEventAsync("settlement-trigger", new SettlementTriggerEvent
                {
                    PaymentId = payment.Id.ToString(),
                    MerchantId = payment.MerchantId.ToString(),
                    Amount = installmentEvent.Amount,
                    Currency = "NOK",
                    CompletedAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                });
            }

            // Send payment confirmation to customer
            await _serviceBus.PublishEventAsync("payment-confirmations", new PaymentConfirmationMessage
            {
                PaymentId = payment.Id.ToString(),
                CustomerId = payment.CustomerId.ToString(),
                Amount = installmentEvent.Amount,
                Currency = "NOK",
                PaymentDate = installmentEvent.PaidAt,
                PaymentMethod = installmentEvent.PaymentMethod
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Installment paid event processed successfully. PaymentId: {PaymentId}, AllInstallmentsPaid: {AllInstallmentsPaid}. CorrelationId: {CorrelationId}",
                payment.Id, allInstallmentsPaid, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing installment paid event. InstallmentId: {InstallmentId}. CorrelationId: {CorrelationId}",
                installmentEvent.InstallmentId, correlationId);
            throw;
        }
    }

    /// <summary>
    /// Handles settlement processed events
    /// Updates merchant settlement status and sends notifications
    /// </summary>
    public async Task HandleSettlementProcessedAsync(SettlementProcessedEvent settlementEvent)
    {
        var correlationId = settlementEvent.CorrelationId;
        _logger.LogInformation("Processing settlement processed event. SettlementId: {SettlementId}, MerchantId: {MerchantId}, Amount: {Amount} NOK. CorrelationId: {CorrelationId}",
            settlementEvent.SettlementId, settlementEvent.MerchantId, settlementEvent.Amount, correlationId);

        try
        {
            // Update settlement records
            await _settlementService.UpdateSettlementStatusAsync(
                Guid.Parse(settlementEvent.SettlementId),
                Enum.Parse<Common.Enums.SettlementStatus>(settlementEvent.Status),
                settlementEvent.BankTransactionId,
                correlationId);

            // Send settlement confirmation to merchant
            await _serviceBus.PublishEventAsync("merchant-notifications", new MerchantSettlementNotification
            {
                SettlementId = settlementEvent.SettlementId,
                MerchantId = settlementEvent.MerchantId,
                Amount = settlementEvent.Amount,
                Currency = settlementEvent.Currency,
                ProcessedAt = settlementEvent.ProcessedAt,
                BankTransactionId = settlementEvent.BankTransactionId,
                Status = settlementEvent.Status,
                CorrelationId = correlationId
            });

            _logger.LogInformation("Settlement processed event handled successfully. SettlementId: {SettlementId}. CorrelationId: {CorrelationId}",
                settlementEvent.SettlementId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing settlement processed event. SettlementId: {SettlementId}. CorrelationId: {CorrelationId}",
                settlementEvent.SettlementId, correlationId);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task BlockPaymentForFraudAsync(Models.Payment payment, FraudDetectedEvent fraudEvent, string correlationId)
    {
        payment.Status = Common.Enums.PaymentStatus.Cancelled;
        payment.FailureReason = $"Blocked due to fraud detection. Score: {fraudEvent.FraudScore}";

        // Create fraud event
        var fraudEventRecord = new Models.PaymentEvent
        {
            PaymentId = payment.Id,
            Type = Common.Enums.PaymentEventType.FRAUD_DETECTED,
            Description = $"Payment blocked due to fraud detection. Score: {fraudEvent.FraudScore}",
            Metadata = new Dictionary<string, object>
            {
                { "FraudDetectionId", fraudEvent.FraudDetectionId },
                { "FraudScore", fraudEvent.FraudScore },
                { "FraudIndicators", fraudEvent.FraudIndicators },
                { "Action", "BLOCKED" },
                { "CorrelationId", correlationId }
            },
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentEvents.Add(fraudEventRecord);

        // Notify security team
        await _serviceBus.PublishEventAsync("security-alerts", new SecurityAlertEvent
        {
            AlertType = "PAYMENT_FRAUD_BLOCKED",
            PaymentId = payment.Id.ToString(),
            CustomerId = payment.CustomerId.ToString(),
            FraudScore = fraudEvent.FraudScore,
            FraudIndicators = fraudEvent.FraudIndicators,
            Severity = "HIGH",
            CorrelationId = correlationId
        });

        _logger.LogWarning("Payment {PaymentId} blocked due to fraud detection. Score: {FraudScore}. CorrelationId: {CorrelationId}",
            payment.Id, fraudEvent.FraudScore, correlationId);
    }

    private async Task FlagPaymentForReviewAsync(Models.Payment payment, FraudDetectedEvent fraudEvent, string correlationId)
    {
        // Add fraud flag but don't block payment
        var fraudEventRecord = new Models.PaymentEvent
        {
            PaymentId = payment.Id,
            Type = Common.Enums.PaymentEventType.FRAUD_DETECTED,
            Description = $"Payment flagged for manual review. Score: {fraudEvent.FraudScore}",
            Metadata = new Dictionary<string, object>
            {
                { "FraudDetectionId", fraudEvent.FraudDetectionId },
                { "FraudScore", fraudEvent.FraudScore },
                { "FraudIndicators", fraudEvent.FraudIndicators },
                { "Action", "REVIEW" },
                { "ReviewRequired", true },
                { "CorrelationId", correlationId }
            },
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentEvents.Add(fraudEventRecord);

        // Notify fraud review team
        await _serviceBus.PublishEventAsync("fraud-review-queue", new FraudReviewRequest
        {
            PaymentId = payment.Id.ToString(),
            CustomerId = payment.CustomerId.ToString(),
            FraudScore = fraudEvent.FraudScore,
            FraudIndicators = fraudEvent.FraudIndicators,
            Priority = fraudEvent.FraudScore > 80 ? "HIGH" : "MEDIUM",
            CorrelationId = correlationId
        });

        _logger.LogInformation("Payment {PaymentId} flagged for fraud review. Score: {FraudScore}. CorrelationId: {CorrelationId}",
            payment.Id, fraudEvent.FraudScore, correlationId);
    }

    private async Task LogFraudDetectionAsync(Models.Payment payment, FraudDetectedEvent fraudEvent, string correlationId)
    {
        // Just log the fraud detection for audit purposes
        var fraudEventRecord = new Models.PaymentEvent
        {
            PaymentId = payment.Id,
            Type = Common.Enums.PaymentEventType.FRAUD_DETECTED,
            Description = $"Fraud detection completed. Score: {fraudEvent.FraudScore} (Allowed)",
            Metadata = new Dictionary<string, object>
            {
                { "FraudDetectionId", fraudEvent.FraudDetectionId },
                { "FraudScore", fraudEvent.FraudScore },
                { "FraudIndicators", fraudEvent.FraudIndicators },
                { "Action", "ALLOW" },
                { "CorrelationId", correlationId }
            },
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentEvents.Add(fraudEventRecord);

        _logger.LogInformation("Fraud detection logged for payment {PaymentId}. Score: {FraudScore} (Allowed). CorrelationId: {CorrelationId}",
            payment.Id, fraudEvent.FraudScore, correlationId);
    }

    #endregion
}

#region Event Models

public class PaymentDeclinedEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string DeclineReason { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class SettlementTriggerEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public DateTime CompletedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class PaymentConfirmationMessage
{
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class MerchantSettlementNotification
{
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public DateTime ProcessedAt { get; set; }
    public string BankTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class SecurityAlertEvent
{
    public string AlertType { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal FraudScore { get; set; }
    public List<string> FraudIndicators { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class FraudReviewRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal FraudScore { get; set; }
    public List<string> FraudIndicators { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

#endregion