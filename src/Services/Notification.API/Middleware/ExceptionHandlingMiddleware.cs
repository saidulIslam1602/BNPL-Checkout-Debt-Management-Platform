using YourCompanyBNPL.Common.Enums;
using System.Net;
using System.Text.Json;
using YourCompanyBNPL.Notification.API.Exceptions;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        _logger.LogError(exception, "An error occurred processing request {CorrelationId}: {Message}", 
            correlationId, exception.Message);

        var response = CreateErrorResponse(exception, correlationId);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)GetStatusCode(exception);

        await context.Response.WriteAsync(jsonResponse);
    }

    private ApiResponse<object> CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            NotificationException notificationEx => ApiResponse<object>.ErrorResult(
                notificationEx.Message, 400),

            FluentValidation.ValidationException validationEx => ApiResponse<object>.ErrorResult(
                validationEx.Errors.Select(e => e.ErrorMessage).ToList(), 400),

            ArgumentException argEx => ApiResponse<object>.ErrorResult(
                argEx.Message, 400),

            UnauthorizedAccessException => ApiResponse<object>.ErrorResult(
                "Access denied", 401),

            TimeoutException timeoutEx => ApiResponse<object>.ErrorResult(
                "Request timeout", 408),

            _ => ApiResponse<object>.ErrorResult(
                _environment.IsDevelopment() ? exception.Message : "An internal server error occurred", 500)
        };
    }

    private Dictionary<string, object> CreateErrorDetails(NotificationException exception, string correlationId)
    {
        var details = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ErrorCode"] = exception.ErrorCode,
            ["Timestamp"] = DateTime.UtcNow
        };

        if (exception.Details != null)
        {
            foreach (var detail in exception.Details)
            {
                details[detail.Key] = detail.Value;
            }
        }

        if (_environment.IsDevelopment() && exception.InnerException != null)
        {
            details["InnerException"] = exception.InnerException.Message;
            details["StackTrace"] = exception.StackTrace;
        }

        return details;
    }

    private Dictionary<string, object> CreateGenericErrorDetails(Exception exception, string correlationId)
    {
        var details = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ErrorCode"] = "INTERNAL_SERVER_ERROR",
            ["Timestamp"] = DateTime.UtcNow
        };

        if (_environment.IsDevelopment())
        {
            details["ExceptionType"] = exception.GetType().Name;
            details["Message"] = exception.Message;
            details["StackTrace"] = exception.StackTrace;
        }

        return details;
    }

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            NotificationException notificationEx => notificationEx.StatusCode,
            FluentValidation.ValidationException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };
    }
}

/// <summary>
/// Extension method to register the exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}