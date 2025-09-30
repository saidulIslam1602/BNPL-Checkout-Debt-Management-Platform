using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YourCompanyBNPL.Notification.API.Data;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Models;
using YourCompanyBNPL.Notification.API.Exceptions;
using YourCompanyBNPL.Notification.API.Infrastructure;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Services;

/// <summary>
/// Implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPushNotificationService _pushService;
    private readonly ITemplateService _templateService;
    private readonly IPreferenceService _preferenceService;
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly INotificationThrottlingService _throttlingService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IPushNotificationService pushService,
        ITemplateService templateService,
        IPreferenceService preferenceService,
        ICircuitBreakerService circuitBreaker,
        INotificationThrottlingService throttlingService,
        IWebhookService webhookService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _pushService = pushService;
        _templateService = templateService;
        _preferenceService = preferenceService;
        _circuitBreaker = circuitBreaker;
        _throttlingService = throttlingService;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending notification of type {Type} via {Channel} to {Recipient}", 
                request.Type, request.Channel, request.Recipient);

            // Check throttling limits
            var canSend = await _throttlingService.CanSendAsync(request.Channel, request.Recipient, cancellationToken);
            if (!canSend)
            {
                var delay = await _throttlingService.GetDelayAsync(request.Channel, request.Recipient, cancellationToken);
                throw new RateLimitExceededException(
                    $"{request.Channel}:{request.Recipient}", 
                    0, // Will be filled by throttling service
                    delay ?? TimeSpan.FromMinutes(1));
            }

            // Check user preferences if customer ID is provided
            if (request.CustomerId.HasValue)
            {
                var isOptedIn = await _preferenceService.IsOptedInAsync(request.CustomerId.Value, request.Type, request.Channel, cancellationToken);
                if (!isOptedIn)
                {
                    throw new NotificationOptOutException(request.CustomerId.Value, request.Type, request.Channel.ToString());
                }
            }

            // Create notification entity
            var notification = new Models.Notification
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                Channel = request.Channel,
                Recipient = request.Recipient,
                Subject = request.Subject ?? string.Empty,
                Content = request.Content ?? string.Empty,
                TemplateId = request.TemplateId?.ToString(),
                TemplateData = request.TemplateData != null ? JsonSerializer.Serialize(request.TemplateData) : null,
                Status = NotificationStatus.Pending,
                Priority = request.Priority,
                ScheduledAt = request.ScheduledAt,
                CustomerId = request.CustomerId,
                MerchantId = request.MerchantId,
                PaymentId = request.PaymentId,
                InstallmentId = request.InstallmentId,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Render template if specified
            if (request.TemplateId.HasValue)
            {
                var templateResponse = await _templateService.RenderTemplateAsync(request.TemplateId.Value, request.TemplateData ?? new(), cancellationToken);
                if (!templateResponse.Success)
                {
                    return ApiResponse<NotificationResponse>.ErrorResult($"Failed to render template: {templateResponse.Message}");
                }

                notification.Subject = templateResponse.Data.Subject;
                notification.Content = request.Channel switch
                {
                    NotificationChannel.Email => templateResponse.Data.HtmlContent ?? templateResponse.Data.TextContent ?? string.Empty,
                    NotificationChannel.SMS => templateResponse.Data.SmsContent ?? templateResponse.Data.TextContent ?? string.Empty,
                    NotificationChannel.Push => templateResponse.Data.PushContent ?? templateResponse.Data.TextContent ?? string.Empty,
                    _ => templateResponse.Data.TextContent ?? string.Empty
                };
            }

            // Save to database
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Send immediately if not scheduled
            if (!request.ScheduledAt.HasValue)
            {
                await SendNotificationNowAsync(notification, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var response = MapToResponse(notification);
            return ApiResponse<NotificationResponse>.SuccessResult(response, "Notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return ApiResponse<NotificationResponse>.ErrorResult($"Failed to send notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<NotificationResponse>>> SendBulkNotificationAsync(SendBulkNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var responses = new List<NotificationResponse>();
            var batchId = request.BatchId ?? Guid.NewGuid().ToString();

            foreach (var notificationRequest in request.Notifications)
            {
                var result = await SendNotificationAsync(notificationRequest, cancellationToken);
                if (result.Success && result.Data != null)
                {
                    // Update batch ID
                    var notification = await _context.Notifications.FindAsync(result.Data.Id);
                    if (notification != null)
                    {
                        notification.BatchId = batchId;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    responses.Add(result.Data);
                }
            }

            return ApiResponse<List<NotificationResponse>>.SuccessResult(responses, $"Sent {responses.Count} notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk notifications");
            return ApiResponse<List<NotificationResponse>>.ErrorResult($"Failed to send bulk notifications: {ex.Message}");
        }
    }

    public async Task<ApiResponse<NotificationResponse>> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            throw new NotificationNotFoundException(notificationId);
        }

        var response = MapToResponse(notification);
        return ApiResponse<NotificationResponse>.SuccessResult(response);
    }

    public async Task<PagedApiResponse<NotificationResponse>> SearchNotificationsAsync(NotificationSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Type))
                query = query.Where(n => n.Type == request.Type);

            if (request.Channel.HasValue)
                query = query.Where(n => n.Channel == request.Channel.Value);

            if (request.Status.HasValue)
                query = query.Where(n => n.Status == request.Status.Value);

            if (request.CustomerId.HasValue)
                query = query.Where(n => n.CustomerId == request.CustomerId.Value);

            if (request.MerchantId.HasValue)
                query = query.Where(n => n.MerchantId == request.MerchantId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(n => n.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(n => n.CreatedAt <= request.ToDate.Value);

            if (!string.IsNullOrEmpty(request.BatchId))
                query = query.Where(n => n.BatchId == request.BatchId);

            var totalCount = await query.CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = notifications.Select(MapToResponse).ToList();

            return PagedApiResponse<NotificationResponse>.SuccessResult(responses, totalCount, request.Page, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search notifications");
            return PagedApiResponse<NotificationResponse>.ErrorResult($"Failed to search notifications: {ex.Message}");
        }
    }

    public async Task<ApiResponse<NotificationResponse>> RetryNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return ApiResponse<NotificationResponse>.ErrorResult("Notification not found");
            }

            if (notification.Status != NotificationStatus.Failed)
            {
                return ApiResponse<NotificationResponse>.ErrorResult("Only failed notifications can be retried");
            }

            if (notification.RetryCount >= notification.MaxRetries)
            {
                return ApiResponse<NotificationResponse>.ErrorResult("Maximum retry attempts exceeded");
            }

            notification.Status = NotificationStatus.Pending;
            notification.RetryCount++;
            notification.ErrorMessage = null;
            notification.UpdatedAt = DateTime.UtcNow;

            await SendNotificationNowAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(notification);
            return ApiResponse<NotificationResponse>.SuccessResult(response, "Notification retry initiated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry notification {NotificationId}", notificationId);
            return ApiResponse<NotificationResponse>.ErrorResult($"Failed to retry notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse> CancelNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return ApiResponse.ErrorResult("Notification not found");
            }

            if (notification.Status != NotificationStatus.Pending && notification.Status != NotificationStatus.Scheduled)
            {
                return ApiResponse.ErrorResult("Only pending or scheduled notifications can be cancelled");
            }

            notification.Status = NotificationStatus.Cancelled;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.SuccessResponse("Notification cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel notification {NotificationId}", notificationId);
            return ApiResponse.ErrorResult($"Failed to cancel notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse<NotificationAnalytics>> GetAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.CreatedAt >= fromDate && n.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);

            var analytics = new NotificationAnalytics
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalSent = notifications.Count(n => n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered),
                TotalDelivered = notifications.Count(n => n.Status == NotificationStatus.Delivered),
                TotalFailed = notifications.Count(n => n.Status == NotificationStatus.Failed),
                TotalOpened = notifications.Count(n => n.ReadAt.HasValue),
                ByChannel = notifications.GroupBy(n => n.Channel).ToDictionary(g => g.Key, g => g.Count()),
                ByType = notifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count()),
                ByStatus = notifications.GroupBy(n => n.Status).ToDictionary(g => g.Key, g => g.Count())
            };

            if (analytics.TotalSent > 0)
            {
                analytics.DeliveryRate = (decimal)analytics.TotalDelivered / analytics.TotalSent * 100;
                analytics.OpenRate = (decimal)analytics.TotalOpened / analytics.TotalSent * 100;
            }

            return ApiResponse<NotificationAnalytics>.SuccessResult(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics");
            return ApiResponse<NotificationAnalytics>.ErrorResult($"Failed to get analytics: {ex.Message}");
        }
    }

    public async Task<ApiResponse<int>> ProcessScheduledNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduledNotifications = await _context.Notifications
                .Where(n => n.Status == NotificationStatus.Scheduled && n.ScheduledAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            var processedCount = 0;
            foreach (var notification in scheduledNotifications)
            {
                await SendNotificationNowAsync(notification, cancellationToken);
                processedCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<int>.SuccessResult(processedCount, $"Processed {processedCount} scheduled notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled notifications");
            return ApiResponse<int>.ErrorResult($"Failed to process scheduled notifications: {ex.Message}");
        }
    }

    public async Task<ApiResponse<int>> ProcessRetryNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var retryNotifications = await _context.Notifications
                .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < n.MaxRetries)
                .ToListAsync(cancellationToken);

            var processedCount = 0;
            foreach (var notification in retryNotifications)
            {
                notification.RetryCount++;
                notification.Status = NotificationStatus.Pending;
                await SendNotificationNowAsync(notification, cancellationToken);
                processedCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<int>.SuccessResult(processedCount, $"Processed {processedCount} retry notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process retry notifications");
            return ApiResponse<int>.ErrorResult($"Failed to process retry notifications: {ex.Message}");
        }
    }

    private async Task SendNotificationNowAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            notification.Status = NotificationStatus.Sending;

            (bool success, string? externalId, string? errorMessage) = notification.Channel switch
            {
                NotificationChannel.Email => await _emailService.SendEmailAsync(notification.Recipient, notification.Subject, notification.Content, cancellationToken: cancellationToken),
                NotificationChannel.SMS => await _smsService.SendSmsAsync(notification.Recipient, notification.Content),
                NotificationChannel.Push => await _pushService.SendPushNotificationAsync(notification.Recipient, notification.Subject, notification.Content, cancellationToken: cancellationToken),
                _ => (false, null, "Unsupported notification channel")
            };

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.ExternalId = externalId;
                
                // Record the sent notification for throttling
                await _throttlingService.RecordSentAsync(notification.Channel, notification.Recipient, cancellationToken);
                
                // Trigger webhook for successful send
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _webhookService.TriggerWebhookAsync(notification.Id, Models.WebhookEvent.NotificationSent, null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to trigger webhook for notification sent: {NotificationId}", notification.Id);
                    }
                }, cancellationToken);
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = errorMessage;
                
                // Trigger webhook for failed send
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var additionalData = new Dictionary<string, object>
                        {
                            ["ErrorMessage"] = errorMessage ?? "Unknown error"
                        };
                        await _webhookService.TriggerWebhookAsync(notification.Id, Models.WebhookEvent.NotificationFailed, additionalData, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to trigger webhook for notification failed: {NotificationId}", notification.Id);
                    }
                }, cancellationToken);
            }

            notification.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            notification.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static NotificationResponse MapToResponse(Models.Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Channel = notification.Channel,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Content = notification.Content,
            Status = notification.Status,
            Priority = notification.Priority,
            ScheduledAt = notification.ScheduledAt,
            SentAt = notification.SentAt,
            DeliveredAt = notification.DeliveredAt,
            ReadAt = notification.ReadAt,
            RetryCount = notification.RetryCount,
            ErrorMessage = notification.ErrorMessage,
            ExternalId = notification.ExternalId,
            CustomerId = notification.CustomerId,
            MerchantId = notification.MerchantId,
            PaymentId = notification.PaymentId,
            InstallmentId = notification.InstallmentId,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt ?? notification.CreatedAt
        };
    }
}