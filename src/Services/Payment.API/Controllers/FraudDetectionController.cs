using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for fraud detection and risk assessment operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FraudDetectionController : ControllerBase
{
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        IFraudDetectionService fraudDetectionService,
        ILogger<FraudDetectionController> logger)
    {
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Assesses fraud risk for a payment request
    /// </summary>
    /// <param name="request">Payment request to assess</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fraud assessment result</returns>
    [HttpPost("assess-payment")]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 400)]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 500)]
    public async Task<ActionResult<ApiResponse<FraudAssessmentResult>>> AssessPaymentRisk(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assessing fraud risk for payment request from customer {CustomerId}", request.CustomerId);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _fraudDetectionService.AssessPaymentRiskAsync(request, ipAddress, cancellationToken);
        
        var response = ApiResponse<FraudAssessmentResult>.SuccessResult(result, "Fraud assessment completed");
        return Ok(response);
    }

    /// <summary>
    /// Assesses overall risk level for a customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer risk assessment</returns>
    [HttpGet("assess-customer/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 404)]
    [ProducesResponseType(typeof(ApiResponse<FraudAssessmentResult>), 500)]
    public async Task<ActionResult<ApiResponse<FraudAssessmentResult>>> AssessCustomerRisk(
        [FromRoute] Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assessing customer risk for {CustomerId}", customerId);

        var result = await _fraudDetectionService.AssessCustomerRiskAsync(customerId, cancellationToken);
        var response = ApiResponse<FraudAssessmentResult>.SuccessResult(result, "Customer risk assessment completed");
        
        return Ok(response);
    }

    /// <summary>
    /// Reports fraudulent activity
    /// </summary>
    /// <param name="request">Fraud report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Report submission result</returns>
    [HttpPost("report")]
    [Authorize(Roles = "Admin,Merchant,FraudAnalyst")]
    [ProducesResponseType(typeof(ApiResponse), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ReportFraud(
        [FromBody] ReportFraudRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reporting fraudulent activity for customer {CustomerId}", request.CustomerId);

        var result = await _fraudDetectionService.ReportFraudulentActivityAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(ReportFraud), result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Updates fraud detection rules
    /// </summary>
    /// <param name="rules">New fraud rules</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("rules")]
    [Authorize(Roles = "Admin,FraudAnalyst")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> UpdateFraudRules(
        [FromBody] List<FraudRule> rules,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating fraud detection rules");

        var result = await _fraudDetectionService.UpdateFraudRulesAsync(rules, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
