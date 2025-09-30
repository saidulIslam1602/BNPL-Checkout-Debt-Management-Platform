using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for payment operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new payment
    /// </summary>
    /// <param name="request">Payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating payment for customer {CustomerId}, merchant {MerchantId}, amount {Amount}",
            request.CustomerId, request.MerchantId, request.Amount);

        var result = await _paymentService.CreatePaymentAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(GetPayment), new { id = result.Data?.Id }, result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes a pending payment
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment details</returns>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> ProcessPayment(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment {PaymentId}", id);

        var result = await _paymentService.ProcessPaymentAsync(id, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets payment by ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> GetPayment(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.GetPaymentAsync(id, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Searches payments with filtering and pagination
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated payment results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentResponse>), 200)]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentResponse>), 400)]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<PagedApiResponse<PaymentResponse>>> SearchPayments(
        [FromQuery] PaymentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.SearchPaymentsAsync(request, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Cancels a pending payment
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment details</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> CancelPayment(
        [FromRoute] Guid id,
        [FromBody] [Required] string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payment {PaymentId} with reason: {Reason}", id, reason);

        var result = await _paymentService.CancelPaymentAsync(id, reason, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a refund for a completed payment
    /// </summary>
    /// <param name="request">Refund creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund details</returns>
    [HttpPost("refunds")]
    [ProducesResponseType(typeof(ApiResponse<RefundResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<RefundResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<RefundResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<RefundResponse>), 500)]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> CreateRefund(
        [FromBody] CreateRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating refund for payment {PaymentId}, amount {Amount}",
            request.PaymentId, request.Amount);

        var result = await _paymentService.CreateRefundAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(GetPayment), new { id = request.PaymentId }, result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets payment analytics for a merchant
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="fromDate">Start date for analytics</param>
    /// <param name="toDate">End date for analytics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment analytics</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<PaymentAnalytics>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentAnalytics>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentAnalytics>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentAnalytics>>> GetPaymentAnalytics(
        [FromQuery] [Required] Guid merchantId,
        [FromQuery] [Required] DateTime fromDate,
        [FromQuery] [Required] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate >= toDate)
        {
            return BadRequest(ApiResponse<PaymentAnalytics>.ErrorResult("FromDate must be before ToDate", 400));
        }

        if ((toDate - fromDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse<PaymentAnalytics>.ErrorResult("Date range cannot exceed 365 days", 400));
        }

        var result = await _paymentService.GetPaymentAnalyticsAsync(merchantId, fromDate, toDate, cancellationToken);
        
        return StatusCode(result.StatusCode, result);
    }
}