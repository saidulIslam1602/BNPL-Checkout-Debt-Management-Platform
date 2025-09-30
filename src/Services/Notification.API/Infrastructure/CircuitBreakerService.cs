using YourCompanyBNPL.Common.Enums;
using Polly;

namespace YourCompanyBNPL.Notification.API.Infrastructure;

public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> operation, CancellationToken cancellationToken = default);
}

public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // Simple implementation - can be enhanced with Polly later
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing operation for service {ServiceName}", serviceName);
            throw;
        }
    }
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
}