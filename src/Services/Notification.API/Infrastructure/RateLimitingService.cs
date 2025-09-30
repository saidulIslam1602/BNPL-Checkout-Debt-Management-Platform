using YourCompanyBNPL.Common.Enums;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using YourCompanyBNPL.Notification.API.Exceptions;

namespace YourCompanyBNPL.Notification.API.Infrastructure;

/// <summary>
/// Rate limiting service interface
/// </summary>
public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limiting service implementation using sliding window algorithm
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IDistributedCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var rateLimitInfo = await GetRateLimitInfoAsync(key, limit, window, cancellationToken);
        return rateLimitInfo.RequestsRemaining > 0;
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rate_limit:{key}";
        var now = DateTimeOffset.UtcNow;
        
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            var rateLimitData = string.IsNullOrEmpty(cachedData) 
                ? new RateLimitData() 
                : JsonSerializer.Deserialize<RateLimitData>(cachedData) ?? new RateLimitData();

            // Clean old requests outside the window
            var windowStart = now.Subtract(window);
            rateLimitData.Requests = rateLimitData.Requests
                .Where(r => r > windowStart)
                .ToList();

            // Add current request
            rateLimitData.Requests.Add(now);

            // Update cache
            var serializedData = JsonSerializer.Serialize(rateLimitData);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window.Add(TimeSpan.FromMinutes(1)) // Add buffer
            };
            
            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

            var requestsInWindow = rateLimitData.Requests.Count;
            var requestsRemaining = Math.Max(0, limit - requestsInWindow);
            var resetTime = rateLimitData.Requests.Count > 0 
                ? rateLimitData.Requests.Min().Add(window)
                : now.Add(window);

            return new RateLimitInfo
            {
                Limit = limit,
                RequestsInWindow = requestsInWindow,
                RequestsRemaining = requestsRemaining,
                WindowStart = windowStart,
                WindowEnd = now,
                ResetTime = resetTime,
                IsAllowed = requestsRemaining > 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check rate limit for key: {Key}", key);
            
            // On error, allow the request but log the issue
            return new RateLimitInfo
            {
                Limit = limit,
                RequestsInWindow = 0,
                RequestsRemaining = limit,
                WindowStart = now.Subtract(window),
                WindowEnd = now,
                ResetTime = now.Add(window),
                IsAllowed = true
            };
        }
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rate_limit:{key}";
        await _cache.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogDebug("Reset rate limit for key: {Key}", key);
    }
}

/// <summary>
/// Rate limit data stored in cache
/// </summary>
public class RateLimitData
{
    public List<DateTimeOffset> Requests { get; set; } = new();
}

/// <summary>
/// Rate limit information
/// </summary>
public class RateLimitInfo
{
    public int Limit { get; set; }
    public int RequestsInWindow { get; set; }
    public int RequestsRemaining { get; set; }
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public DateTimeOffset ResetTime { get; set; }
    public bool IsAllowed { get; set; }
}

/// <summary>
/// Rate limiting middleware
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitingService rateLimitingService,
        RateLimitOptions options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitingService = rateLimitingService;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var rateLimitPolicy = GetRateLimitPolicy(context, endpoint);
        
        if (rateLimitPolicy == null)
        {
            await _next(context);
            return;
        }

        var key = GenerateKey(context, rateLimitPolicy);
        var rateLimitInfo = await _rateLimitingService.GetRateLimitInfoAsync(
            key, 
            rateLimitPolicy.Limit, 
            rateLimitPolicy.Window);

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", rateLimitPolicy.Limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", rateLimitInfo.RequestsRemaining.ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", rateLimitInfo.ResetTime.ToUnixTimeSeconds().ToString());

        if (!rateLimitInfo.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
            
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", ((int)rateLimitPolicy.Window.TotalSeconds).ToString());
            
            var errorResponse = new
            {
                error = "Rate limit exceeded",
                message = $"Too many requests. Limit: {rateLimitPolicy.Limit} per {rateLimitPolicy.Window.TotalMinutes} minutes",
                retryAfter = rateLimitInfo.ResetTime
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        await _next(context);
    }

    private RateLimitPolicy? GetRateLimitPolicy(HttpContext context, Endpoint? endpoint)
    {
        // Check for endpoint-specific rate limiting
        var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();
        if (rateLimitAttribute != null)
        {
            return new RateLimitPolicy
            {
                Limit = rateLimitAttribute.Limit,
                Window = rateLimitAttribute.Window,
                KeyGenerator = rateLimitAttribute.KeyGenerator
            };
        }

        // Default rate limiting based on path
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        return path switch
        {
            var p when p?.StartsWith("/api/v1/notifications") == true => _options.NotificationPolicy,
            var p when p?.StartsWith("/api/v1/templates") == true => _options.TemplatePolicy,
            var p when p?.StartsWith("/api/v1/campaigns") == true => _options.CampaignPolicy,
            _ => _options.DefaultPolicy
        };
    }

    private string GenerateKey(HttpContext context, RateLimitPolicy policy)
    {
        return policy.KeyGenerator switch
        {
            RateLimitKeyGenerator.IpAddress => $"ip:{context.Connection.RemoteIpAddress}",
            RateLimitKeyGenerator.User => $"user:{context.User.Identity?.Name ?? "anonymous"}",
            RateLimitKeyGenerator.IpAndUser => $"ip_user:{context.Connection.RemoteIpAddress}:{context.User.Identity?.Name ?? "anonymous"}",
            RateLimitKeyGenerator.ApiKey => $"apikey:{GetApiKey(context)}",
            _ => $"global:{context.Request.Path}"
        };
    }

    private string GetApiKey(HttpContext context)
    {
        return context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "unknown";
    }
}

/// <summary>
/// Rate limit policy configuration
/// </summary>
public class RateLimitPolicy
{
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
    public RateLimitKeyGenerator KeyGenerator { get; set; } = RateLimitKeyGenerator.IpAddress;
}

/// <summary>
/// Rate limit options
/// </summary>
public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public RateLimitPolicy DefaultPolicy { get; set; } = new() { Limit = 100, Window = TimeSpan.FromMinutes(1) };
    public RateLimitPolicy NotificationPolicy { get; set; } = new() { Limit = 50, Window = TimeSpan.FromMinutes(1) };
    public RateLimitPolicy TemplatePolicy { get; set; } = new() { Limit = 20, Window = TimeSpan.FromMinutes(1) };
    public RateLimitPolicy CampaignPolicy { get; set; } = new() { Limit = 10, Window = TimeSpan.FromMinutes(1) };
}

/// <summary>
/// Rate limit key generator types
/// </summary>
public enum RateLimitKeyGenerator
{
    IpAddress,
    User,
    IpAndUser,
    ApiKey,
    Global
}

/// <summary>
/// Rate limit attribute for endpoint-specific rate limiting
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute : Attribute
{
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
    public RateLimitKeyGenerator KeyGenerator { get; set; } = RateLimitKeyGenerator.IpAddress;

    public RateLimitAttribute(int limit, int windowMinutes)
    {
        Limit = limit;
        Window = TimeSpan.FromMinutes(windowMinutes);
    }
}

/// <summary>
/// Extension methods for rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new RateLimitOptions();
        configuration.GetSection("RateLimit").Bind(options);
        services.AddSingleton(options);
        
        services.AddSingleton<IRateLimitingService, RateLimitingService>();
        
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}