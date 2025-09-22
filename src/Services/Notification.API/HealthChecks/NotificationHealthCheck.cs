using Microsoft.Extensions.Diagnostics.HealthChecks;
using RivertyBNPL.Services.Notification.API.Data;
using RivertyBNPL.Services.Notification.API.Providers;

namespace RivertyBNPL.Services.Notification.API.HealthChecks;

/// <summary>
/// Health check for notification service components
/// </summary>
public class NotificationHealthCheck : IHealthCheck
{
    private readonly NotificationDbContext _context;
    private readonly INotificationProviderFactory _providerFactory;
    private readonly ILogger<NotificationHealthCheck> _logger;

    public NotificationHealthCheck(
        NotificationDbContext context,
        INotificationProviderFactory providerFactory,
        ILogger<NotificationHealthCheck> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var isHealthy = true;
        var issues = new List<string>();

        try
        {
            // Check database connectivity
            var canConnectToDatabase = await _context.Database.CanConnectAsync(cancellationToken);
            healthData["database"] = canConnectToDatabase ? "healthy" : "unhealthy";
            
            if (!canConnectToDatabase)
            {
                isHealthy = false;
                issues.Add("Cannot connect to database");
            }

            // Check notification providers
            var providers = _providerFactory.GetAllProviders();
            var providerHealth = new Dictionary<string, bool>();

            foreach (var provider in providers)
            {
                try
                {
                    var providerHealthy = await provider.IsHealthyAsync(cancellationToken);
                    providerHealth[provider.Channel.ToString()] = providerHealthy;
                    
                    if (!providerHealthy)
                    {
                        issues.Add($"{provider.Channel} provider is unhealthy");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for {Channel} provider", provider.Channel);
                    providerHealth[provider.Channel.ToString()] = false;
                    issues.Add($"{provider.Channel} provider health check failed: {ex.Message}");
                }
            }

            healthData["providers"] = providerHealth;

            // Check queue status (basic check)
            try
            {
                var queueCount = _context.NotificationQueue.Count(q => q.Status == Models.NotificationQueueStatus.Pending);
                healthData["pending_queue_items"] = queueCount;
                
                // Alert if queue is backing up (more than 1000 pending items)
                if (queueCount > 1000)
                {
                    issues.Add($"High number of pending queue items: {queueCount}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check queue status");
                issues.Add("Queue status check failed");
            }

            // Check recent notification success rate
            try
            {
                var recentTime = DateTime.UtcNow.AddHours(-1);
                var recentNotifications = _context.Notifications
                    .Where(n => n.CreatedAt >= recentTime)
                    .ToList();

                if (recentNotifications.Any())
                {
                    var successRate = (double)recentNotifications.Count(n => n.Status == Models.NotificationStatus.Sent || n.Status == Models.NotificationStatus.Delivered) 
                                    / recentNotifications.Count * 100;
                    
                    healthData["recent_success_rate"] = Math.Round(successRate, 2);
                    
                    // Alert if success rate is below 80%
                    if (successRate < 80)
                    {
                        issues.Add($"Low recent success rate: {successRate:F1}%");
                    }
                }
                else
                {
                    healthData["recent_success_rate"] = "no_recent_notifications";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check recent notification success rate");
                issues.Add("Success rate check failed");
            }

            healthData["last_check"] = DateTime.UtcNow;

            if (isHealthy && !issues.Any())
            {
                return HealthCheckResult.Healthy("All notification service components are healthy", healthData);
            }
            else if (issues.Count == 1 && issues[0].Contains("success rate"))
            {
                // Degraded if only success rate is low
                return HealthCheckResult.Degraded($"Notification service is degraded: {string.Join(", ", issues)}", null, healthData);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Notification service issues detected: {string.Join(", ", issues)}", null, healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex, healthData);
        }
    }
}

/// <summary>
/// Health check for individual notification providers
/// </summary>
public class NotificationProviderHealthCheck : IHealthCheck
{
    private readonly INotificationProviderFactory _providerFactory;
    private readonly ILogger<NotificationProviderHealthCheck> _logger;

    public NotificationProviderHealthCheck(
        INotificationProviderFactory providerFactory,
        ILogger<NotificationProviderHealthCheck> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var issues = new List<string>();

        try
        {
            var providers = _providerFactory.GetAllProviders();
            
            foreach (var provider in providers)
            {
                try
                {
                    var isHealthy = await provider.IsHealthyAsync(cancellationToken);
                    healthData[provider.Channel.ToString().ToLower()] = new
                    {
                        status = isHealthy ? "healthy" : "unhealthy",
                        last_check = DateTime.UtcNow
                    };

                    if (!isHealthy)
                    {
                        issues.Add($"{provider.Channel} provider is not healthy");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed for {Channel} provider", provider.Channel);
                    healthData[provider.Channel.ToString().ToLower()] = new
                    {
                        status = "error",
                        error = ex.Message,
                        last_check = DateTime.UtcNow
                    };
                    issues.Add($"{provider.Channel} provider health check failed");
                }
            }

            if (!issues.Any())
            {
                return HealthCheckResult.Healthy("All notification providers are healthy", healthData);
            }
            else
            {
                return HealthCheckResult.Degraded($"Some notification providers are unhealthy: {string.Join(", ", issues)}", null, healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider health check failed with exception");
            return HealthCheckResult.Unhealthy($"Provider health check failed: {ex.Message}", ex, healthData);
        }
    }
}