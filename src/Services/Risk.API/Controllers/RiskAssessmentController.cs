using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Risk.API.Controllers;

/// <summary>
/// Controller for risk assessment operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class RiskAssessmentController : ControllerBase
{
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ILogger<RiskAssessmentController> _logger;

    public RiskAssessmentController(
        IRiskAssessmentService riskAssessmentService,
        ILogger<RiskAssessmentController> logger)
    {
        _riskAssessmentService = riskAssessmentService;
        _logger = logger;
    }

    /// <summary>
    /// Performs comprehensive credit assessment for BNPL eligibility
    /// </summary>
    /// <param name="request">Credit assessment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credit assessment result</returns>
    [HttpPost("assess")]
    [ProducesResponseType(typeof(ApiResponse<CreditAssessmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CreditAssessmentResponse>>> AssessCreditRisk(
        [FromBody] CreditAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting credit assessment for customer {CustomerId}", request.CustomerId);

            var result = await _riskAssessmentService.AssessCreditRiskAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Credit assessment completed for customer {CustomerId}. Approved: {IsApproved}, Score: {CreditScore}",
                    request.CustomerId, result.Data?.IsApproved, result.Data?.CreditScore);
                return Ok(result);
            }

            _logger.LogWarning("Credit assessment failed for customer {CustomerId}: {Error}", request.CustomerId, result.Message);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credit assessment for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred during credit assessment", 500));
        }
    }

    /// <summary>
    /// Gets existing credit assessment by ID
    /// </summary>
    /// <param name="assessmentId">Assessment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credit assessment details</returns>
    [HttpGet("{assessmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CreditAssessmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CreditAssessmentResponse>>> GetCreditAssessment(
        [FromRoute] Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving credit assessment {AssessmentId}", assessmentId);

            var result = await _riskAssessmentService.GetCreditAssessmentAsync(assessmentId, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit assessment {AssessmentId}", assessmentId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving credit assessment", 500));
        }
    }

    /// <summary>
    /// Gets customer's credit assessments with pagination
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of credit assessments</returns>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(PagedApiResponse<CreditAssessmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<PagedApiResponse<CreditAssessmentResponse>>> GetCustomerAssessments(
        [FromRoute] Guid customerId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving credit assessments for customer {CustomerId}, Page: {Page}, PageSize: {PageSize}",
                customerId, page, pageSize);

            var result = await _riskAssessmentService.GetCustomerAssessmentsAsync(customerId, page, pageSize, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit assessments for customer {CustomerId}", customerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving customer assessments", 500));
        }
    }

    /// <summary>
    /// Searches credit assessments with filtering
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedApiResponse<CreditAssessmentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<PagedApiResponse<CreditAssessmentResponse>>> SearchAssessments(
        [FromBody] RiskAssessmentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching credit assessments with criteria: {Criteria}", 
                System.Text.Json.JsonSerializer.Serialize(request));

            var result = await _riskAssessmentService.SearchAssessmentsAsync(request, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching credit assessments");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while searching assessments", 500));
        }
    }

    /// <summary>
    /// Gets customer risk profile
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer risk profile</returns>
    [HttpGet("profile/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerRiskProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CustomerRiskProfileResponse>>> GetRiskProfile(
        [FromRoute] Guid customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving risk profile for customer {CustomerId}", customerId);

            var result = await _riskAssessmentService.GetRiskProfileAsync(customerId, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk profile for customer {CustomerId}", customerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving risk profile", 500));
        }
    }

    /// <summary>
    /// Updates customer risk profile based on payment behavior
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated risk profile</returns>
    [HttpPut("profile/{customerId:guid}/update")]
    [ProducesResponseType(typeof(ApiResponse<CustomerRiskProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<CustomerRiskProfileResponse>>> UpdateRiskProfile(
        [FromRoute] Guid customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating risk profile for customer {CustomerId}", customerId);

            var result = await _riskAssessmentService.UpdateRiskProfileAsync(customerId, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Risk profile updated for customer {CustomerId}. New risk level: {RiskLevel}",
                    customerId, result.Data?.CurrentRiskLevel);
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk profile for customer {CustomerId}", customerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while updating risk profile", 500));
        }
    }

    /// <summary>
    /// Gets risk analytics for reporting
    /// </summary>
    /// <param name="fromDate">Start date for analytics</param>
    /// <param name="toDate">End date for analytics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risk analytics data</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<RiskAnalytics>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<RiskAnalytics>>> GetRiskAnalytics(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fromDate >= toDate)
            {
                return BadRequest(ApiResponse.ErrorResult("FromDate must be earlier than ToDate", 400));
            }

            if ((toDate - fromDate).TotalDays > 365)
            {
                return BadRequest(ApiResponse.ErrorResult("Date range cannot exceed 365 days", 400));
            }

            _logger.LogInformation("Retrieving risk analytics from {FromDate} to {ToDate}", fromDate, toDate);

            var result = await _riskAssessmentService.GetRiskAnalyticsAsync(fromDate, toDate, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk analytics from {FromDate} to {ToDate}", fromDate, toDate);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving risk analytics", 500));
        }
    }
}