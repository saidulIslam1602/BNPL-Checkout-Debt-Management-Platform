using YourCompanyBNPL.Common.Enums;
using System.Net;

namespace YourCompanyBNPL.Notification.API.Exceptions;

/// <summary>
/// Base exception for notification-related errors
/// </summary>
public abstract class NotificationException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }
    public Dictionary<string, object>? Details { get; }

    protected NotificationException(
        string errorCode, 
        string message, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Dictionary<string, object>? details = null,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }
}

/// <summary>
/// Exception thrown when a notification is not found
/// </summary>
public class NotificationNotFoundException : NotificationException
{
    public NotificationNotFoundException(Guid notificationId)
        : base(
            "NOTIFICATION_NOT_FOUND",
            $"Notification with ID '{notificationId}' was not found",
            HttpStatusCode.NotFound,
            new Dictionary<string, object> { ["NotificationId"] = notificationId })
    {
    }
}

/// <summary>
/// Exception thrown when a template is not found
/// </summary>
public class TemplateNotFoundException : NotificationException
{
    public TemplateNotFoundException(string templateName, string? language = null)
        : base(
            "TEMPLATE_NOT_FOUND",
            $"Template '{templateName}' not found" + (language != null ? $" for language '{language}'" : ""),
            HttpStatusCode.NotFound,
            new Dictionary<string, object> 
            { 
                ["TemplateName"] = templateName,
                ["Language"] = language ?? "default"
            })
    {
    }

    public TemplateNotFoundException(Guid templateId)
        : base(
            "TEMPLATE_NOT_FOUND",
            $"Template with ID '{templateId}' was not found",
            HttpStatusCode.NotFound,
            new Dictionary<string, object> { ["TemplateId"] = templateId })
    {
    }
}

/// <summary>
/// Exception thrown when template rendering fails
/// </summary>
public class TemplateRenderException : NotificationException
{
    public TemplateRenderException(string templateName, string error, Exception? innerException = null)
        : base(
            "TEMPLATE_RENDER_ERROR",
            $"Failed to render template '{templateName}': {error}",
            HttpStatusCode.BadRequest,
            new Dictionary<string, object> 
            { 
                ["TemplateName"] = templateName,
                ["RenderError"] = error
            },
            innerException)
    {
    }
}

/// <summary>
/// Exception thrown when notification delivery fails
/// </summary>
public class NotificationDeliveryException : NotificationException
{
    public NotificationDeliveryException(string channel, string recipient, string error, Exception? innerException = null)
        : base(
            "DELIVERY_FAILED",
            $"Failed to deliver {channel} notification to '{recipient}': {error}",
            HttpStatusCode.BadGateway,
            new Dictionary<string, object> 
            { 
                ["Channel"] = channel,
                ["Recipient"] = recipient,
                ["DeliveryError"] = error
            },
            innerException)
    {
    }
}

/// <summary>
/// Exception thrown when user has opted out of notifications
/// </summary>
public class NotificationOptOutException : NotificationException
{
    public NotificationOptOutException(Guid customerId, string notificationType, string channel)
        : base(
            "USER_OPTED_OUT",
            $"Customer '{customerId}' has opted out of '{notificationType}' notifications via '{channel}'",
            HttpStatusCode.Forbidden,
            new Dictionary<string, object> 
            { 
                ["CustomerId"] = customerId,
                ["NotificationType"] = notificationType,
                ["Channel"] = channel
            })
    {
    }
}

/// <summary>
/// Exception thrown when rate limit is exceeded
/// </summary>
public class RateLimitExceededException : NotificationException
{
    public RateLimitExceededException(string resource, int limit, TimeSpan window)
        : base(
            "RATE_LIMIT_EXCEEDED",
            $"Rate limit exceeded for '{resource}'. Limit: {limit} requests per {window.TotalMinutes} minutes",
            HttpStatusCode.TooManyRequests,
            new Dictionary<string, object> 
            { 
                ["Resource"] = resource,
                ["Limit"] = limit,
                ["WindowMinutes"] = window.TotalMinutes
            })
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class NotificationValidationException : NotificationException
{
    public NotificationValidationException(string field, string error)
        : base(
            "VALIDATION_ERROR",
            $"Validation failed for field '{field}': {error}",
            HttpStatusCode.BadRequest,
            new Dictionary<string, object> 
            { 
                ["Field"] = field,
                ["ValidationError"] = error
            })
    {
    }

    public NotificationValidationException(Dictionary<string, string> validationErrors)
        : base(
            "VALIDATION_ERROR",
            "Multiple validation errors occurred",
            HttpStatusCode.BadRequest,
            new Dictionary<string, object> 
            { 
                ["ValidationErrors"] = validationErrors
            })
    {
    }
}

/// <summary>
/// Exception thrown when external service is unavailable
/// </summary>
public class ExternalServiceException : NotificationException
{
    public ExternalServiceException(string serviceName, string error, Exception? innerException = null)
        : base(
            "EXTERNAL_SERVICE_ERROR",
            $"External service '{serviceName}' error: {error}",
            HttpStatusCode.BadGateway,
            new Dictionary<string, object> 
            { 
                ["ServiceName"] = serviceName,
                ["ServiceError"] = error
            },
            innerException)
    {
    }
}

/// <summary>
/// Exception thrown when campaign operation fails
/// </summary>
public class CampaignException : NotificationException
{
    public CampaignException(Guid campaignId, string operation, string error)
        : base(
            "CAMPAIGN_ERROR",
            $"Campaign '{campaignId}' {operation} failed: {error}",
            HttpStatusCode.BadRequest,
            new Dictionary<string, object> 
            { 
                ["CampaignId"] = campaignId,
                ["Operation"] = operation,
                ["Error"] = error
            })
    {
    }
}

/// <summary>
/// Exception thrown when configuration is invalid
/// </summary>
public class ConfigurationException : NotificationException
{
    public ConfigurationException(string configKey, string error)
        : base(
            "CONFIGURATION_ERROR",
            $"Configuration error for '{configKey}': {error}",
            HttpStatusCode.InternalServerError,
            new Dictionary<string, object> 
            { 
                ["ConfigurationKey"] = configKey,
                ["ConfigurationError"] = error
            })
    {
    }
}