using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Notification.API.Infrastructure;

namespace YourCompanyBNPL.Notification.API.Services;

/// <summary>
/// Background service to process scheduled notifications
/// </summary>
public class ScheduledNotificationProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledNotificationProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1); // Check every minute

    public ScheduledNotificationProcessor(
        IServiceProvider serviceProvider,
        ILogger<ScheduledNotificationProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled notification processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<INotificationScheduler>();
                
                await scheduler.ProcessScheduledNotificationsAsync(stoppingToken);
                
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled notifications");
                
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Scheduled notification processor stopped");
    }
}