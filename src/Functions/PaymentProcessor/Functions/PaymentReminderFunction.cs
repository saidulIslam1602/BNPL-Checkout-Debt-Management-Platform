using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RivertyBNPL.Functions.PaymentProcessor.Services;
using RivertyBNPL.Functions.PaymentProcessor.Configuration;

namespace RivertyBNPL.Functions.PaymentProcessor.Functions;

/// <summary>
/// Azure Function for sending payment reminders to Norwegian BNPL customers
/// Implements Norwegian consumer protection regulations for payment notifications
/// </summary>
public class PaymentReminderFunction
{
    private readonly ILogger<PaymentReminderFunction> _logger;
    private readonly IPaymentReminderService _reminderService;
    private readonly NotificationOptions _options;

    public PaymentReminderFunction(
        ILogger<PaymentReminderFunction> logger,
        IPaymentReminderService reminderService,
        IOptions<NotificationOptions> options)
    {
        _logger = logger;
        _reminderService = reminderService;
        _options = options.Value;
    }

    /// <summary>
    /// Send upcoming payment reminders - runs daily at 10:00 Norwegian time
    /// Sends reminders 3 days before payment due date (Norwegian standard)
    /// </summary>
    [Function("SendUpcomingPaymentReminders")]
    public async Task SendUpcomingPaymentReminders(
        [TimerTrigger("0 0 10 * * *", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting upcoming payment reminders. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            // Norwegian regulation: Remind customers 3 days before due date
            var reminderDate = DateTime.UtcNow.AddDays(3).Date;
            var result = await _reminderService.SendUpcomingPaymentRemindersAsync(reminderDate, correlationId);

            _logger.LogInformation(
                "Upcoming payment reminders sent. SMS: {SmsCount}, Email: {EmailCount}, Push: {PushCount}, Failed: {FailedCount}. CorrelationId: {CorrelationId}",
                result.SmsCount,
                result.EmailCount,
                result.PushCount,
                result.FailedCount,
                correlationId);

            // Track reminder effectiveness
            await _reminderService.TrackReminderMetricsAsync(result, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending upcoming payment reminders. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Send overdue payment notices - runs daily at 11:00 Norwegian time
    /// Implements Norwegian debt collection regulations (Inkassoloven)
    /// </summary>
    [Function("SendOverduePaymentNotices")]
    public async Task SendOverduePaymentNotices(
        [TimerTrigger("0 0 11 * * *", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting overdue payment notices. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var result = await _reminderService.SendOverduePaymentNoticesAsync(correlationId);

            _logger.LogInformation(
                "Overdue payment notices sent. First Notice: {FirstNoticeCount}, Second Notice: {SecondNoticeCount}, Final Notice: {FinalNoticeCount}, Failed: {FailedCount}. CorrelationId: {CorrelationId}",
                result.FirstNoticeCount,
                result.SecondNoticeCount,
                result.FinalNoticeCount,
                result.FailedCount,
                correlationId);

            // Alert if high number of overdue notices
            var totalNotices = result.FirstNoticeCount + result.SecondNoticeCount + result.FinalNoticeCount;
            if (totalNotices > _options.OverdueNoticeAlertThreshold)
            {
                _logger.LogWarning(
                    "High number of overdue notices sent: {TotalNotices}. Threshold: {Threshold}. CorrelationId: {CorrelationId}",
                    totalNotices,
                    _options.OverdueNoticeAlertThreshold,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending overdue payment notices. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Send payment confirmations - triggered by Service Bus message
    /// Sends immediate confirmation when payment is received
    /// </summary>
    [Function("SendPaymentConfirmations")]
    public async Task SendPaymentConfirmations(
        [ServiceBusTrigger("payment-confirmations", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Processing payment confirmation message. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var paymentConfirmation = System.Text.Json.JsonSerializer.Deserialize<PaymentConfirmationMessage>(message);
            if (paymentConfirmation == null)
            {
                _logger.LogWarning("Invalid payment confirmation message format. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            await _reminderService.SendPaymentConfirmationAsync(paymentConfirmation, correlationId);

            _logger.LogInformation(
                "Payment confirmation sent to customer {CustomerId} for payment {PaymentId}. CorrelationId: {CorrelationId}",
                paymentConfirmation.CustomerId,
                paymentConfirmation.PaymentId,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending payment confirmation. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Service Bus retry
        }
    }

    /// <summary>
    /// Send welcome messages for new BNPL customers - triggered by Service Bus
    /// Includes Norwegian consumer rights information
    /// </summary>
    [Function("SendWelcomeMessages")]
    public async Task SendWelcomeMessages(
        [ServiceBusTrigger("customer-welcome", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Processing welcome message. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var welcomeMessage = System.Text.Json.JsonSerializer.Deserialize<WelcomeMessage>(message);
            if (welcomeMessage == null)
            {
                _logger.LogWarning("Invalid welcome message format. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            await _reminderService.SendWelcomeMessageAsync(welcomeMessage, correlationId);

            _logger.LogInformation(
                "Welcome message sent to new customer {CustomerId}. CorrelationId: {CorrelationId}",
                welcomeMessage.CustomerId,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending welcome message. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Service Bus retry
        }
    }

    /// <summary>
    /// Send monthly statements - runs on the 1st of each month at 08:00
    /// Norwegian regulation requires monthly statements for credit products
    /// </summary>
    [Function("SendMonthlyStatements")]
    public async Task SendMonthlyStatements(
        [TimerTrigger("0 0 8 1 * *", TimeZone = "W. Europe Standard Time")] TimerInfo timer,
        FunctionContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting monthly statement generation. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            var result = await _reminderService.SendMonthlyStatementsAsync(previousMonth, correlationId);

            _logger.LogInformation(
                "Monthly statements sent. Total: {TotalCount}, Email: {EmailCount}, Postal: {PostalCount}, Failed: {FailedCount}. CorrelationId: {CorrelationId}",
                result.TotalCount,
                result.EmailCount,
                result.PostalCount,
                result.FailedCount,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending monthly statements. CorrelationId: {CorrelationId}", 
                correlationId);
        }
    }

    /// <summary>
    /// Send payment plan updates - triggered by Service Bus message
    /// Notifies customers when payment plans are modified
    /// </summary>
    [Function("SendPaymentPlanUpdates")]
    public async Task SendPaymentPlanUpdates(
        [ServiceBusTrigger("payment-plan-updates", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        var correlationId = context.BindingContext.BindingData["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Processing payment plan update message. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var planUpdate = System.Text.Json.JsonSerializer.Deserialize<PaymentPlanUpdateMessage>(message);
            if (planUpdate == null)
            {
                _logger.LogWarning("Invalid payment plan update message format. CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            await _reminderService.SendPaymentPlanUpdateAsync(planUpdate, correlationId);

            _logger.LogInformation(
                "Payment plan update sent to customer {CustomerId} for plan {PlanId}. CorrelationId: {CorrelationId}",
                planUpdate.CustomerId,
                planUpdate.PlanId,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending payment plan update. CorrelationId: {CorrelationId}", 
                correlationId);
            throw; // Re-throw to trigger Service Bus retry
        }
    }

    /// <summary>
    /// Manual reminder sending for specific customers
    /// Allows customer service to send ad-hoc reminders
    /// </summary>
    [Function("SendManualReminder")]
    public async Task<string> SendManualReminder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "reminders/send")] HttpRequestData req,
        FunctionContext context)
    {
        var correlationId = req.Headers.GetValues("X-Correlation-ID").FirstOrDefault() ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Manual reminder request received. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var reminderRequest = System.Text.Json.JsonSerializer.Deserialize<ManualReminderRequest>(requestBody);

            if (reminderRequest == null)
            {
                var errorResponse = new { Success = false, ErrorMessage = "Invalid request format" };
                return System.Text.Json.JsonSerializer.Serialize(errorResponse);
            }

            var result = await _reminderService.SendManualReminderAsync(reminderRequest, correlationId);

            var response = new
            {
                Success = result.Success,
                MessageId = result.MessageId,
                SentAt = result.SentAt,
                CorrelationId = correlationId
            };

            _logger.LogInformation(
                "Manual reminder sent to customer {CustomerId}. Success: {Success}. CorrelationId: {CorrelationId}",
                reminderRequest.CustomerId,
                result.Success,
                correlationId);

            return System.Text.Json.JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error sending manual reminder. CorrelationId: {CorrelationId}", 
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
}

/// <summary>
/// Message models for Service Bus communication
/// </summary>
public class PaymentConfirmationMessage
{
    public string PaymentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class WelcomeMessage
{
    public string CustomerId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "no";
}

public class PaymentPlanUpdateMessage
{
    public string PlanId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string UpdateType { get; set; } = string.Empty; // MODIFIED, PAUSED, RESUMED, CANCELLED
    public Dictionary<string, object> Changes { get; set; } = new();
}

public class ManualReminderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string ReminderType { get; set; } = string.Empty; // PAYMENT_DUE, OVERDUE, CUSTOM
    public string Message { get; set; } = string.Empty;
    public List<string> Channels { get; set; } = new(); // SMS, EMAIL, PUSH
    public DateTime? ScheduledFor { get; set; }
}