using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Hangfire;
using RivertyBNPL.Services.Notification.API.Data;
using RivertyBNPL.Services.Notification.API.Models;
using RivertyBNPL.Services.Notification.API.Providers;

namespace RivertyBNPL.Services.Notification.API.Services;

/// <summary>
/// Implementation of notification queue service
/// </summary>
public class NotificationQueueService : INotificationQueueService
{
    private readonly NotificationDbContext _context;
    private readonly INotificationProviderFactory _providerFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(
        NotificationDbContext context,
        INotificationProviderFactory providerFactory,
        IBackgroundJobClient backgroundJobClient,
        ILogger<NotificationQueueService> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<bool> QueueNotificationAsync(Models.Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueItem = new NotificationQueue
            {
                Id = Guid.NewGuid(),
                QueueName = GetQueueName(notification.Channel, notification.Priority),
                Channel = notification.Channel,
                Priority = notification.Priority,
                NotificationData = JsonSerializer.Serialize(new
                {
                    NotificationId = notification.Id,
                    RecipientId = notification.RecipientId,
                    RecipientEmail = notification.RecipientEmail,
                    RecipientPhone = notification.RecipientPhone,
                    RecipientDeviceToken = notification.RecipientDeviceToken,
                    Type = notification.Type,
                    Channel = notification.Channel,
                    Subject = notification.Subject,
                    Content = notification.Content,
                    Priority = notification.Priority,
                    CorrelationId = notification.CorrelationId,
                    Metadata = notification.Metadata
                }),
                ScheduledFor = notification.ScheduledAt,
                Status = NotificationQueueStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationQueue.Add(queueItem);
            await _context.SaveChangesAsync(cancellationToken);

            // Schedule background job
            if (notification.ScheduledAt.HasValue)
            {
                _backgroundJobClient.Schedule<NotificationQueueProcessor>(
                    processor => processor.ProcessQueuedNotificationAsync(queueItem.Id),
                    notification.ScheduledAt.Value);
            }
            else
            {
                _backgroundJobClient.Enqueue<NotificationQueueProcessor>(
                    processor => processor.ProcessQueuedNotificationAsync(queueItem.Id));
            }

            _logger.LogInformation("Queued notification {NotificationId} for processing", notification.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue notification {NotificationId}", notification.Id);
            return false;
        }
    }

    public async Task ProcessQueuedNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var pendingItems = await _context.NotificationQueue
            .Where(q => q.Status == NotificationQueueStatus.Pending && 
                       (q.ScheduledFor == null || q.ScheduledFor <= DateTime.UtcNow))
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);

        foreach (var queueItem in pendingItems)
        {
            await ProcessQueueItemAsync(queueItem, cancellationToken);
        }
    }

    public async Task<Dictionary<string, int>> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.NotificationQueue
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(s => s.Status.ToString(), s => s.Count);
    }

    private async Task ProcessQueueItemAsync(NotificationQueue queueItem, CancellationToken cancellationToken)
    {
        try
        {
            queueItem.Status = NotificationQueueStatus.Processing;
            queueItem.ProcessedAt = DateTime.UtcNow;
            queueItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Deserialize notification data
            var notificationData = JsonSerializer.Deserialize<QueuedNotificationData>(queueItem.NotificationData);
            if (notificationData == null)
            {
                throw new InvalidOperationException("Failed to deserialize notification data");
            }

            // Get the actual notification from database
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationData.NotificationId, cancellationToken);

            if (notification == null)
            {
                throw new InvalidOperationException($"Notification {notificationData.NotificationId} not found");
            }

            // Send the notification
            var provider = _providerFactory.GetProvider(notification.Channel);
            var success = await provider.SendAsync(notification, cancellationToken);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                queueItem.Status = NotificationQueueStatus.Completed;
                
                _logger.LogInformation("Successfully processed queued notification {NotificationId}", notification.Id);
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                queueItem.Status = NotificationQueueStatus.Failed;
                queueItem.ErrorMessage = "Provider failed to send notification";
                
                _logger.LogWarning("Failed to process queued notification {NotificationId}", notification.Id);
            }

            notification.UpdatedAt = DateTime.UtcNow;
            queueItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue item {QueueItemId}", queueItem.Id);
            
            queueItem.Status = NotificationQueueStatus.Failed;
            queueItem.ErrorMessage = ex.Message;
            queueItem.RetryCount++;
            queueItem.UpdatedAt = DateTime.UtcNow;

            // Retry logic
            if (queueItem.RetryCount < 3)
            {
                queueItem.Status = NotificationQueueStatus.Pending;
                
                // Schedule retry with exponential backoff
                var retryDelay = TimeSpan.FromMinutes(Math.Pow(2, queueItem.RetryCount));
                _backgroundJobClient.Schedule<NotificationQueueProcessor>(
                    processor => processor.ProcessQueuedNotificationAsync(queueItem.Id),
                    DateTime.UtcNow.Add(retryDelay));
            }

            await _context.SaveChangesAsync();
        }
    }

    private static string GetQueueName(Shared.Common.Enums.NotificationChannel channel, NotificationPriority priority)
    {
        return $"{channel.ToString().ToLower()}_{priority.ToString().ToLower()}";
    }
}

/// <summary>
/// Background job processor for queued notifications
/// </summary>
public class NotificationQueueProcessor
{
    private readonly NotificationDbContext _context;
    private readonly INotificationProviderFactory _providerFactory;
    private readonly ILogger<NotificationQueueProcessor> _logger;

    public NotificationQueueProcessor(
        NotificationDbContext context,
        INotificationProviderFactory providerFactory,
        ILogger<NotificationQueueProcessor> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task ProcessQueuedNotificationAsync(Guid queueItemId)
    {
        try
        {
            var queueItem = await _context.NotificationQueue
                .FirstOrDefaultAsync(q => q.Id == queueItemId);

            if (queueItem == null)
            {
                _logger.LogWarning("Queue item {QueueItemId} not found", queueItemId);
                return;
            }

            if (queueItem.Status != NotificationQueueStatus.Pending)
            {
                _logger.LogInformation("Queue item {QueueItemId} already processed with status {Status}", queueItemId, queueItem.Status);
                return;
            }

            queueItem.Status = NotificationQueueStatus.Processing;
            queueItem.ProcessedAt = DateTime.UtcNow;
            queueItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Deserialize notification data
            var notificationData = JsonSerializer.Deserialize<QueuedNotificationData>(queueItem.NotificationData);
            if (notificationData == null)
            {
                throw new InvalidOperationException("Failed to deserialize notification data");
            }

            // Get the actual notification from database
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationData.NotificationId);

            if (notification == null)
            {
                throw new InvalidOperationException($"Notification {notificationData.NotificationId} not found");
            }

            // Send the notification
            var provider = _providerFactory.GetProvider(notification.Channel);
            var success = await provider.SendAsync(notification);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                queueItem.Status = NotificationQueueStatus.Completed;
                
                _logger.LogInformation("Successfully processed queued notification {NotificationId}", notification.Id);
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                queueItem.Status = NotificationQueueStatus.Failed;
                queueItem.ErrorMessage = "Provider failed to send notification";
                
                _logger.LogWarning("Failed to process queued notification {NotificationId}", notification.Id);
            }

            notification.UpdatedAt = DateTime.UtcNow;
            queueItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue item {QueueItemId}", queueItemId);
            
            var queueItem = await _context.NotificationQueue
                .FirstOrDefaultAsync(q => q.Id == queueItemId);

            if (queueItem != null)
            {
                queueItem.Status = NotificationQueueStatus.Failed;
                queueItem.ErrorMessage = ex.Message;
                queueItem.RetryCount++;
                queueItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}

/// <summary>
/// Data structure for queued notification data
/// </summary>
public class QueuedNotificationData
{
    public Guid NotificationId { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string? RecipientDeviceToken { get; set; }
    public Shared.Common.Enums.NotificationType Type { get; set; }
    public Shared.Common.Enums.NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public string? CorrelationId { get; set; }
    public string? Metadata { get; set; }
}