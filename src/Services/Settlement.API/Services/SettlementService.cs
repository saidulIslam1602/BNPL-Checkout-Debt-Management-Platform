using AutoMapper;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Settlement.API.Data;
using YourCompanyBNPL.Settlement.API.DTOs;
using YourCompanyBNPL.Settlement.API.Models;

namespace YourCompanyBNPL.Settlement.API.Services;

public class SettlementService : ISettlementService
{
    private readonly SettlementDbContext _context;
    private readonly IMapper _mapper;
    private readonly INorwegianBankService _bankService;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        SettlementDbContext context,
        IMapper mapper,
        INorwegianBankService bankService,
        ILogger<SettlementService> logger)
    {
        _context = context;
        _mapper = mapper;
        _bankService = bankService;
        _logger = logger;
    }

    public async Task<SettlementResponse> CreateSettlementAsync(CreateSettlementRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating settlement for merchant {MerchantId}, amount {Amount}", request.MerchantId, request.Amount);

        // Verify merchant account exists
        var merchantAccount = await _context.MerchantAccounts
            .FirstOrDefaultAsync(m => m.MerchantId == request.MerchantId && m.IsActive, cancellationToken);

        if (merchantAccount == null)
        {
            throw new InvalidOperationException($"Active merchant account not found for merchant {request.MerchantId}");
        }

        if (!merchantAccount.IsVerified)
        {
            throw new InvalidOperationException($"Merchant account for {request.MerchantId} is not verified");
        }

        // Create settlement transaction
        var settlement = _mapper.Map<SettlementTransaction>(request);
        _context.Settlements.Add(settlement);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Settlement created with ID {SettlementId} and reference {Reference}", settlement.Id, settlement.Reference);

        return _mapper.Map<SettlementResponse>(settlement);
    }

    public async Task<SettlementResponse?> GetSettlementByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements.FindAsync(new object[] { id }, cancellationToken);
        return settlement != null ? _mapper.Map<SettlementResponse>(settlement) : null;
    }

    public async Task<SettlementResponse?> GetSettlementByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements
            .FirstOrDefaultAsync(s => s.Reference == reference, cancellationToken);
        return settlement != null ? _mapper.Map<SettlementResponse>(settlement) : null;
    }

    public async Task<List<SettlementResponse>> GetSettlementsByMerchantAsync(Guid merchantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Settlements.Where(s => s.MerchantId == merchantId);

        if (fromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.CreatedAt <= toDate.Value);

        var settlements = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
        return _mapper.Map<List<SettlementResponse>>(settlements);
    }

    public async Task<SettlementResponse> ProcessSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.Settlements.FindAsync(new object[] { settlementId }, cancellationToken);
        if (settlement == null)
        {
            throw new InvalidOperationException($"Settlement {settlementId} not found");
        }

        if (settlement.Status != "Pending")
        {
            throw new InvalidOperationException($"Settlement {settlementId} is already {settlement.Status}");
        }

        // Get merchant account details
        var merchantAccount = await _context.MerchantAccounts
            .FirstOrDefaultAsync(m => m.MerchantId == settlement.MerchantId && m.IsActive, cancellationToken);

        if (merchantAccount == null)
        {
            settlement.Status = "Failed";
            settlement.ErrorMessage = "Merchant account not found";
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Merchant account not found");
        }

        // Update status to Processing
        settlement.Status = "Processing";
        settlement.ProcessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Initiate bank transfer
            var bankRequest = new SettlementRequest
            {
                MerchantId = settlement.MerchantId,
                BankAccount = merchantAccount.BankAccountNumber,
                Amount = settlement.Amount,
                Reference = settlement.Reference,
                ExecutionDate = null,
                IsUrgent = false
            };

            var result = await _bankService.InitiateSettlementAsync(bankRequest, cancellationToken);

            if (result.Success)
            {
                settlement.Status = "Completed";
                settlement.CompletedAt = DateTime.UtcNow;
                settlement.BankTransferId = result.TransferId;
                merchantAccount.LastSettlementAt = DateTime.UtcNow;
                
                _logger.LogInformation("Settlement {SettlementId} processed successfully", settlementId);
            }
            else
            {
                settlement.Status = result.IsRetryable ? "Pending" : "Failed";
                settlement.ErrorMessage = result.ErrorMessage;
                settlement.ErrorCode = result.ErrorCode;
                settlement.RetryCount++;
                
                _logger.LogWarning("Settlement {SettlementId} failed: {ErrorMessage}", settlementId, result.ErrorMessage);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return _mapper.Map<SettlementResponse>(settlement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing settlement {SettlementId}", settlementId);
            settlement.Status = "Failed";
            settlement.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<SettlementBatchResponse> CreateBatchSettlementAsync(List<Guid> settlementIds, CancellationToken cancellationToken = default)
    {
        var settlements = await _context.Settlements
            .Where(s => settlementIds.Contains(s.Id) && s.Status == "Pending")
            .ToListAsync(cancellationToken);

        var batch = new SettlementBatch
        {
            Id = Guid.NewGuid(),
            BatchReference = $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            TransactionCount = settlements.Count,
            TotalAmount = settlements.Sum(s => s.Amount),
            Currency = "NOK",
            Status = "Processing",
            CreatedAt = DateTime.UtcNow
        };

        _context.SettlementBatches.Add(batch);

        foreach (var settlement in settlements)
        {
            settlement.BatchId = batch.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Process settlements asynchronously (in a real application, this would be done in a background job)
        _ = Task.Run(async () =>
        {
            foreach (var settlementId in settlementIds)
            {
                try
                {
                    await ProcessSettlementAsync(settlementId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing settlement {SettlementId} in batch {BatchId}", settlementId, batch.Id);
                }
            }

            // Update batch status
            var finalBatch = await _context.SettlementBatches.FindAsync(batch.Id);
            if (finalBatch != null)
            {
                var batchSettlements = await _context.Settlements
                    .Where(s => s.BatchId == batch.Id)
                    .ToListAsync();

                finalBatch.SuccessfulTransactions = batchSettlements.Count(s => s.Status == "Completed");
                finalBatch.FailedTransactions = batchSettlements.Count(s => s.Status == "Failed");
                finalBatch.Status = finalBatch.FailedTransactions == 0 ? "Completed" : "PartiallyCompleted";
                finalBatch.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }, CancellationToken.None);

        return _mapper.Map<SettlementBatchResponse>(batch);
    }

    public async Task<SettlementSummaryResponse> GetSettlementSummaryAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var settlements = await _context.Settlements
            .Where(s => s.MerchantId == merchantId)
            .ToListAsync(cancellationToken);

        return new SettlementSummaryResponse
        {
            MerchantId = merchantId,
            TotalSettlements = settlements.Count,
            TotalAmount = settlements.Sum(s => s.Amount),
            PendingSettlements = settlements.Count(s => s.Status == "Pending"),
            PendingAmount = settlements.Where(s => s.Status == "Pending").Sum(s => s.Amount),
            CompletedSettlements = settlements.Count(s => s.Status == "Completed"),
            CompletedAmount = settlements.Where(s => s.Status == "Completed").Sum(s => s.Amount),
            FailedSettlements = settlements.Count(s => s.Status == "Failed"),
            LastSettlementAt = settlements.OrderByDescending(s => s.CompletedAt).FirstOrDefault()?.CompletedAt
        };
    }

    public async Task<MerchantAccountResponse> RegisterMerchantAccountAsync(MerchantAccountRequest request, CancellationToken cancellationToken = default)
    {
        // Check if merchant already has an account
        var existingAccount = await _context.MerchantAccounts
            .FirstOrDefaultAsync(m => m.MerchantId == request.MerchantId, cancellationToken);

        if (existingAccount != null)
        {
            throw new InvalidOperationException($"Merchant {request.MerchantId} already has a registered account");
        }

        var merchantAccount = _mapper.Map<MerchantAccount>(request);
        _context.MerchantAccounts.Add(merchantAccount);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Merchant account registered for merchant {MerchantId}", request.MerchantId);

        return _mapper.Map<MerchantAccountResponse>(merchantAccount);
    }

    public async Task<MerchantAccountResponse?> GetMerchantAccountAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var account = await _context.MerchantAccounts
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId, cancellationToken);
        return account != null ? _mapper.Map<MerchantAccountResponse>(account) : null;
    }

    public async Task<bool> VerifyMerchantAccountAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var account = await _context.MerchantAccounts
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId && m.IsActive, cancellationToken);

        if (account == null)
        {
            throw new InvalidOperationException($"Merchant account not found for {merchantId}");
        }

        // Verify account with bank
        var validationResult = await _bankService.ValidateAccountAsync(account.BankAccountNumber, cancellationToken);

        if (validationResult.IsValid)
        {
            account.IsVerified = true;
            account.VerifiedAt = DateTime.UtcNow;
            account.BankName = validationResult.BankName;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Merchant account verified for {MerchantId}", merchantId);
            return true;
        }

        _logger.LogWarning("Merchant account verification failed for {MerchantId}", merchantId);
        return false;
    }
}