using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for enhanced settlement operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettlementsController : ControllerBase
{
    private readonly IEnhancedSettlementService _settlementService;
    private readonly ILogger<SettlementsController> _logger;

    public SettlementsController(
        IEnhancedSettlementService settlementService,
        ILogger<SettlementsController> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new settlement batch
    /// </summary>
    /// <param name="request">Settlement batch creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created settlement batch details</returns>
    [HttpPost("batches")]
    [Authorize(Roles = "Admin,Merchant")]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 500)]
    public async Task<ActionResult<ApiResponse<SettlementBatchResponse>>> CreateSettlementBatch(
        [FromBody] CreateSettlementBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating settlement batch for merchant {MerchantId}", request.MerchantId);

        var result = await _settlementService.CreateSettlementBatchAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(GetSettlementSummary), new { id = result.Data?.Id }, result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes a settlement batch
    /// </summary>
    /// <param name="batchId">Settlement batch ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    [HttpPost("batches/{batchId:guid}/process")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<SettlementBatchResponse>), 500)]
    public async Task<ActionResult<ApiResponse<SettlementBatchResponse>>> ProcessSettlementBatch(
        [FromRoute] Guid batchId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing settlement batch {BatchId}", batchId);

        var result = await _settlementService.ProcessSettlementBatchAsync(batchId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets settlement summary by ID
    /// </summary>
    /// <param name="id">Settlement ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement summary</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SettlementSummary>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SettlementSummary>), 404)]
    [ProducesResponseType(typeof(ApiResponse<SettlementSummary>), 500)]
    public async Task<ActionResult<ApiResponse<SettlementSummary>>> GetSettlementSummary(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _settlementService.GetSettlementSummaryAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets merchant settlements with filtering and pagination
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated settlement results</returns>
    [HttpGet("merchants/{merchantId:guid}")]
    [ProducesResponseType(typeof(PagedApiResponse<SettlementSummary>), 200)]
    [ProducesResponseType(typeof(PagedApiResponse<SettlementSummary>), 400)]
    [ProducesResponseType(typeof(PagedApiResponse<SettlementSummary>), 500)]
    public async Task<ActionResult<PagedApiResponse<SettlementSummary>>> GetMerchantSettlements(
        [FromRoute] Guid merchantId,
        [FromQuery] SettlementSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _settlementService.GetMerchantSettlementsAsync(merchantId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Generates a settlement report for a merchant
    /// </summary>
    /// <param name="request">Report generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement report</returns>
    [HttpPost("reports")]
    [Authorize(Roles = "Admin,Merchant")]
    [ProducesResponseType(typeof(ApiResponse<SettlementReportResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SettlementReportResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SettlementReportResponse>), 500)]
    public async Task<ActionResult<ApiResponse<SettlementReportResponse>>> GenerateSettlementReport(
        [FromBody] SettlementReportRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating settlement report for merchant {MerchantId}", request.MerchantId);

        if (request.FromDate >= request.ToDate)
        {
            return BadRequest(ApiResponse<SettlementReportResponse>.ErrorResult("FromDate must be before ToDate", 400));
        }

        if ((request.ToDate - request.FromDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse<SettlementReportResponse>.ErrorResult("Date range cannot exceed 365 days", 400));
        }

        var result = await _settlementService.GenerateSettlementReportAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes automatic settlements for eligible merchants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    [HttpPost("process-automatic")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ProcessAutomaticSettlements(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing automatic settlements");

        var result = await _settlementService.ProcessAutomaticSettlementsAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets settlement forecast for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="days">Number of days to forecast (default: 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement forecast</returns>
    [HttpGet("merchants/{merchantId:guid}/forecast")]
    [Authorize(Roles = "Admin,Merchant")]
    [ProducesResponseType(typeof(ApiResponse<SettlementForecastResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SettlementForecastResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<SettlementForecastResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<SettlementForecastResponse>), 500)]
    public async Task<ActionResult<ApiResponse<SettlementForecastResponse>>> GetSettlementForecast(
        [FromRoute] Guid merchantId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 1 || days > 365)
        {
            return BadRequest(ApiResponse<SettlementForecastResponse>.ErrorResult("Days must be between 1 and 365", 400));
        }

        var result = await _settlementService.GetSettlementForecastAsync(merchantId, days, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}