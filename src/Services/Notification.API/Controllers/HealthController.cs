using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RivertyBNPL.Services.Notification.API.Services;
using RivertyBNPL.Shared.Common.Models;

namespace RivertyBNPL.Services.Notification.API.Controllers;

/// <summary>
/// Controller for health and monitoring endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly INotificationQueueService _queueService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        INotificationQueueService queueService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _queueService = queueService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthStatus>>> GetHealthAsync()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new HealthStatus
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Entries = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthEntry
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Data = kvp.Value.Data
                    })
            };

            var statusCode = healthReport.Status switch
            {
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => 200,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => 200,
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => 503,
                _ => 503
            };

            return StatusCode(statusCode, ApiResponse<HealthStatus>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, ApiResponse<HealthStatus>.Failure("Health check failed", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Detailed system status endpoint
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<SystemStatus>>> GetSystemStatusAsync()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var queueStats = await _queueService.GetQueueStatsAsync();

            var systemStatus = new SystemStatus
            {
                OverallStatus = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
                QueueStats = queueStats,
                HealthChecks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HealthEntry
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Data = kvp.Value.Data
                    })
            };

            return Ok(ApiResponse<SystemStatus>.Success(systemStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System status check failed");
            return StatusCode(500, ApiResponse<SystemStatus>.Failure("System status check failed", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Queue statistics endpoint
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetQueueStatsAsync()
    {
        try
        {
            var stats = await _queueService.GetQueueStatsAsync();
            return Ok(ApiResponse<Dictionary<string, int>>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue statistics");
            return StatusCode(500, ApiResponse<Dictionary<string, int>>.Failure("Failed to get queue statistics", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// Readiness probe endpoint
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadinessAsync()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            if (healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            {
                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            else
            {
                return StatusCode(503, new { status = "not_ready", timestamp = DateTime.UtcNow });
            }
        }
        catch
        {
            return StatusCode(503, new { status = "not_ready", timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Liveness probe endpoint
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLivenessAsync()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

/// <summary>
/// Health status response model
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, HealthEntry> Entries { get; set; } = new();
}

/// <summary>
/// Health entry model
/// </summary>
public class HealthEntry
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}

/// <summary>
/// System status response model
/// </summary>
public class SystemStatus
{
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, int> QueueStats { get; set; } = new();
    public Dictionary<string, HealthEntry> HealthChecks { get; set; } = new();
}