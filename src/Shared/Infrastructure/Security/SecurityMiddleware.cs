using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RivertyBNPL.Shared.Infrastructure.Security;

/// <summary>
/// Comprehensive security middleware for Norwegian BNPL APIs
/// Implements rate limiting, request validation, and security headers
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly SecurityOptions _options;
    private readonly IRedisService _redisService;

    public SecurityMiddleware(
        RequestDelegate next,
        ILogger<SecurityMiddleware> logger,
        IOptions<SecurityOptions> options,
        IRedisService redisService)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _redisService = redisService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        try
        {
            // Add security headers
            AddSecurityHeaders(context);

            // Validate request size
            if (!ValidateRequestSize(context))
            {
                await WriteErrorResponseAsync(context, HttpStatusCode.RequestEntityTooLarge, 
                    "Request size exceeds maximum allowed limit", requestId);
                return;
            }

            // Rate limiting
            var rateLimitResult = await CheckRateLimitAsync(context);
            if (!rateLimitResult.IsAllowed)
            {
                await WriteRateLimitResponseAsync(context, rateLimitResult, requestId);
                return;
            }

            // Validate request signature for sensitive operations
            if (RequiresSignatureValidation(context))
            {
                var signatureValid = await ValidateRequestSignatureAsync(context);
                if (!signatureValid)
                {
                    await WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, 
                        "Invalid request signature", requestId);
                    return;
                }
            }

            // Check for suspicious patterns
            var suspiciousActivity = await DetectSuspiciousActivityAsync(context);
            if (suspiciousActivity.IsSuspicious)
            {
                _logger.LogWarning("Suspicious activity detected from {ClientIP}. Reason: {Reason}. RequestId: {RequestId}",
                    GetClientIP(context), suspiciousActivity.Reason, requestId);

                if (suspiciousActivity.ShouldBlock)
                {
                    await WriteErrorResponseAsync(context, HttpStatusCode.Forbidden, 
                        "Request blocked due to suspicious activity", requestId);
                    return;
                }
            }

            // Validate Norwegian-specific requirements
            if (IsNorwegianSpecificEndpoint(context))
            {
                var norwegianValidation = await ValidateNorwegianRequirementsAsync(context);
                if (!norwegianValidation.IsValid)
                {
                    await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, 
                        norwegianValidation.ErrorMessage, requestId);
                    return;
                }
            }

            // Log request for audit
            await LogRequestAsync(context, requestId);

            // Continue to next middleware
            await _next(context);

            // Log response for audit
            await LogResponseAsync(context, requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security middleware. RequestId: {RequestId}", requestId);
            
            if (!context.Response.HasStarted)
            {
                await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, 
                    "An error occurred while processing the request", requestId);
            }
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Norwegian GDPR compliance headers
        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("X-XSS-Protection", "1; mode=block");
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        
        // HSTS for HTTPS enforcement
        if (context.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // CSP for Norwegian financial services
        var csp = "default-src 'self'; " +
                 "script-src 'self' 'unsafe-inline' https://api.vipps.no https://api.bankid.no; " +
                 "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                 "font-src 'self' https://fonts.gstatic.com; " +
                 "img-src 'self' data: https:; " +
                 "connect-src 'self' https://api.riverty.com wss://api.riverty.com https://api.vipps.no https://api.bankid.no; " +
                 "frame-ancestors 'none'; " +
                 "base-uri 'self'; " +
                 "form-action 'self'";
        
        headers.Append("Content-Security-Policy", csp);

        // Norwegian privacy headers
        headers.Append("X-Privacy-Policy", "https://riverty.com/privacy-no");
        headers.Append("X-Data-Controller", "Riverty AS, Norway");
        headers.Append("X-GDPR-Compliant", "true");

        // API versioning and identification
        headers.Append("X-API-Version", "1.0");
        headers.Append("X-Service", "RivertyBNPL");
        headers.Append("X-Country", "NO");
    }

    private bool ValidateRequestSize(HttpContext context)
    {
        if (context.Request.ContentLength.HasValue)
        {
            return context.Request.ContentLength.Value <= _options.MaxRequestSizeBytes;
        }
        return true;
    }

    private async Task<RateLimitResult> CheckRateLimitAsync(HttpContext context)
    {
        var clientIP = GetClientIP(context);
        var endpoint = context.Request.Path.Value ?? "";
        var userId = GetUserId(context);

        // Different rate limits for different scenarios
        var rateLimitKey = GetRateLimitKey(clientIP, endpoint, userId);
        var rateLimitConfig = GetRateLimitConfig(endpoint);

        var currentCount = await _redisService.GetAsync<int?>($"rate_limit:{rateLimitKey}") ?? 0;
        
        if (currentCount >= rateLimitConfig.MaxRequests)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                CurrentCount = currentCount,
                MaxRequests = rateLimitConfig.MaxRequests,
                WindowSizeSeconds = rateLimitConfig.WindowSizeSeconds,
                RetryAfterSeconds = rateLimitConfig.WindowSizeSeconds
            };
        }

        // Increment counter
        var newCount = currentCount + 1;
        await _redisService.SetAsync($"rate_limit:{rateLimitKey}", newCount, 
            TimeSpan.FromSeconds(rateLimitConfig.WindowSizeSeconds));

        return new RateLimitResult
        {
            IsAllowed = true,
            CurrentCount = newCount,
            MaxRequests = rateLimitConfig.MaxRequests,
            WindowSizeSeconds = rateLimitConfig.WindowSizeSeconds
        };
    }

    private bool RequiresSignatureValidation(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Require signature validation for sensitive operations
        return path.Contains("/payments/") ||
               path.Contains("/settlements/") ||
               path.Contains("/refunds/") ||
               path.Contains("/webhooks/");
    }

    private async Task<bool> ValidateRequestSignatureAsync(HttpContext context)
    {
        try
        {
            var signature = context.Request.Headers["X-Signature"].FirstOrDefault();
            var timestamp = context.Request.Headers["X-Timestamp"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
            {
                return false;
            }

            // Check timestamp to prevent replay attacks
            if (!long.TryParse(timestamp, out var timestampLong))
            {
                return false;
            }

            var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampLong);
            var timeDifference = DateTimeOffset.UtcNow - requestTime;
            
            if (Math.Abs(timeDifference.TotalMinutes) > _options.SignatureValidityMinutes)
            {
                _logger.LogWarning("Request signature timestamp outside valid window. Timestamp: {Timestamp}, Difference: {Difference} minutes",
                    requestTime, timeDifference.TotalMinutes);
                return false;
            }

            // Read request body
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Create signature payload
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            var queryString = context.Request.QueryString.Value ?? "";
            var payload = $"{method}|{path}|{queryString}|{body}|{timestamp}";

            // Verify signature
            var expectedSignature = ComputeHMACSHA256(payload, _options.SignatureSecret);
            
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request signature");
            return false;
        }
    }

    private async Task<SuspiciousActivityResult> DetectSuspiciousActivityAsync(HttpContext context)
    {
        var clientIP = GetClientIP(context);
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";
        var path = context.Request.Path.Value ?? "";

        var suspiciousPatterns = new List<string>();

        // Check for suspicious user agents
        if (IsSuspiciousUserAgent(userAgent))
        {
            suspiciousPatterns.Add("Suspicious user agent");
        }

        // Check for rapid requests from same IP
        var requestCount = await GetRecentRequestCountAsync(clientIP);
        if (requestCount > _options.SuspiciousRequestThreshold)
        {
            suspiciousPatterns.Add($"High request frequency: {requestCount} requests");
        }

        // Check for SQL injection patterns
        if (ContainsSQLInjectionPatterns(context.Request.QueryString.Value))
        {
            suspiciousPatterns.Add("Potential SQL injection attempt");
        }

        // Check for XSS patterns
        if (ContainsXSSPatterns(context.Request.QueryString.Value))
        {
            suspiciousPatterns.Add("Potential XSS attempt");
        }

        // Check for Norwegian-specific suspicious patterns
        if (ContainsNorwegianSuspiciousPatterns(context))
        {
            suspiciousPatterns.Add("Norwegian-specific suspicious pattern detected");
        }

        var isSuspicious = suspiciousPatterns.Any();
        var shouldBlock = suspiciousPatterns.Count >= 2; // Block if multiple suspicious patterns

        if (isSuspicious)
        {
            // Log suspicious activity
            await LogSuspiciousActivityAsync(clientIP, userAgent, path, suspiciousPatterns);
        }

        return new SuspiciousActivityResult
        {
            IsSuspicious = isSuspicious,
            ShouldBlock = shouldBlock,
            Reason = string.Join(", ", suspiciousPatterns)
        };
    }

    private bool IsNorwegianSpecificEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        return path.Contains("/bnpl/") ||
               path.Contains("/vipps/") ||
               path.Contains("/bankid/") ||
               path.Contains("/norwegian/");
    }

    private async Task<NorwegianValidationResult> ValidateNorwegianRequirementsAsync(HttpContext context)
    {
        try
        {
            // Validate Norwegian phone number format if present
            var phoneNumber = context.Request.Headers["X-Phone-Number"].FirstOrDefault();
            if (!string.IsNullOrEmpty(phoneNumber) && !IsValidNorwegianPhoneNumber(phoneNumber))
            {
                return new NorwegianValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid Norwegian phone number format"
                };
            }

            // Validate Norwegian postal code if present
            var postalCode = context.Request.Headers["X-Postal-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(postalCode) && !IsValidNorwegianPostalCode(postalCode))
            {
                return new NorwegianValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid Norwegian postal code format"
                };
            }

            // Validate Norwegian organization number if present
            var orgNumber = context.Request.Headers["X-Org-Number"].FirstOrDefault();
            if (!string.IsNullOrEmpty(orgNumber) && !IsValidNorwegianOrgNumber(orgNumber))
            {
                return new NorwegianValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid Norwegian organization number format"
                };
            }

            return new NorwegianValidationResult { IsValid = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Norwegian requirements");
            return new NorwegianValidationResult
            {
                IsValid = false,
                ErrorMessage = "Error validating Norwegian requirements"
            };
        }
    }

    #region Helper Methods

    private string GetClientIP(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIP))
        {
            return xRealIP;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ??
               context.User?.FindFirst("user_id")?.Value ??
               context.Request.Headers["X-User-ID"].FirstOrDefault();
    }

    private string GetRateLimitKey(string clientIP, string endpoint, string? userId)
    {
        // Create composite key for rate limiting
        var keyParts = new List<string> { clientIP };
        
        if (!string.IsNullOrEmpty(userId))
        {
            keyParts.Add(userId);
        }
        
        // Normalize endpoint for rate limiting
        var normalizedEndpoint = NormalizeEndpointForRateLimit(endpoint);
        keyParts.Add(normalizedEndpoint);

        return string.Join(":", keyParts);
    }

    private RateLimitConfig GetRateLimitConfig(string endpoint)
    {
        var normalizedEndpoint = endpoint.ToLower();

        // Stricter limits for sensitive endpoints
        if (normalizedEndpoint.Contains("/payments/") || normalizedEndpoint.Contains("/bnpl/"))
        {
            return new RateLimitConfig
            {
                MaxRequests = _options.PaymentEndpointRateLimit,
                WindowSizeSeconds = 60
            };
        }

        if (normalizedEndpoint.Contains("/auth/") || normalizedEndpoint.Contains("/login/"))
        {
            return new RateLimitConfig
            {
                MaxRequests = _options.AuthEndpointRateLimit,
                WindowSizeSeconds = 300 // 5 minutes
            };
        }

        // Default rate limit
        return new RateLimitConfig
        {
            MaxRequests = _options.DefaultRateLimit,
            WindowSizeSeconds = 60
        };
    }

    private string NormalizeEndpointForRateLimit(string endpoint)
    {
        // Remove IDs and other variable parts for rate limiting
        return System.Text.RegularExpressions.Regex.Replace(endpoint, @"/[0-9a-fA-F-]{8,}", "/[id]");
    }

    private string ComputeHMACSHA256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python", "java",
            "scanner", "nikto", "sqlmap", "nmap", "masscan"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int> GetRecentRequestCountAsync(string clientIP)
    {
        var key = $"request_count:{clientIP}";
        var count = await _redisService.GetAsync<int?>(key) ?? 0;
        
        // Increment and set expiry
        await _redisService.SetAsync(key, count + 1, TimeSpan.FromMinutes(5));
        
        return count;
    }

    private bool ContainsSQLInjectionPatterns(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var sqlPatterns = new[]
        {
            "union select", "drop table", "insert into", "delete from",
            "update set", "exec(", "execute(", "sp_", "xp_", "'; --",
            "' or '1'='1", "' or 1=1", "admin'--", "' union select"
        };

        return sqlPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsXSSPatterns(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var xssPatterns = new[]
        {
            "<script", "javascript:", "onerror=", "onload=", "onclick=",
            "alert(", "document.cookie", "window.location", "eval("
        };

        return xssPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsNorwegianSuspiciousPatterns(HttpContext context)
    {
        // Check for attempts to access Norwegian-specific sensitive data
        var queryString = context.Request.QueryString.Value?.ToLower() ?? "";
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var norwegianSuspiciousPatterns = new[]
        {
            "personnummer", "fødselsnummer", "ssn", "national_id",
            "bankkontonummer", "account_number", "kredittkort", "credit_card"
        };

        return norwegianSuspiciousPatterns.Any(pattern => 
            queryString.Contains(pattern) || path.Contains(pattern));
    }

    private bool IsValidNorwegianPhoneNumber(string phoneNumber)
    {
        // Norwegian phone number validation: +47 followed by 8 digits
        return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\+47\d{8}$");
    }

    private bool IsValidNorwegianPostalCode(string postalCode)
    {
        // Norwegian postal code: 4 digits
        return System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{4}$");
    }

    private bool IsValidNorwegianOrgNumber(string orgNumber)
    {
        // Norwegian organization number: 9 digits with MOD-11 checksum
        if (!System.Text.RegularExpressions.Regex.IsMatch(orgNumber, @"^\d{9}$"))
            return false;

        // Implement MOD-11 validation for Norwegian org numbers
        var weights = new[] { 3, 2, 7, 6, 5, 4, 3, 2 };
        var sum = 0;

        for (int i = 0; i < 8; i++)
        {
            sum += (orgNumber[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        var checkDigit = remainder == 0 ? 0 : 11 - remainder;

        return checkDigit == (orgNumber[8] - '0');
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        if (_options.EnableRequestLogging)
        {
            _logger.LogInformation("Request: {Method} {Path} from {ClientIP}. RequestId: {RequestId}",
                context.Request.Method,
                context.Request.Path,
                GetClientIP(context),
                requestId);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string requestId)
    {
        if (_options.EnableResponseLogging)
        {
            _logger.LogInformation("Response: {StatusCode} for RequestId: {RequestId}",
                context.Response.StatusCode,
                requestId);
        }
    }

    private async Task LogSuspiciousActivityAsync(string clientIP, string userAgent, string path, List<string> patterns)
    {
        var suspiciousActivity = new
        {
            ClientIP = clientIP,
            UserAgent = userAgent,
            Path = path,
            Patterns = patterns,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogWarning("Suspicious activity detected: {@SuspiciousActivity}", suspiciousActivity);

        // Store in Redis for analysis
        var key = $"suspicious_activity:{clientIP}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        await _redisService.SetAsync(key, suspiciousActivity, TimeSpan.FromDays(7));
    }

    private async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string message, string requestId)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            Error = new
            {
                Code = statusCode.ToString(),
                Message = message,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private async Task WriteRateLimitResponseAsync(HttpContext context, RateLimitResult rateLimitResult, string requestId)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";
        context.Response.Headers.Append("Retry-After", rateLimitResult.RetryAfterSeconds.ToString());
        context.Response.Headers.Append("X-RateLimit-Limit", rateLimitResult.MaxRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, rateLimitResult.MaxRequests - rateLimitResult.CurrentCount).ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(rateLimitResult.RetryAfterSeconds).ToUnixTimeSeconds().ToString());

        var errorResponse = new
        {
            Error = new
            {
                Code = "RATE_LIMIT_EXCEEDED",
                Message = "Rate limit exceeded. Please try again later.",
                RequestId = requestId,
                RateLimit = new
                {
                    Limit = rateLimitResult.MaxRequests,
                    Remaining = Math.Max(0, rateLimitResult.MaxRequests - rateLimitResult.CurrentCount),
                    Reset = DateTimeOffset.UtcNow.AddSeconds(rateLimitResult.RetryAfterSeconds).ToUnixTimeSeconds(),
                    RetryAfter = rateLimitResult.RetryAfterSeconds
                }
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    #endregion
}

#region Models and Configuration

public class SecurityOptions
{
    public long MaxRequestSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int DefaultRateLimit { get; set; } = 100;
    public int PaymentEndpointRateLimit { get; set; } = 10;
    public int AuthEndpointRateLimit { get; set; } = 5;
    public int SuspiciousRequestThreshold { get; set; } = 50;
    public int SignatureValidityMinutes { get; set; } = 5;
    public string SignatureSecret { get; set; } = string.Empty;
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseLogging { get; set; } = false;
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int CurrentCount { get; set; }
    public int MaxRequests { get; set; }
    public int WindowSizeSeconds { get; set; }
    public int RetryAfterSeconds { get; set; }
}

public class RateLimitConfig
{
    public int MaxRequests { get; set; }
    public int WindowSizeSeconds { get; set; }
}

public class SuspiciousActivityResult
{
    public bool IsSuspicious { get; set; }
    public bool ShouldBlock { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class NorwegianValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion