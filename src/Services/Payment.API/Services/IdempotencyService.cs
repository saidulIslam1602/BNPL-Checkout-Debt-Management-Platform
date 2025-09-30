using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for handling idempotent payment operations
/// </summary>
public interface IIdempotencyService
{
    Task<T?> GetIdempotentResultAsync<T>(string idempotencyKey, CancellationToken cancellationToken = default) where T : class;
    Task StoreIdempotentResultAsync<T>(string idempotencyKey, T result, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> IsIdempotentRequestAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    string GenerateIdempotencyKey(object request);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(PaymentDbContext context, ILogger<IdempotencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T?> GetIdempotentResultAsync<T>(string idempotencyKey, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var record = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (record == null)
            {
                return null;
            }

            _logger.LogInformation("Found idempotent result for key {IdempotencyKey}", idempotencyKey);
            return JsonSerializer.Deserialize<T>(record.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving idempotent result for key {IdempotencyKey}", idempotencyKey);
            return null;
        }
    }

    public async Task StoreIdempotentResultAsync<T>(string idempotencyKey, T result, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var expiryTime = expiry ?? TimeSpan.FromHours(24);
            var responseData = JsonSerializer.Serialize(result);

            var record = new IdempotencyRecord
            {
                IdempotencyKey = idempotencyKey,
                ResponseData = responseData,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiryTime)
            };

            _context.IdempotencyRecords.Add(record);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Stored idempotent result for key {IdempotencyKey}", idempotencyKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing idempotent result for key {IdempotencyKey}", idempotencyKey);
        }
    }

    public async Task<bool> IsIdempotentRequestAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.IdempotencyRecords
                .AnyAsync(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency for key {IdempotencyKey}", idempotencyKey);
            return false;
        }
    }

    public string GenerateIdempotencyKey(object request)
    {
        var json = JsonSerializer.Serialize(request);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Middleware for handling idempotent requests
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        // Only apply to POST requests
        if (context.Request.Method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        // Check for idempotency key header
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || 
            string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var key = idempotencyKey.ToString();

        // Check if we have a cached result
        var cachedResult = await idempotencyService.GetIdempotentResultAsync<object>(key);
        if (cachedResult != null)
        {
            _logger.LogInformation("Returning cached result for idempotency key {IdempotencyKey}", key);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            
            var json = JsonSerializer.Serialize(cachedResult);
            await context.Response.WriteAsync(json);
            return;
        }

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Store the result if successful
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            
            if (!string.IsNullOrEmpty(responseText))
            {
                var result = JsonSerializer.Deserialize<object>(responseText);
                if (result != null)
                {
                    await idempotencyService.StoreIdempotentResultAsync(key, result);
                }
            }
        }

        // Copy the response back to the original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}