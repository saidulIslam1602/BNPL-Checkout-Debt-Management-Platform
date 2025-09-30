using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Notification.API.Controllers;

/// <summary>
/// Controller for health check endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
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

            return StatusCode(statusCode, ApiResponse<HealthStatus>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, ApiResponse<HealthStatus>.ErrorResult(new[] { ex.Message }.ToList()));
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