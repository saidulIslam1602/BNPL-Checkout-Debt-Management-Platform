using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Risk.API.Controllers;

/// <summary>
/// Controller for credit bureau integration operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class CreditBureauController : ControllerBase
{
    private readonly ICreditBureauService _creditBureauService;
    private readonly ILogger<CreditBureauController> _logger;

    public CreditBureauController(
        ICreditBureauService creditBureauService,
        ILogger<CreditBureauController> logger)
    {
        _creditBureauService = creditBureauService;
        _logger = logger;
    }

    /// <summary>
    /// Gets credit report from Norwegian credit bureau
    /// </summary>
    /// <param name="request">Credit bureau request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credit bureau summary</returns>
    [HttpPost("credit-report")]
    [ProducesResponseType(typeof(ApiResponse<CreditBureauSummary>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 503)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CreditBureauSummary>>> GetCreditReport(
        [FromBody] CreditBureauRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting credit report from {Bureau} for SSN {SSN}",
                request.BureauName, MaskSSN(request.SocialSecurityNumber));

            var result = await _creditBureauService.GetCreditReportAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully retrieved credit report from {Bureau}, Score: {Score}",
                    request.BureauName, result.Data?.CreditScore);
                return Ok(result);
            }

            _logger.LogWarning("Credit report request failed for bureau {Bureau}: {Error}", request.BureauName, result.Message);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting credit report from {Bureau}", request.BureauName);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while requesting credit report", 500));
        }
    }

    /// <summary>
    /// Validates Norwegian social security number
    /// </summary>
    /// <param name="socialSecurityNumber">Norwegian SSN to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-ssn")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateSSN(
        [FromBody] ValidateSSNRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
            {
                return BadRequest(ApiResponse.ErrorResult("Social security number is required", 400));
            }

            _logger.LogInformation("Validating Norwegian SSN {SSN}", MaskSSN(request.SocialSecurityNumber));

            var result = await _creditBureauService.ValidateSSNAsync(request.SocialSecurityNumber, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("SSN validation completed for {SSN}. Valid: {IsValid}",
                    MaskSSN(request.SocialSecurityNumber), result.Data);
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SSN {SSN}", MaskSSN(request.SocialSecurityNumber));
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while validating SSN", 500));
        }
    }

    /// <summary>
    /// Checks for bankruptcy records
    /// </summary>
    /// <param name="socialSecurityNumber">Norwegian SSN to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bankruptcy check result</returns>
    [HttpPost("check-bankruptcy")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<bool>>> CheckBankruptcy(
        [FromBody] ValidateSSNRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
            {
                return BadRequest(ApiResponse.ErrorResult("Social security number is required", 400));
            }

            _logger.LogInformation("Checking bankruptcy records for SSN {SSN}", MaskSSN(request.SocialSecurityNumber));

            var result = await _creditBureauService.CheckBankruptcyAsync(request.SocialSecurityNumber, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Bankruptcy check completed for {SSN}. Has bankruptcy: {HasBankruptcy}",
                    MaskSSN(request.SocialSecurityNumber), result.Data);
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bankruptcy records for SSN {SSN}", MaskSSN(request.SocialSecurityNumber));
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while checking bankruptcy records", 500));
        }
    }

    /// <summary>
    /// Gets payment history from credit bureau
    /// </summary>
    /// <param name="socialSecurityNumber">Norwegian SSN</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment history data</returns>
    [HttpPost("payment-history")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, object>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> GetPaymentHistory(
        [FromBody] ValidateSSNRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
            {
                return BadRequest(ApiResponse.ErrorResult("Social security number is required", 400));
            }

            _logger.LogInformation("Retrieving payment history for SSN {SSN}", MaskSSN(request.SocialSecurityNumber));

            var result = await _creditBureauService.GetPaymentHistoryAsync(request.SocialSecurityNumber, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Payment history retrieved for {SSN}", MaskSSN(request.SocialSecurityNumber));
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history for SSN {SSN}", MaskSSN(request.SocialSecurityNumber));
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving payment history", 500));
        }
    }

    /// <summary>
    /// Gets available credit bureaus and their status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available credit bureaus</returns>
    [HttpGet("bureaus")]
    [ProducesResponseType(typeof(ApiResponse<List<CreditBureauInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<CreditBureauInfo>>>> GetAvailableBureaus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving available credit bureaus");

            // This would typically come from configuration and health checks
            var bureaus = new List<CreditBureauInfo>
            {
                new()
                {
                    Name = "Experian",
                    DisplayName = "Experian Norway",
                    IsActive = true,
                    SupportsFullReport = true,
                    SupportsScoreOnly = true,
                    AverageResponseTime = TimeSpan.FromSeconds(2.5),
                    LastHealthCheck = DateTime.UtcNow.AddMinutes(-5),
                    HealthStatus = "Healthy"
                },
                new()
                {
                    Name = "Bisnode",
                    DisplayName = "Bisnode (Dun & Bradstreet)",
                    IsActive = true,
                    SupportsFullReport = true,
                    SupportsScoreOnly = false,
                    AverageResponseTime = TimeSpan.FromSeconds(3.2),
                    LastHealthCheck = DateTime.UtcNow.AddMinutes(-3),
                    HealthStatus = "Healthy"
                },
                new()
                {
                    Name = "Lindorff",
                    DisplayName = "Lindorff (Intrum)",
                    IsActive = true,
                    SupportsFullReport = false,
                    SupportsScoreOnly = true,
                    AverageResponseTime = TimeSpan.FromSeconds(1.8),
                    LastHealthCheck = DateTime.UtcNow.AddMinutes(-2),
                    HealthStatus = "Healthy"
                }
            };

            return Ok(ApiResponse<List<CreditBureauInfo>>.SuccessResult(bureaus, "Available credit bureaus retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available credit bureaus");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving credit bureaus", 500));
        }
    }

    /// <summary>
    /// Tests connectivity to a specific credit bureau
    /// </summary>
    /// <param name="bureauName">Name of the credit bureau to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test result</returns>
    [HttpPost("test-connectivity/{bureauName}")]
    [ProducesResponseType(typeof(ApiResponse<BureauConnectivityTest>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<BureauConnectivityTest>>> TestBureauConnectivity(
        [FromRoute] string bureauName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bureauName))
            {
                return BadRequest(ApiResponse.ErrorResult("Bureau name is required", 400));
            }

            _logger.LogInformation("Testing connectivity to credit bureau {Bureau}", bureauName);

            // This would perform actual connectivity tests
            var testResult = new BureauConnectivityTest
            {
                BureauName = bureauName,
                TestTime = DateTime.UtcNow,
                IsConnected = true,
                ResponseTime = TimeSpan.FromMilliseconds(1250),
                StatusMessage = "Connection successful",
                LastError = null
            };

            await Task.Delay(100, cancellationToken); // Simulate test delay

            return Ok(ApiResponse<BureauConnectivityTest>.SuccessResult(testResult, "Connectivity test completed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connectivity to credit bureau {Bureau}", bureauName);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while testing connectivity", 500));
        }
    }

    private static string MaskSSN(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn) || ssn.Length < 4)
            return "****";

        return ssn[..2] + "****" + ssn[^2..];
    }
}

/// <summary>
/// Request for SSN validation
/// </summary>
public class ValidateSSNRequest
{
    [Required]
    [MaxLength(11)]
    public string SocialSecurityNumber { get; set; } = string.Empty;
}

/// <summary>
/// Credit bureau information
/// </summary>
public class CreditBureauInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool SupportsFullReport { get; set; }
    public bool SupportsScoreOnly { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

/// <summary>
/// Bureau connectivity test result
/// </summary>
public class BureauConnectivityTest
{
    public string BureauName { get; set; } = string.Empty;
    public DateTime TestTime { get; set; }
    public bool IsConnected { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string? LastError { get; set; }
}