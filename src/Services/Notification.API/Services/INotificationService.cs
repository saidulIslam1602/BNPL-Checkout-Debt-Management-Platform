using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Models;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Services;

// Core interfaces for Notification API services

public interface INotificationService
{
    Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<NotificationResponse>>> SendBulkNotificationAsync(SendBulkNotificationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<NotificationResponse>> GetNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedApiResponse<NotificationResponse>> SearchNotificationsAsync(NotificationSearchRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<NotificationResponse>> RetryNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse> CancelNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<NotificationAnalytics>> GetAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessScheduledNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ProcessRetryNotificationsAsync(CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task<(bool success, string? externalId, string? errorMessage)> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface ISmsService
{
    Task<(bool success, string? externalId, string? errorMessage)> SendSmsAsync(string phoneNumber, string message);
}

public interface IPushNotificationService
{
    Task<(bool success, string? externalId, string? errorMessage)> SendPushNotificationAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
}

public interface ITemplateService
{
    Task<ApiResponse<TemplateRenderResult>> RenderTemplateAsync(Guid templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default);
}

public interface IPreferenceService
{
    Task<bool> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken cancellationToken);
    Task<NotificationPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsOptedInAsync(Guid userId, string notificationType, NotificationChannel channel, CancellationToken cancellationToken);
}

public interface ICampaignService
{
    Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request);
}

public interface INotificationThrottlingService
{
    Task<bool> CanSendAsync(NotificationChannel channel, string recipient, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetDelayAsync(NotificationChannel channel, string recipient, CancellationToken cancellationToken = default);
    Task RecordSentAsync(NotificationChannel channel, string recipient, CancellationToken cancellationToken = default);
}

public interface IWebhookService
{
    Task<ApiResponse<WebhookDeliveryResponse>> CreateWebhookAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WebhookDeliveryResponse>> UpdateWebhookAsync(Guid id, CreateWebhookRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WebhookDeliveryResponse>> GetWebhookAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedApiResponse<WebhookDeliveryResponse>> ListWebhooksAsync(Guid? customerId, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteWebhookAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse> TriggerWebhookAsync(Guid notificationId, Models.WebhookEvent eventType, Dictionary<string, object>? additionalData, CancellationToken cancellationToken = default);
    Task<PagedApiResponse<WebhookDeliveryResponse>> GetWebhookDeliveriesAsync(Guid? webhookId, Guid? notificationId, Common.Enums.WebhookDeliveryStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse> RetryWebhookDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);
}