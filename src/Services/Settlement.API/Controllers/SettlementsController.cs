using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourCompanyBNPL.Settlement.API.DTOs;
using YourCompanyBNPL.Settlement.API.Services;

namespace YourCompanyBNPL.Settlement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<SettlementsController> _logger;

    public SettlementsController(
        ISettlementService settlementService,
        ILogger<SettlementsController> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new settlement transaction
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SettlementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettlementResponse>> CreateSettlement([FromBody] CreateSettlementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _settlementService.CreateSettlementAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSettlement), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create settlement");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get settlement by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SettlementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementResponse>> GetSettlement(Guid id, CancellationToken cancellationToken)
    {
        var settlement = await _settlementService.GetSettlementByIdAsync(id, cancellationToken);
        if (settlement == null)
        {
            return NotFound(new { error = "Settlement not found" });
        }
        return Ok(settlement);
    }

    /// <summary>
    /// Get settlement by reference
    /// </summary>
    [HttpGet("reference/{reference}")]
    [ProducesResponseType(typeof(SettlementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementResponse>> GetSettlementByReference(string reference, CancellationToken cancellationToken)
    {
        var settlement = await _settlementService.GetSettlementByReferenceAsync(reference, cancellationToken);
        if (settlement == null)
        {
            return NotFound(new { error = "Settlement not found" });
        }
        return Ok(settlement);
    }

    /// <summary>
    /// Get settlements for a merchant
    /// </summary>
    [HttpGet("merchant/{merchantId}")]
    [ProducesResponseType(typeof(List<SettlementResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SettlementResponse>>> GetMerchantSettlements(
        Guid merchantId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var settlements = await _settlementService.GetSettlementsByMerchantAsync(merchantId, fromDate, toDate, cancellationToken);
        return Ok(settlements);
    }

    /// <summary>
    /// Process a settlement transaction
    /// </summary>
    [HttpPost("{id}/process")]
    [ProducesResponseType(typeof(SettlementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementResponse>> ProcessSettlement(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _settlementService.ProcessSettlementAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to process settlement {SettlementId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create batch settlement
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(SettlementBatchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettlementBatchResponse>> CreateBatchSettlement([FromBody] List<Guid> settlementIds, CancellationToken cancellationToken)
    {
        if (settlementIds == null || settlementIds.Count == 0)
        {
            return BadRequest(new { error = "Settlement IDs are required" });
        }

        var batch = await _settlementService.CreateBatchSettlementAsync(settlementIds, cancellationToken);
        return Created(string.Empty, batch);
    }

    /// <summary>
    /// Get settlement summary for a merchant
    /// </summary>
    [HttpGet("merchant/{merchantId}/summary")]
    [ProducesResponseType(typeof(SettlementSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettlementSummaryResponse>> GetSettlementSummary(Guid merchantId, CancellationToken cancellationToken)
    {
        var summary = await _settlementService.GetSettlementSummaryAsync(merchantId, cancellationToken);
        return Ok(summary);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MerchantAccountsController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<MerchantAccountsController> _logger;

    public MerchantAccountsController(
        ISettlementService settlementService,
        ILogger<MerchantAccountsController> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Register a merchant bank account
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MerchantAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MerchantAccountResponse>> RegisterAccount([FromBody] MerchantAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _settlementService.RegisterMerchantAccountAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAccount), new { merchantId = result.MerchantId }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to register merchant account");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get merchant account details
    /// </summary>
    [HttpGet("{merchantId}")]
    [ProducesResponseType(typeof(MerchantAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MerchantAccountResponse>> GetAccount(Guid merchantId, CancellationToken cancellationToken)
    {
        var account = await _settlementService.GetMerchantAccountAsync(merchantId, cancellationToken);
        if (account == null)
        {
            return NotFound(new { error = "Merchant account not found" });
        }
        return Ok(account);
    }

    /// <summary>
    /// Verify merchant bank account
    /// </summary>
    [HttpPost("{merchantId}/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> VerifyAccount(Guid merchantId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _settlementService.VerifyMerchantAccountAsync(merchantId, cancellationToken);
            return Ok(new { verified = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to verify merchant account");
            return BadRequest(new { error = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "Settlement API",
            timestamp = DateTime.UtcNow
        });
    }
}