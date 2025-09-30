using YourCompanyBNPL.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Payment.API.Services;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Controller for webhook operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Receives webhook notifications from payment providers
    /// </summary>
    /// <param name="provider">Payment provider name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> ReceiveWebhook(
        [FromRoute] string provider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get signature from headers
            var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? 
                           Request.Headers["X-Hub-Signature-256"].FirstOrDefault() ?? 
                           Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest(ApiResponse.ErrorResult("Missing webhook signature", 400));
            }

            // Read the raw body
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(payload))
            {
                return BadRequest(ApiResponse.ErrorResult("Empty webhook payload", 400));
            }

            _logger.LogInformation("Received webhook from provider {Provider}", provider);

            var result = await _webhookService.ProcessWebhookAsync(provider, signature, payload, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from provider {Provider}", provider);
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Registers a webhook endpoint for a merchant
    /// </summary>
    /// <param name="request">Webhook registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [HttpPost("endpoints")]
    [Authorize(Roles = "Admin,Merchant")]
    [ProducesResponseType(typeof(ApiResponse), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> RegisterWebhookEndpoint(
        [FromBody] RegisterWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering webhook endpoint for merchant {MerchantId}", request.MerchantId);

        var result = await _webhookService.RegisterWebhookEndpointAsync(request, cancellationToken);
        
        return result.Success 
            ? CreatedAtAction(nameof(RegisterWebhookEndpoint), result)
            : StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Sends a webhook notification for a payment event
    /// </summary>
    /// <param name="request">Webhook send request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> SendWebhook(
        [FromBody] SendWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending webhook for payment {PaymentId}", request.PaymentId);

        var result = await _webhookService.SendWebhookAsync(request.PaymentId, request.EventType, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Retries failed webhook deliveries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Retry result</returns>
    [HttpPost("retry-failed")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> RetryFailedWebhooks(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying failed webhook deliveries");

        var result = await _webhookService.RetryFailedWebhooksAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

/// <summary>
/// Request for sending a webhook
/// </summary>
public class SendWebhookRequest
{
    [Required]
    public Guid PaymentId { get; set; }
    
    [Required]
    public Common.Enums.WebhookEventType EventType { get; set; }
}