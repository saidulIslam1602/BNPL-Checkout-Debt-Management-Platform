using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YourCompanyBNPL.Shared.Infrastructure.Security;

namespace YourCompanyBNPL.Shared.Infrastructure.Idempotency;

/// <summary>
/// Idempotency service for Norwegian BNPL operations
/// Ensures payment operations are safe to retry and prevents duplicate processing
/// </summary>
public interface IIdempotencyService
{
    Task<IdempotencyResult<T>> ExecuteAsync<T>(string idempotencyKey, Func<Task<T>> operation, TimeSpan? expiry = null);
    Task<IdempotencyResult<T>> ExecuteAsync<T>(string idempotencyKey, Func<Task<T>> operation, IdempotencyOptions options);
    Task<bool> IsProcessedAsync(string idempotencyKey);
    Task<T> GetResultAsync<T>(string idempotencyKey);
    Task InvalidateAsync(string idempotencyKey);
    Task<string> GenerateKeyAsync(object request, string? customerId = null);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly ILogger<IdempotencyService> _logger;
    private readonly IRedisService _redisService;
    private readonly IdempotencyConfiguration _config;

    public IdempotencyService(
        ILogger<IdempotencyService> logger,
        IRedisService redisService,
        IOptions<IdempotencyConfiguration> config)
    {
        _logger = logger;
        _redisService = redisService;
        _config = config.Value;
    }

    public async Task<IdempotencyResult<T>> ExecuteAsync<T>(string idempotencyKey, Func<Task<T>> operation, TimeSpan? expiry = null)
    {
        var options = new IdempotencyOptions
        {
            Expiry = expiry ?? TimeSpan.FromHours(_config.DefaultExpiryHours),
            RetryOnFailure = _config.RetryOnFailure,
            MaxRetries = _config.MaxRetries,
            RetryDelay = TimeSpan.FromSeconds(_config.RetryDelaySeconds)
        };

        return await ExecuteAsync(idempotencyKey, operation, options);
    }

    public async Task<IdempotencyResult<T>> ExecuteAsync<T>(string idempotencyKey, Func<Task<T>> operation, IdempotencyOptions options)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));
        }

        var normalizedKey = NormalizeKey(idempotencyKey);
        var lockKey = $"lock:{normalizedKey}";
        var resultKey = $"result:{normalizedKey}";
        var statusKey = $"status:{normalizedKey}";

        _logger.LogDebug("Executing idempotent operation with key: {IdempotencyKey}", normalizedKey);

        try
        {
            // Check if operation was already completed
            var existingResult = await GetExistingResultAsync<T>(resultKey, statusKey);
            if (existingResult != null)
            {
                _logger.LogInformation("Returning cached result for idempotency key: {IdempotencyKey}", normalizedKey);
                return existingResult;
            }

            // Acquire distributed lock to prevent concurrent execution
            var lockAcquired = await AcquireLockAsync(lockKey, options.LockTimeout);
            if (!lockAcquired)
            {
                _logger.LogWarning("Failed to acquire lock for idempotency key: {IdempotencyKey}", normalizedKey);
                
                // Wait and check if another process completed the operation
                await Task.Delay(options.RetryDelay);
                var resultAfterWait = await GetExistingResultAsync<T>(resultKey, statusKey);
                if (resultAfterWait != null)
                {
                    return resultAfterWait;
                }

                return new IdempotencyResult<T>
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to acquire lock for idempotent operation",
                    Status = IdempotencyStatus.LOCKED
                };
            }

            try
            {
                // Double-check after acquiring lock
                var doubleCheckResult = await GetExistingResultAsync<T>(resultKey, statusKey);
                if (doubleCheckResult != null)
                {
                    return doubleCheckResult;
                }

                // Mark operation as in progress
                await SetOperationStatusAsync(statusKey, IdempotencyStatus.IN_PROGRESS, options.Expiry);

                // Execute the operation with retry logic
                var result = await ExecuteWithRetryAsync(operation, options, normalizedKey);

                // Store the result
                var idempotencyResult = new IdempotencyResult<T>
                {
                    IsSuccess = true,
                    Result = result,
                    Status = IdempotencyStatus.COMPLETED,
                    CompletedAt = DateTime.UtcNow,
                    IdempotencyKey = normalizedKey
                };

                await StoreResultAsync(resultKey, statusKey, idempotencyResult, options.Expiry);

                _logger.LogInformation("Idempotent operation completed successfully for key: {IdempotencyKey}", normalizedKey);
                return idempotencyResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Idempotent operation failed for key: {IdempotencyKey}", normalizedKey);

                // Store failure result
                var failureResult = new IdempotencyResult<T>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Status = IdempotencyStatus.FAILED,
                    CompletedAt = DateTime.UtcNow,
                    IdempotencyKey = normalizedKey
                };

                if (options.StoreFailures)
                {
                    await StoreResultAsync(resultKey, statusKey, failureResult, options.FailureExpiry ?? options.Expiry);
                }
                else
                {
                    await SetOperationStatusAsync(statusKey, IdempotencyStatus.FAILED, TimeSpan.FromMinutes(5));
                }

                return failureResult;
            }
            finally
            {
                // Release the lock
                await ReleaseLockAsync(lockKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in idempotency service for key: {IdempotencyKey}", normalizedKey);
            throw;
        }
    }

    public async Task<bool> IsProcessedAsync(string idempotencyKey)
    {
        var normalizedKey = NormalizeKey(idempotencyKey);
        var statusKey = $"status:{normalizedKey}";
        
        var status = await _redisService.GetAsync<IdempotencyStatus?>(statusKey);
        return status == IdempotencyStatus.COMPLETED;
    }

    public async Task<T> GetResultAsync<T>(string idempotencyKey)
    {
        var normalizedKey = NormalizeKey(idempotencyKey);
        var resultKey = $"result:{normalizedKey}";
        
        var result = await _redisService.GetAsync<IdempotencyResult<T>>(resultKey);
        if (result != null && result.Result != null)
        {
            return result.Result;
        }
        
        #pragma warning disable CS8603
        return default;
        #pragma warning restore CS8603
    }

    public async Task InvalidateAsync(string idempotencyKey)
    {
        var normalizedKey = NormalizeKey(idempotencyKey);
        var resultKey = $"result:{normalizedKey}";
        var statusKey = $"status:{normalizedKey}";
        var lockKey = $"lock:{normalizedKey}";

        await Task.WhenAll(
            _redisService.DeleteAsync(resultKey),
            _redisService.DeleteAsync(statusKey),
            _redisService.DeleteAsync(lockKey)
        );

        _logger.LogInformation("Invalidated idempotency key: {IdempotencyKey}", normalizedKey);
    }

    public async Task<string> GenerateKeyAsync(object request, string? customerId = null)
    {
        try
        {
            // Serialize request to JSON for consistent hashing
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Create hash of the request
            using var sha256 = SHA256.Create();
            var requestHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(requestJson)));

            // Include customer ID for additional uniqueness
            var keyComponents = new List<string>
            {
                request.GetType().Name,
                requestHash[..16] // First 16 characters of hash for brevity
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                keyComponents.Add(customerId);
            }

            // Add timestamp component for time-sensitive operations
            if (_config.IncludeTimestamp)
            {
                var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHH");
                keyComponents.Add(timestamp);
            }

            var key = string.Join(":", keyComponents);
            
            _logger.LogDebug("Generated idempotency key: {IdempotencyKey} for request type: {RequestType}", 
                key, request.GetType().Name);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating idempotency key for request type: {RequestType}", 
                request.GetType().Name);
            throw;
        }
    }

    #region Private Methods

    private string NormalizeKey(string key)
    {
        // Add prefix to avoid key collisions
        return $"{_config.KeyPrefix}:{key}";
    }

    private async Task<IdempotencyResult<T>?> GetExistingResultAsync<T>(string resultKey, string statusKey)
    {
        var status = await _redisService.GetAsync<IdempotencyStatus?>(statusKey);
        
        if (status == IdempotencyStatus.COMPLETED)
        {
            var result = await _redisService.GetAsync<IdempotencyResult<T>>(resultKey);
            if (result != null)
            {
                result.Status = IdempotencyStatus.COMPLETED;
                return result;
            }
        }
        else if (status == IdempotencyStatus.FAILED)
        {
            var failedResult = await _redisService.GetAsync<IdempotencyResult<T>>(resultKey);
            if (failedResult != null)
            {
                return failedResult;
            }
        }
        else if (status == IdempotencyStatus.IN_PROGRESS)
        {
            return new IdempotencyResult<T>
            {
                IsSuccess = false,
                Status = IdempotencyStatus.IN_PROGRESS,
                ErrorMessage = "Operation is currently in progress"
            };
        }

        return null;
    }

    private async Task<bool> AcquireLockAsync(string lockKey, TimeSpan timeout)
    {
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _redisService.SetIfNotExistsAsync(lockKey, lockValue, timeout);
        
        if (acquired)
        {
            _logger.LogDebug("Acquired lock: {LockKey}", lockKey);
        }
        else
        {
            _logger.LogDebug("Failed to acquire lock: {LockKey}", lockKey);
        }

        return acquired;
    }

    private async Task ReleaseLockAsync(string lockKey)
    {
        await _redisService.DeleteAsync(lockKey);
        _logger.LogDebug("Released lock: {LockKey}", lockKey);
    }

    private async Task SetOperationStatusAsync(string statusKey, IdempotencyStatus status, TimeSpan expiry)
    {
        await _redisService.SetAsync(statusKey, status, expiry);
    }

    private async Task StoreResultAsync<T>(string resultKey, string statusKey, IdempotencyResult<T> result, TimeSpan expiry)
    {
        await Task.WhenAll(
            _redisService.SetAsync(resultKey, result, expiry),
            _redisService.SetAsync(statusKey, result.Status, expiry)
        );
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, IdempotencyOptions options, string key)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < options.MaxRetries)
        {
            try
            {
                attempts++;
                _logger.LogDebug("Executing operation attempt {Attempt}/{MaxRetries} for key: {IdempotencyKey}", 
                    attempts, options.MaxRetries, key);

                return await operation();
            }
            catch (Exception ex) when (options.RetryOnFailure && IsRetryableException(ex) && attempts < options.MaxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Operation attempt {Attempt} failed for key: {IdempotencyKey}. Retrying in {Delay}ms", 
                    attempts, key, options.RetryDelay.TotalMilliseconds);

                await Task.Delay(options.RetryDelay);
            }
        }

        // If we get here, all retries failed
        throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts");
    }

    private static bool IsRetryableException(Exception ex)
    {
        // Define which exceptions are retryable
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               (ex is InvalidOperationException && ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}

#region Models and Configuration

public class IdempotencyConfiguration
{
    public string KeyPrefix { get; set; } = "idempotency";
    public int DefaultExpiryHours { get; set; } = 24;
    public bool RetryOnFailure { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public bool IncludeTimestamp { get; set; } = false;
    public bool StoreFailures { get; set; } = true;
}

public class IdempotencyOptions
{
    public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan? FailureExpiry { get; set; }
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool RetryOnFailure { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public bool StoreFailures { get; set; } = true;
}

public class IdempotencyResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public IdempotencyStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum IdempotencyStatus
{
    PENDING,
    IN_PROGRESS,
    COMPLETED,
    FAILED,
    LOCKED,
    EXPIRED
}

#endregion

#region Redis Service Extension

public static class RedisServiceExtensions
{
    public static async Task<bool> SetIfNotExistsAsync<T>(this IRedisService redisService, string key, T value, TimeSpan expiry)
    {
        // This would be implemented based on your Redis client
        // For example, using StackExchange.Redis: SET key value EX seconds NX
        try
        {
            var existing = await redisService.GetAsync<T>(key);
            if (existing != null)
                return false;

            await redisService.SetAsync(key, value, expiry);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

#endregion