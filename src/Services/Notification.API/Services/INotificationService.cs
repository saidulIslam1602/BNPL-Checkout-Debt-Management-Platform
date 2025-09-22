using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// Interface for notification operations
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a single notification
    /// </summary>
    Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends bulk notifications
    /// </summary>
    Task<ApiResponse<List<NotificationResponse>>> SendBulkNotificationAsync(SendBulkNotificationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets notification by ID
    /// </summary>
    Task<ApiResponse<NotificationResponse>> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches notifications with filtering
    /// </summary>
    Task<PagedApiResponse<NotificationResponse>> SearchNotificationsAsync(NotificationSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retries failed notification
    /// </summary>
    Task<ApiResponse<NotificationResponse>> RetryNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels scheduled notification
    /// </summary>
    Task<ApiResponse> CancelNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets notification analytics
    /// </summary>
    Task<ApiResponse<NotificationAnalytics>> GetAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes scheduled notifications
    /// </summary>
    Task<ApiResponse<int>> ProcessScheduledNotificationsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes retry notifications
    /// </summary>
    Task<ApiResponse<int>> ProcessRetryNotificationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for email service
/// </summary>
public interface IEmailService
{
    Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendEmailAsync(string to, string subject, string htmlContent, string? textContent = null, CancellationToken cancellationToken = default);
    Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkEmailAsync(List<(string To, string Subject, string HtmlContent, string? TextContent)> emails, CancellationToken cancellationToken = default);
    Task<bool> ValidateEmailAsync(string email);
}

/// <summary>
/// Interface for SMS service
/// </summary>
public interface ISmsService
{
    Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default);
    Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkSmsAsync(List<(string To, string Message)> messages, CancellationToken cancellationToken = default);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
}

/// <summary>
/// Interface for push notification service
/// </summary>
public interface IPushNotificationService
{
    Task<(bool Success, string? ExternalId, string? ErrorMessage)> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);
    Task<(bool Success, List<string> ExternalIds, string? ErrorMessage)> SendBulkPushNotificationAsync(List<(string DeviceToken, string Title, string Body, Dictionary<string, object>? Data)> notifications, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for template service
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Creates a new template
    /// </summary>
    Task<ApiResponse<TemplateResponse>> CreateTemplateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates existing template
    /// </summary>
    Task<ApiResponse<TemplateResponse>> UpdateTemplateAsync(Guid templateId, CreateTemplateRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets template by ID
    /// </summary>
    Task<ApiResponse<TemplateResponse>> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets template by name and language
    /// </summary>
    Task<ApiResponse<TemplateResponse>> GetTemplateByNameAsync(string name, string language = "en", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists templates with filtering
    /// </summary>
    Task<PagedApiResponse<TemplateResponse>> ListTemplatesAsync(string? type = null, string? language = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders template with data
    /// </summary>
    Task<ApiResponse<(string Subject, string HtmlContent, string? TextContent, string? SmsContent, string? PushContent)>> RenderTemplateAsync(Guid templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes template
    /// </summary>
    Task<ApiResponse> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for preference service
/// </summary>
public interface IPreferenceService
{
    /// <summary>
    /// Updates customer notification preferences
    /// </summary>
    Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(Guid customerId, UpdatePreferencesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets customer notification preferences
    /// </summary>
    Task<ApiResponse<PreferencesResponse>> GetPreferencesAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if customer has opted in for specific notification type and channel
    /// </summary>
    Task<bool> IsOptedInAsync(Guid customerId, string notificationType, Models.NotificationChannel channel, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for campaign service
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Creates a new campaign
    /// </summary>
    Task<ApiResponse<CampaignResponse>> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets campaign by ID
    /// </summary>
    Task<ApiResponse<CampaignResponse>> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists campaigns
    /// </summary>
    Task<PagedApiResponse<CampaignResponse>> ListCampaignsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts campaign execution
    /// </summary>
    Task<ApiResponse> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pauses campaign execution
    /// </summary>
    Task<ApiResponse> PauseCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels campaign
    /// </summary>
    Task<ApiResponse> CancelCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
}