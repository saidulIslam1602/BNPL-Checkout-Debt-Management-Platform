using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RivertyBNPL.NotificationScheduler.Functions.Services;

namespace RivertyBNPL.NotificationScheduler.Functions.Functions;

/// <summary>
/// Azure Function for sending payment reminders
/// </summary>
public class PaymentReminderFunction
{
    private readonly ILogger<PaymentReminderFunction> _logger;
    private readonly IPaymentReminderService _paymentReminderService;

    public PaymentReminderFunction(
        ILogger<PaymentReminderFunction> logger,
        IPaymentReminderService paymentReminderService)
    {
        _logger = logger;
        _paymentReminderService = paymentReminderService;
    }

    /// <summary>
    /// Runs every day at 9:00 AM to send payment reminders
    /// </summary>
    [Function("PaymentReminderDaily")]
    public async Task RunDailyReminders([TimerTrigger("0 0 9 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Payment reminder function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Send reminders for payments due in 3 days
            var threeDayReminders = await _paymentReminderService.SendPaymentRemindersAsync(3);
            _logger.LogInformation("Sent {Count} payment reminders for payments due in 3 days", threeDayReminders);

            // Send reminders for payments due in 1 day
            var oneDayReminders = await _paymentReminderService.SendPaymentRemindersAsync(1);
            _logger.LogInformation("Sent {Count} payment reminders for payments due in 1 day", oneDayReminders);

            // Send reminders for payments due today
            var todayReminders = await _paymentReminderService.SendPaymentRemindersAsync(0);
            _logger.LogInformation("Sent {Count} payment reminders for payments due today", todayReminders);

            _logger.LogInformation("Payment reminder function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending payment reminders");
            throw;
        }
    }

    /// <summary>
    /// Runs every hour to send overdue payment notifications
    /// </summary>
    [Function("OverduePaymentNotifications")]
    public async Task RunOverdueNotifications([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Overdue payment notification function started at: {Time}", DateTime.UtcNow);

        try
        {
            var overdueNotifications = await _paymentReminderService.SendOverdueNotificationsAsync();
            _logger.LogInformation("Sent {Count} overdue payment notifications", overdueNotifications);

            _logger.LogInformation("Overdue payment notification function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending overdue payment notifications");
            throw;
        }
    }

    /// <summary>
    /// Runs every 4 hours to process BNPL installment reminders
    /// </summary>
    [Function("BNPLInstallmentReminders")]
    public async Task RunBNPLReminders([TimerTrigger("0 0 */4 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("BNPL installment reminder function started at: {Time}", DateTime.UtcNow);

        try
        {
            // Send reminders for BNPL installments due in 7 days
            var weekReminders = await _paymentReminderService.SendBNPLInstallmentRemindersAsync(7);
            _logger.LogInformation("Sent {Count} BNPL installment reminders for payments due in 7 days", weekReminders);

            // Send reminders for BNPL installments due in 3 days
            var threeDayReminders = await _paymentReminderService.SendBNPLInstallmentRemindersAsync(3);
            _logger.LogInformation("Sent {Count} BNPL installment reminders for payments due in 3 days", threeDayReminders);

            // Send reminders for BNPL installments due tomorrow
            var tomorrowReminders = await _paymentReminderService.SendBNPLInstallmentRemindersAsync(1);
            _logger.LogInformation("Sent {Count} BNPL installment reminders for payments due tomorrow", tomorrowReminders);

            _logger.LogInformation("BNPL installment reminder function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending BNPL installment reminders");
            throw;
        }
    }

    /// <summary>
    /// Runs every 6 hours to send customer engagement notifications
    /// </summary>
    [Function("CustomerEngagementNotifications")]
    public async Task RunCustomerEngagement([TimerTrigger("0 0 */6 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Customer engagement notification function started at: {Time}", DateTime.UtcNow);

        try
        {
            var engagementNotifications = await _paymentReminderService.SendCustomerEngagementNotificationsAsync();
            _logger.LogInformation("Sent {Count} customer engagement notifications", engagementNotifications);

            _logger.LogInformation("Customer engagement notification function completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending customer engagement notifications");
            throw;
        }
    }

    /// <summary>
    /// Manual trigger for payment reminders (for testing or emergency use)
    /// </summary>
    [Function("ManualPaymentReminder")]
    public async Task<string> RunManualReminder(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Manual payment reminder function triggered");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var daysAhead = int.TryParse(query["daysAhead"], out var days) ? days : 1;
            var notificationType = query["type"] ?? "payment";

            int count = notificationType.ToLower() switch
            {
                "bnpl" => await _paymentReminderService.SendBNPLInstallmentRemindersAsync(daysAhead),
                "overdue" => await _paymentReminderService.SendOverdueNotificationsAsync(),
                "engagement" => await _paymentReminderService.SendCustomerEngagementNotificationsAsync(),
                _ => await _paymentReminderService.SendPaymentRemindersAsync(daysAhead)
            };

            var result = $"Sent {count} {notificationType} notifications for {daysAhead} days ahead";
            _logger.LogInformation(result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in manual payment reminder function");
            throw;
        }
    }
}