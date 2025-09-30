using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Notification.API.Models;
using NotificationEntity = YourCompanyBNPL.Notification.API.Models.Notification;

namespace YourCompanyBNPL.Notification.API.Services;

/// <summary>
/// Service for queuing notifications
/// </summary>
public interface INotificationQueueService
{
    Task EnqueueAsync(NotificationEntity notification);
    Task<List<NotificationEntity>> DequeueAsync(int count = 10);
}

public class NotificationQueueService : INotificationQueueService
{
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(ILogger<NotificationQueueService> logger)
    {
        _logger = logger;
    }

    public async Task EnqueueAsync(NotificationEntity notification)
    {
        // TODO: Implement with Azure Service Bus or Redis Queue
        await Task.CompletedTask;
        _logger.LogInformation("Notification {Id} queued", notification.Id);
    }

    public async Task<List<NotificationEntity>> DequeueAsync(int count = 10)
    {
        // TODO: Implement queue retrieval
        await Task.CompletedTask;
        return new List<NotificationEntity>();
    }
}