using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for payment method tokenization operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentTokensController : ControllerBase
{
    private readonly IPaymentTokenizationService _tokenizationService;
    private readonly ILogger<PaymentTokensController> _logger;

    public PaymentTokensController(
        IPaymentTokenizationService tokenizationService,
        ILogger<PaymentTokensController> logger)
    {
        _tokenizationService = tokenizationService;
        _logger = logger;
    }

    /// <summary>
    /// Tokenizes a payment method for secure storage
    /// </summary>
    /// <param name="request">Tokenization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment token details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentTokenResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTokenResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTokenResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentTokenResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentTokenResponse>>> TokenizePaymentMethod(
        [FromBody] TokenizePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tokenizing payment method for customer {CustomerId}", request.CustomerId);

        var result = await _tokenizationService.TokenizePaymentMethodAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(GetPaymentMethod), new { token = result.Data?.Token }, result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets payment method details by token
    /// </summary>
    /// <param name="token">Payment token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment method details</returns>
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDetails>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDetails>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentMethodDetails>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentMethodDetails>>> GetPaymentMethod(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        var result = await _tokenizationService.GetPaymentMethodAsync(token, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Deletes a tokenized payment method
    /// </summary>
    /// <param name="token">Payment token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{token}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> DeletePaymentMethod(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting payment method token {Token}", token);

        var result = await _tokenizationService.DeletePaymentMethodAsync(token, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Gets customer's tokenized payment methods
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated payment methods</returns>
    [HttpGet("customers/{customerId:guid}")]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentTokenResponse>), 200)]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentTokenResponse>), 400)]
    [ProducesResponseType(typeof(PagedApiResponse<PaymentTokenResponse>), 500)]
    public async Task<ActionResult<PagedApiResponse<PaymentTokenResponse>>> GetCustomerPaymentMethods(
        [FromRoute] Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new PagedApiResponse<PaymentTokenResponse>
            {
                Success = false,
                Errors = new List<string> { "Page must be greater than 0" },
                StatusCode = 400
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new PagedApiResponse<PaymentTokenResponse>
            {
                Success = false,
                Errors = new List<string> { "PageSize must be between 1 and 100" },
                StatusCode = 400
            });
        }

        var result = await _tokenizationService.GetCustomerPaymentMethodsAsync(customerId, page, pageSize, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Processes a payment using a tokenized payment method
    /// </summary>
    /// <param name="request">Tokenized payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment result</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 400)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), 500)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> ProcessTokenizedPayment(
        [FromBody] ProcessTokenizedPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing tokenized payment with token {Token}", request.PaymentToken);

        var result = await _tokenizationService.ProcessTokenizedPaymentAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction("GetPayment", "Payments", new { id = result.Data?.Id }, result)
            : StatusCode(result.StatusCode, result);
    }
}