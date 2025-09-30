using System.Collections.Concurrent;

namespace YourCompanyBNPL.Risk.API.Infrastructure;

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Circuit breaker service interface
/// </summary>
public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> operation, CancellationToken cancellationToken = default);
    CircuitBreakerState GetState(string circuitName);
    void Reset(string circuitName);
}

/// <summary>
/// Circuit breaker implementation
/// </summary>
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly IConfiguration _configuration;
    private readonly ILogger<CircuitBreakerService> _logger;

    public CircuitBreakerService(IConfiguration configuration, ILogger<CircuitBreakerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string circuitName, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var circuitBreaker = _circuitBreakers.GetOrAdd(circuitName, name => CreateCircuitBreaker(name));
        return await circuitBreaker.ExecuteAsync(operation, cancellationToken);
    }

    public CircuitBreakerState GetState(string circuitName)
    {
        return _circuitBreakers.TryGetValue(circuitName, out var circuitBreaker) 
            ? circuitBreaker.State 
            : CircuitBreakerState.Closed;
    }

    public void Reset(string circuitName)
    {
        if (_circuitBreakers.TryGetValue(circuitName, out var circuitBreaker))
        {
            circuitBreaker.Reset();
            _logger.LogInformation("Circuit breaker {CircuitName} has been reset", circuitName);
        }
    }

    private CircuitBreaker CreateCircuitBreaker(string name)
    {
        var failureThreshold = _configuration.GetValue<int>("CircuitBreaker:FailureThreshold", 5);
        var recoveryTime = TimeSpan.FromSeconds(_configuration.GetValue<int>("CircuitBreaker:RecoveryTimeSeconds", 30));
        var timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("CircuitBreaker:TimeoutSeconds", 10));

        return new CircuitBreaker(name, failureThreshold, recoveryTime, timeout, _logger);
    }
}

/// <summary>
/// Circuit breaker implementation
/// </summary>
internal class CircuitBreaker
{
    private readonly string _name;
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryTime;
    private readonly TimeSpan _timeout;
    private readonly ILogger _logger;
    private readonly object _lock = new();

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;

    public CircuitBreaker(string name, int failureThreshold, TimeSpan recoveryTime, TimeSpan timeout, ILogger logger)
    {
        _name = name;
        _failureThreshold = failureThreshold;
        _recoveryTime = recoveryTime;
        _timeout = timeout;
        _logger = logger;
    }

    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime >= _recoveryTime)
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _logger.LogInformation("Circuit breaker {CircuitName} moved to HalfOpen state", _name);
                }
                else
                {
                    _logger.LogWarning("Circuit breaker {CircuitName} is Open, rejecting request", _name);
                    throw new CircuitBreakerOpenException($"Circuit breaker {_name} is open");
                }
            }
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var result = await operation().ConfigureAwait(false);

            OnSuccess();
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate cancellation
        }
        catch (OperationCanceledException)
        {
            OnFailure(new TimeoutException($"Operation timed out after {_timeout.TotalSeconds} seconds"));
            throw new TimeoutException($"Circuit breaker {_name}: Operation timed out");
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _lastFailureTime = DateTime.MinValue;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _logger.LogInformation("Circuit breaker {CircuitName} moved to Closed state after successful operation", _name);
            }
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _logger.LogError(exception, "Circuit breaker {CircuitName} moved to Open state after {FailureCount} failures", 
                    _name, _failureCount);
            }
            else
            {
                _logger.LogWarning(exception, "Circuit breaker {CircuitName} recorded failure {FailureCount}/{FailureThreshold}", 
                    _name, _failureCount, _failureThreshold);
            }
        }
    }
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}