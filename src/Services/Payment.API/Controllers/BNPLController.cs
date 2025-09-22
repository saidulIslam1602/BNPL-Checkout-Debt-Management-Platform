using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RivertyBNPL.Payment.API.Services;
using RivertyBNPL.Payment.API.DTOs;
using RivertyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace RivertyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for BNPL (Buy Now, Pay Later) operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BNPLController : ControllerBase
{
    private readonly IBNPLService _bnplService;
    private readonly ILogger<BNPLController> _logger;

    public BNPLController(IBNPLService bnplService, ILogger<BNPLController> logger)
    {
        _bnplService = bnplService;
        _logger = logger;
    }

    /// <summary>
    /// Calculates BNPL payment plan options
    /// </summary>
    /// <param name="request">BNPL calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BNPL calculation results</returns>
    [HttpPost("calculate")]
    [AllowAnonymous] // Allow anonymous access for checkout calculations
    [ProducesResponseType(typeof(ApiResponse<BNPLCalculationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<BNPLCalculationResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<BNPLCalculationResponse>), 500)]
    public async Task<ActionResult<ApiResponse<BNPLCalculationResponse>>> CalculateBNPLPlan(
        [FromBody] BNPLCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating BNPL plan for amount {Amount}, plan type {PlanType}",
            request.Amount, request.PlanType);

        var result = await _bnplService.CalculateBNPLPlanAsync(request, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a BNPL payment plan for an existing payment
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="request">BNPL plan creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created BNPL plan details</returns>
    [HttpPost("payments/{paymentId:guid}/plans")]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 201)]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 400)]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 404)]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 500)]
    public async Task<ActionResult<ApiResponse<BNPLPlanSummary>>> CreateBNPLPlan(
        [FromRoute] Guid paymentId,
        [FromBody] BNPLCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating BNPL plan for payment {PaymentId}", paymentId);

        var result = await _bnplService.CreateBNPLPlanAsync(paymentId, request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(GetBNPLPlan), new { id = result.Data?.Id }, result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets BNPL plan by ID
    /// </summary>
    /// <param name="id">BNPL plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BNPL plan details</returns>
    [HttpGet("plans/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 200)]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 404)]
    [ProducesResponseType(typeof(ApiResponse<BNPLPlanSummary>), 500)]
    public async Task<ActionResult<ApiResponse<BNPLPlanSummary>>> GetBNPLPlan(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _bnplService.GetBNPLPlanAsync(id, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets customer's BNPL plans
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated BNPL plans</returns>
    [HttpGet("customers/{customerId:guid}/plans")]
    [ProducesResponseType(typeof(PagedApiResponse<BNPLPlanSummary>), 200)]
    [ProducesResponseType(typeof(PagedApiResponse<BNPLPlanSummary>), 400)]
    [ProducesResponseType(typeof(PagedApiResponse<BNPLPlanSummary>), 500)]
    public async Task<ActionResult<PagedApiResponse<BNPLPlanSummary>>> GetCustomerBNPLPlans(
        [FromRoute] Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new PagedApiResponse<BNPLPlanSummary>
            {
                Success = false,
                Errors = new List<string> { "Page must be greater than 0" },
                StatusCode = 400
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new PagedApiResponse<BNPLPlanSummary>
            {
                Success = false,
                Errors = new List<string> { "PageSize must be between 1 and 100" },
                StatusCode = 400
            });
        }

        var result = await _bnplService.GetCustomerBNPLPlansAsync(customerId, page, pageSize, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes an installment payment
    /// </summary>
    /// <param name="request">Installment processing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated installment details</returns>
    [HttpPost("installments/process")]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<InstallmentResponse>>> ProcessInstallment(
        [FromBody] ProcessInstallmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing installment {InstallmentId}", request.InstallmentId);

        var result = await _bnplService.ProcessInstallmentAsync(request, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets overdue installments for collection processing
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated overdue installments</returns>
    [HttpGet("installments/overdue")]
    [Authorize(Roles = "Admin,CollectionAgent")]
    [ProducesResponseType(typeof(PagedApiResponse<InstallmentResponse>), 200)]
    [ProducesResponseType(typeof(PagedApiResponse<InstallmentResponse>), 400)]
    [ProducesResponseType(typeof(PagedApiResponse<InstallmentResponse>), 500)]
    public async Task<ActionResult<PagedApiResponse<InstallmentResponse>>> GetOverdueInstallments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new PagedApiResponse<InstallmentResponse>
            {
                Success = false,
                Errors = new List<string> { "Page must be greater than 0" },
                StatusCode = 400
            });
        }

        if (pageSize < 1 || pageSize > 500)
        {
            return BadRequest(new PagedApiResponse<InstallmentResponse>
            {
                Success = false,
                Errors = new List<string> { "PageSize must be between 1 and 500" },
                StatusCode = 400
            });
        }

        var result = await _bnplService.GetOverdueInstallmentsAsync(page, pageSize, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes overdue installments (marks as overdue and calculates late fees)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    [HttpPost("installments/process-overdue")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ProcessOverdueInstallments(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing overdue installments");

        var result = await _bnplService.ProcessOverdueInstallmentsAsync(cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets installment by ID
    /// </summary>
    /// <param name="id">Installment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Installment details</returns>
    [HttpGet("installments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<InstallmentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<InstallmentResponse>>> GetInstallment(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _bnplService.GetInstallmentAsync(id, cancellationToken);
        
        if (!result.Success)
        {
            return result.StatusCode switch
            {
                404 => NotFound(result),
                _ => StatusCode(result.StatusCode, result)
            };
        }

        return Ok(result);
    }
}