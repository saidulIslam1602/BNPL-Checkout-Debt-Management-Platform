using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RivertyBNPL.Services.Notification.API.Data;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Models;
using RivertyBNPL.Services.Notification.API.Providers;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Services;

/// <summary>
/// Implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationTemplateService _templateService;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly INotificationQueueService _queueService;
    private readonly INotificationProviderFactory _providerFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IMapper mapper,
        INotificationTemplateService templateService,
        INotificationPreferenceService preferenceService,
        INotificationQueueService queueService,
        INotificationProviderFactory providerFactory,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _templateService = templateService;
        _preferenceService = preferenceService;
        _queueService = queueService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending notification to {RecipientId} via {Channel}", request.RecipientId, request.Channel);

        // Check user preferences
        var isAllowed = await _preferenceService.IsNotificationAllowedAsync(request.RecipientId, request.Type, request.Channel, cancellationToken);
        if (!isAllowed)
        {
            _logger.LogWarning("Notification blocked by user preferences for {RecipientId}", request.RecipientId);
            throw new InvalidOperationException("Notification blocked by user preferences");
        }

        // Create notification entity
        var notification = await CreateNotificationEntityAsync(request, cancellationToken);

        // Save to database
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Queue for processing if scheduled, otherwise send immediately
        if (request.ScheduledAt.HasValue)
        {
            var optimalTime = await _preferenceService.GetOptimalSendTimeAsync(request.RecipientId, request.ScheduledAt.Value, cancellationToken);
            notification.ScheduledAt = optimalTime ?? request.ScheduledAt.Value;
            await _queueService.QueueNotificationAsync(notification, cancellationToken);
            notification.Status = NotificationStatus.Queued;
        }
        else
        {
            await SendNotificationImmediatelyAsync(notification, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<NotificationResponse>(notification);
    }

    public async Task<BulkNotificationResponse> SendBulkNotificationAsync(SendBulkNotificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending bulk notification to {Count} recipients", request.Recipients.Count);

        var response = new BulkNotificationResponse
        {
            BatchId = request.BatchId ?? Guid.NewGuid().ToString(),
            TotalCount = request.Recipients.Count
        };

        var notifications = new List<Models.Notification>();

        foreach (var recipient in request.Recipients)
        {
            try
            {
                // Check user preferences
                var isAllowed = await _preferenceService.IsNotificationAllowedAsync(recipient.RecipientId, request.Type, request.Channel, cancellationToken);
                if (!isAllowed)
                {
                    response.Errors.Add($"Notification blocked by user preferences for {recipient.RecipientId}");
                    response.FailedCount++;
                    continue;
                }

                // Merge template data
                var templateData = new Dictionary<string, object>(request.CommonTemplateData ?? new Dictionary<string, object>());
                if (recipient.TemplateData != null)
                {
                    foreach (var kvp in recipient.TemplateData)
                    {
                        templateData[kvp.Key] = kvp.Value;
                    }
                }

                // Create individual notification request
                var individualRequest = new SendNotificationRequest
                {
                    RecipientId = recipient.RecipientId,
                    RecipientEmail = recipient.RecipientEmail,
                    RecipientPhone = recipient.RecipientPhone,
                    RecipientDeviceToken = recipient.RecipientDeviceToken,
                    Type = request.Type,
                    Channel = request.Channel,
                    Subject = request.Subject,
                    Content = request.Content,
                    TemplateName = request.TemplateName,
                    TemplateData = templateData,
                    Priority = request.Priority,
                    ScheduledAt = request.ScheduledAt,
                    CorrelationId = recipient.CorrelationId,
                    Metadata = request.Metadata
                };

                var notification = await CreateNotificationEntityAsync(individualRequest, cancellationToken);
                notification.BatchId = response.BatchId;
                notifications.Add(notification);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for recipient {RecipientId}", recipient.RecipientId);
                response.Errors.Add($"Failed to create notification for {recipient.RecipientId}: {ex.Message}");
                response.FailedCount++;
            }
        }

        // Save all notifications
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);

        // Process notifications
        foreach (var notification in notifications)
        {
            try
            {
                if (request.ScheduledAt.HasValue)
                {
                    var optimalTime = await _preferenceService.GetOptimalSendTimeAsync(notification.RecipientId, request.ScheduledAt.Value, cancellationToken);
                    notification.ScheduledAt = optimalTime ?? request.ScheduledAt.Value;
                    await _queueService.QueueNotificationAsync(notification, cancellationToken);
                    notification.Status = NotificationStatus.Queued;
                }
                else
                {
                    await SendNotificationImmediatelyAsync(notification, cancellationToken);
                }

                response.Notifications.Add(_mapper.Map<NotificationResponse>(notification));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification {NotificationId}", notification.Id);
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = ex.Message;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<NotificationResponse?> GetNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        return notification != null ? _mapper.Map<NotificationResponse>(notification) : null;
    }

    public async Task<(List<NotificationResponse> Notifications, int TotalCount)> GetNotificationsAsync(NotificationQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(queryParams.RecipientId))
            query = query.Where(n => n.RecipientId == queryParams.RecipientId);

        if (queryParams.Type.HasValue)
            query = query.Where(n => n.Type == queryParams.Type.Value);

        if (queryParams.Channel.HasValue)
            query = query.Where(n => n.Channel == queryParams.Channel.Value);

        if (queryParams.Status.HasValue)
            query = query.Where(n => n.Status == queryParams.Status.Value);

        if (queryParams.FromDate.HasValue)
            query = query.Where(n => n.CreatedAt >= queryParams.FromDate.Value);

        if (queryParams.ToDate.HasValue)
            query = query.Where(n => n.CreatedAt <= queryParams.ToDate.Value);

        if (!string.IsNullOrEmpty(queryParams.BatchId))
            query = query.Where(n => n.BatchId == queryParams.BatchId);

        if (!string.IsNullOrEmpty(queryParams.CorrelationId))
            query = query.Where(n => n.CorrelationId == queryParams.CorrelationId);

        // Apply sorting
        query = queryParams.SortBy?.ToLower() switch
        {
            "updatedat" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.UpdatedAt) : query.OrderByDescending(n => n.UpdatedAt),
            "sentat" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.SentAt) : query.OrderByDescending(n => n.SentAt),
            "deliveredat" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.DeliveredAt) : query.OrderByDescending(n => n.DeliveredAt),
            "type" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.Type) : query.OrderByDescending(n => n.Type),
            "channel" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.Channel) : query.OrderByDescending(n => n.Channel),
            "status" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.Status) : query.OrderByDescending(n => n.Status),
            "priority" => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.Priority) : query.OrderByDescending(n => n.Priority),
            _ => queryParams.SortOrder == "asc" ? query.OrderBy(n => n.CreatedAt) : query.OrderByDescending(n => n.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var notifications = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return (_mapper.Map<List<NotificationResponse>>(notifications), totalCount);
    }

    public async Task<bool> UpdateNotificationStatusAsync(Guid id, UpdateNotificationStatusRequest request, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null)
            return false;

        notification.Status = request.Status;
        notification.ErrorMessage = request.ErrorMessage;
        notification.ExternalId = request.ExternalId;
        notification.UpdatedAt = DateTime.UtcNow;

        if (request.Status == NotificationStatus.Sent && !notification.SentAt.HasValue)
            notification.SentAt = DateTime.UtcNow;

        if (request.Status == NotificationStatus.Delivered && !notification.DeliveredAt.HasValue)
            notification.DeliveredAt = DateTime.UtcNow;

        if (request.Metadata != null)
        {
            notification.Metadata = JsonSerializer.Serialize(request.Metadata);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RecordDeliveryEventAsync(Guid id, NotificationDeliveryEventRequest request, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null)
            return false;

        var notificationEvent = new NotificationEvent
        {
            Id = Guid.NewGuid(),
            NotificationId = id,
            EventType = request.EventType,
            EventTime = request.EventTime,
            ExternalId = request.ExternalId,
            EventData = request.EventData != null ? JsonSerializer.Serialize(request.EventData) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationEvents.Add(notificationEvent);

        // Update notification status based on event type
        switch (request.EventType.ToLower())
        {
            case "delivered":
                notification.Status = NotificationStatus.Delivered;
                notification.DeliveredAt = request.EventTime;
                break;
            case "opened":
            case "read":
                notification.ReadAt = request.EventTime;
                break;
            case "bounced":
            case "failed":
                notification.Status = NotificationStatus.Failed;
                break;
        }

        notification.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null || notification.Status != NotificationStatus.Queued)
            return false;

        notification.Status = NotificationStatus.Cancelled;
        notification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<NotificationResponse?> RetryNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null || notification.Status != NotificationStatus.Failed)
            return null;

        if (notification.RetryCount >= notification.MaxRetries)
        {
            _logger.LogWarning("Maximum retry attempts reached for notification {NotificationId}", id);
            return null;
        }

        notification.RetryCount++;
        notification.Status = NotificationStatus.Pending;
        notification.ErrorMessage = null;
        notification.UpdatedAt = DateTime.UtcNow;

        await SendNotificationImmediatelyAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<NotificationResponse>(notification);
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.CreatedAt >= fromDate && n.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var stats = new NotificationStatsDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalSent = notifications.Count(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered),
            TotalDelivered = notifications.Count(n => n.Status == NotificationStatus.Delivered),
            TotalFailed = notifications.Count(n => n.Status == NotificationStatus.Failed),
            ByChannel = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key, g => g.Count()),
            ByType = notifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
            ByStatus = notifications.GroupBy(n => n.Status).ToDictionary(g => g.Key, g => g.Count())
        };

        stats.DeliveryRate = stats.TotalSent > 0 ? (decimal)stats.TotalDelivered / stats.TotalSent * 100 : 0;

        return stats;
    }

    private async Task<Models.Notification> CreateNotificationEntityAsync(SendNotificationRequest request, CancellationToken cancellationToken)
    {
        string subject = request.Subject ?? string.Empty;
        string content = request.Content ?? string.Empty;
        string? htmlContent = null;

        // Use template if specified
        if (!string.IsNullOrEmpty(request.TemplateName))
        {
            var templateData = request.TemplateData ?? new Dictionary<string, object>();
            var (templateSubject, templateBody, templateHtml) = await _templateService.RenderTemplateAsync(request.TemplateName, templateData, cancellationToken);
            
            subject = templateSubject;
            content = templateBody;
            htmlContent = templateHtml;
        }

        var notification = new Models.Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = request.RecipientId,
            RecipientEmail = request.RecipientEmail,
            RecipientPhone = request.RecipientPhone,
            RecipientDeviceToken = request.RecipientDeviceToken,
            Type = request.Type,
            Channel = request.Channel,
            Subject = subject,
            Content = htmlContent ?? content,
            TemplateData = request.TemplateData != null ? JsonSerializer.Serialize(request.TemplateData) : null,
            Status = NotificationStatus.Pending,
            Priority = request.Priority,
            ScheduledAt = request.ScheduledAt,
            CorrelationId = request.CorrelationId,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return notification;
    }

    private async Task SendNotificationImmediatelyAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            notification.Status = NotificationStatus.Sending;
            
            var provider = _providerFactory.GetProvider(notification.Channel);
            var success = await provider.SendAsync(notification, cancellationToken);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "Failed to send notification";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
        }

        notification.UpdatedAt = DateTime.UtcNow;
    }
}