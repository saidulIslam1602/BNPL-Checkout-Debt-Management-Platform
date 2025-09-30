using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.Infrastructure;
using YourCompanyBNPL.Common.Models;
using System.Reflection;
using System.Diagnostics;

namespace YourCompanyBNPL.Risk.API.Controllers;

/// <summary>
/// Health check and monitoring controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ICircuitBreakerService circuitBreakerService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _circuitBreakerService = circuitBreakerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets basic health status
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatus>), 200)]
    public async Task<ActionResult<ApiResponse<HealthStatus>>> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var healthStatus = new HealthStatus
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Checks = healthReport.Entries.Select(entry => new HealthCheck
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration,
                    Description = entry.Value.Description,
                    Data = entry.Value.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""),
                    Exception = entry.Value.Exception?.Message
                }).ToList()
            };

            var statusCode = healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? 200 : 503;
            
            return StatusCode(statusCode, ApiResponse<HealthStatus>.SuccessResult(healthStatus, "Health check completed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return StatusCode(503, ApiResponse.ErrorResult("Health check failed", 503));
        }
    }

    /// <summary>
    /// Gets detailed system information
    /// </summary>
    /// <returns>System information</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<SystemInfo>), 200)]
    public ActionResult<ApiResponse<SystemInfo>> GetSystemInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var buildDate = GetBuildDate(assembly);

            var systemInfo = new SystemInfo
            {
                ServiceName = "Risk Assessment API",
                Version = version,
                BuildDate = buildDate,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                StartTime = Process.GetCurrentProcess().StartTime,
                WorkingSet = Environment.WorkingSet,
                GCMemory = GC.GetTotalMemory(false),
                ThreadCount = Process.GetCurrentProcess().Threads.Count,
                UpTime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
            };

            return Ok(ApiResponse<SystemInfo>.SuccessResult(systemInfo, "System information retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system information");
            return StatusCode(500, ApiResponse.ErrorResult("Failed to retrieve system information", 500));
        }
    }

    /// <summary>
    /// Gets circuit breaker status
    /// </summary>
    /// <returns>Circuit breaker status</returns>
    [HttpGet("circuit-breakers")]
    [ProducesResponseType(typeof(ApiResponse<List<CircuitBreakerStatus>>), 200)]
    public ActionResult<ApiResponse<List<CircuitBreakerStatus>>> GetCircuitBreakerStatus()
    {
        try
        {
            var circuitNames = new[] { "credit-bureau", "ml-service", "database", "service-bus" };
            var circuitStatuses = circuitNames.Select(name => new CircuitBreakerStatus
            {
                Name = name,
                State = _circuitBreakerService.GetState(name).ToString(),
                LastChecked = DateTime.UtcNow
            }).ToList();

            return Ok(ApiResponse<List<CircuitBreakerStatus>>.SuccessResult(circuitStatuses, "Circuit breaker status retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving circuit breaker status");
            return StatusCode(500, ApiResponse.ErrorResult("Failed to retrieve circuit breaker status", 500));
        }
    }

    /// <summary>
    /// Resets a specific circuit breaker
    /// </summary>
    /// <param name="circuitName">Name of the circuit breaker to reset</param>
    /// <returns>Reset result</returns>
    [HttpPost("circuit-breakers/{circuitName}/reset")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public ActionResult<ApiResponse> ResetCircuitBreaker([FromRoute] string circuitName)
    {
        try
        {
            var validCircuits = new[] { "credit-bureau", "ml-service", "database", "service-bus" };
            
            if (!validCircuits.Contains(circuitName, StringComparer.OrdinalIgnoreCase))
            {
                return NotFound(ApiResponse.ErrorResult($"Circuit breaker '{circuitName}' not found", 404));
            }

            _circuitBreakerService.Reset(circuitName);
            
            _logger.LogInformation("Circuit breaker {CircuitName} reset by user", circuitName);
            
            return Ok(ApiResponse.SuccessResponse($"Circuit breaker '{circuitName}' has been reset"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting circuit breaker {CircuitName}", circuitName);
            return StatusCode(500, ApiResponse.ErrorResult("Failed to reset circuit breaker", 500));
        }
    }

    /// <summary>
    /// Gets performance metrics
    /// </summary>
    /// <returns>Performance metrics</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceMetrics>), 200)]
    public ActionResult<ApiResponse<PerformanceMetrics>> GetMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            var metrics = new PerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = new MemoryMetrics
                {
                    WorkingSet = process.WorkingSet64,
                    PrivateMemory = process.PrivateMemorySize64,
                    VirtualMemory = process.VirtualMemorySize64,
                    GCMemory = GC.GetTotalMemory(false),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                },
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                UpTime = DateTime.UtcNow - process.StartTime
            };

            return Ok(ApiResponse<PerformanceMetrics>.SuccessResult(metrics, "Performance metrics retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, ApiResponse.ErrorResult("Failed to retrieve performance metrics", 500));
        }
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        try
        {
            var attribute = assembly.GetCustomAttribute<System.Reflection.AssemblyMetadataAttribute>();
            if (attribute?.Key == "BuildDate" && DateTime.TryParse(attribute.Value, out var buildDate))
            {
                return buildDate;
            }
        }
        catch
        {
            // Ignore errors
        }

        // Fallback to file creation time
        try
        {
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
            {
                return System.IO.File.GetCreationTimeUtc(location);
            }
        }
        catch
        {
            // Ignore errors
        }

        return DateTime.MinValue;
    }

    private static double GetCpuUsage()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            Thread.Sleep(500); // Sample for 500ms
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Round(cpuUsageTotal * 100, 2);
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Health status information
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public List<HealthCheck> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check information
/// </summary>
public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Data { get; set; }
    public string? Exception { get; set; }
}

/// <summary>
/// System information
/// </summary>
public class SystemInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public DateTime StartTime { get; set; }
    public long WorkingSet { get; set; }
    public long GCMemory { get; set; }
    public int ThreadCount { get; set; }
    public TimeSpan UpTime { get; set; }
}

/// <summary>
/// Circuit breaker status
/// </summary>
public class CircuitBreakerStatus
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public MemoryMetrics MemoryUsage { get; set; } = new();
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public TimeSpan UpTime { get; set; }
}

/// <summary>
/// Memory metrics
/// </summary>
public class MemoryMetrics
{
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public long GCMemory { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}