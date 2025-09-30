using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace YourCompanyBNPL.Risk.API.Infrastructure;

/// <summary>
/// Service for rate limiting API requests
/// </summary>
public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limiting information
/// </summary>
public class RateLimitInfo
{
    public bool IsAllowed { get; set; }
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
    public DateTime WindowStart { get; set; }
    public TimeSpan TimeUntilReset { get; set; }
}

/// <summary>
/// In-memory rate limiting service implementation
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var info = await GetRateLimitInfoAsync(key, limit, window, cancellationToken);
        return info.IsAllowed;
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cacheKey = $"rate_limit_{key}";

        var rateLimitData = _cache.Get<RateLimitData>(cacheKey);

        if (rateLimitData == null || now >= rateLimitData.WindowStart.Add(window))
        {
            // New window or first request
            rateLimitData = new RateLimitData
            {
                RequestCount = 1,
                WindowStart = now
            };

            _cache.Set(cacheKey, rateLimitData, window);

            return new RateLimitInfo
            {
                IsAllowed = true,
                RequestCount = 1,
                Limit = limit,
                Window = window,
                WindowStart = rateLimitData.WindowStart,
                TimeUntilReset = window
            };
        }

        // Within current window
        rateLimitData.RequestCount++;
        _cache.Set(cacheKey, rateLimitData, rateLimitData.WindowStart.Add(window) - now);

        var isAllowed = rateLimitData.RequestCount <= limit;
        var timeUntilReset = rateLimitData.WindowStart.Add(window) - now;

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}", 
                key, rateLimitData.RequestCount, limit);
        }

        await Task.CompletedTask;

        return new RateLimitInfo
        {
            IsAllowed = isAllowed,
            RequestCount = rateLimitData.RequestCount,
            Limit = limit,
            Window = window,
            WindowStart = rateLimitData.WindowStart,
            TimeUntilReset = timeUntilReset
        };
    }

    private class RateLimitData
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

/// <summary>
/// Rate limiting middleware
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimitingService = rateLimitingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        var key = $"{clientId}:{endpoint}";

        var (limit, window) = GetRateLimitConfig(endpoint);
        var rateLimitInfo = await _rateLimitingService.GetRateLimitInfoAsync(key, limit, window);

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", Math.Max(0, limit - rateLimitInfo.RequestCount).ToString());
        context.Response.Headers.Add("X-RateLimit-Reset", ((DateTimeOffset)rateLimitInfo.WindowStart.Add(rateLimitInfo.Window)).ToUnixTimeSeconds().ToString());

        if (!rateLimitInfo.IsAllowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Add("Retry-After", ((int)rateLimitInfo.TimeUntilReset.TotalSeconds).ToString());

            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get client ID from JWT claims first
        var clientId = context.User?.FindFirst("client_id")?.Value;
        if (!string.IsNullOrEmpty(clientId))
            return clientId;

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        return "anonymous";
    }

    private static string GetEndpointIdentifier(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        return path switch
        {
            var p when p.Contains("/riskassessment/assess") => "risk-assessment",
            var p when p.Contains("/frauddetection/detect") => "fraud-detection",
            var p when p.Contains("/creditbureau/credit-report") => "credit-bureau",
            var p when p.Contains("/modelmanagement/predict") => "ml-prediction",
            _ => "general"
        };
    }

    private (int limit, TimeSpan window) GetRateLimitConfig(string endpoint)
    {
        var defaultLimit = _configuration.GetValue<int>("RateLimiting:DefaultRateLimit", 100);
        var defaultWindow = TimeSpan.FromSeconds(_configuration.GetValue<int>("RateLimiting:DefaultTimeWindow", 60));

        return endpoint switch
        {
            "risk-assessment" => (_configuration.GetValue<int>("RateLimiting:RiskAssessmentRateLimit", 50), defaultWindow),
            "fraud-detection" => (_configuration.GetValue<int>("RateLimiting:FraudDetectionRateLimit", 200), defaultWindow),
            "credit-bureau" => (20, defaultWindow), // Lower limit for external API calls
            "ml-prediction" => (100, defaultWindow),
            _ => (defaultLimit, defaultWindow)
        };
    }
}