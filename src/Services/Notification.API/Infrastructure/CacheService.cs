using YourCompanyBNPL.Common.Enums;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace YourCompanyBNPL.Notification.API.Infrastructure;

/// <summary>
/// Cache service interface
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis-based cache service implementation
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheOptions _options;

    public CacheService(
        IDistributedCache distributedCache, 
        ILogger<CacheService> logger,
        CacheOptions options)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(_options.DefaultExpiry);
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cached value for key: {Key} with expiry: {Expiry}", key, expiry ?? _options.DefaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: This is a simplified implementation
            // In production, you might want to use Redis SCAN command for better performance
            _logger.LogDebug("Removing cached values by pattern: {Pattern}", pattern);
            
            // For now, we'll just log the pattern removal
            // A full Redis implementation would require additional Redis-specific code
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached values by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if key exists: {Key}", key);
            return false;
        }
    }
}

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TemplateExpiry { get; set; } = TimeSpan.FromHours(2);
    public TimeSpan PreferenceExpiry { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AnalyticsExpiry { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Cache key generator for consistent key naming
/// </summary>
public static class CacheKeys
{
    private const string PREFIX = "notification_api";
    
    public static string Template(Guid templateId) => $"{PREFIX}:template:{templateId}";
    public static string TemplateByName(string name, string language) => $"{PREFIX}:template:name:{name}:{language}";
    public static string Templates(string? type = null, string? language = null, bool? isActive = null) 
        => $"{PREFIX}:templates:{type ?? "all"}:{language ?? "all"}:{isActive?.ToString() ?? "all"}";
    
    public static string Preference(Guid customerId) => $"{PREFIX}:preference:{customerId}";
    public static string OptIn(Guid customerId, string notificationType, string channel) 
        => $"{PREFIX}:optin:{customerId}:{notificationType}:{channel}";
    
    public static string Analytics(DateTime fromDate, DateTime toDate) 
        => $"{PREFIX}:analytics:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
    
    public static string Campaign(Guid campaignId) => $"{PREFIX}:campaign:{campaignId}";
    public static string Campaigns() => $"{PREFIX}:campaigns";
    
    public static string NotificationStats() => $"{PREFIX}:notification_stats";
}

/// <summary>
/// Extension methods for cache service registration
/// </summary>
public static class CacheServiceExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = new CacheOptions();
        configuration.GetSection("Cache").Bind(cacheOptions);
        services.AddSingleton(cacheOptions);

        // Add Redis distributed cache
        var connectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddDistributedMemoryCache(); // Fallback to memory cache
            // TODO: Add Redis when package is available
            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration = connectionString;
            //     options.InstanceName = "NotificationAPI";
            // });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, CacheService>();
        
        return services;
    }
}