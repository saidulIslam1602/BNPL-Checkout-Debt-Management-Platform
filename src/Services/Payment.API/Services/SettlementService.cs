using Microsoft.EntityFrameworkCore;
using AutoMapper;
using RivertyBNPL.Payment.API.Data;
using RivertyBNPL.Payment.API.DTOs;
using RivertyBNPL.Payment.API.Models;
using RivertyBNPL.Common.Models;
using RivertyBNPL.Common.Enums;

namespace RivertyBNPL.Payment.API.Services;

/// <summary>
/// Implementation of settlement operations for merchant payouts
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly PaymentDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        PaymentDbContext context,
        IMapper mapper,
        ILogger<SettlementService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SettlementSummary>>> CreateSettlementsAsync(DateTime settlementDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating settlements for date {SettlementDate}", settlementDate);

            // Get all merchants with eligible transactions
            var eligiblePayments = await _context.Payments
                .Include(p => p.Merchant)
                .Where(p => p.Status == PaymentStatus.Completed &&
                           p.ProcessedAt.HasValue &&
                           p.ProcessedAt.Value.Date <= settlementDate.Date &&
                           !_context.SettlementTransactions.Any(st => st.PaymentId == p.Id))
                .GroupBy(p => p.MerchantId)
                .ToListAsync(cancellationToken);

            var settlements = new List<Settlement>();

            foreach (var merchantPayments in eligiblePayments)
            {
                var merchantId = merchantPayments.Key;
                var payments = merchantPayments.ToList();
                var merchant = payments.First().Merchant;

                if (merchant == null || !merchant.IsActive)
                    continue;

                // Calculate settlement amounts
                var grossAmount = payments.Sum(p => p.Amount);
                var totalFees = payments.Sum(p => p.Fees);
                var netAmount = grossAmount - totalFees;

                // Create settlement
                var settlement = new Settlement
                {
                    MerchantId = merchantId,
                    SettlementDate = settlementDate,
                    GrossAmount = grossAmount,
                    Fees = totalFees,
                    NetAmount = netAmount,
                    Currency = payments.First().Currency, // Assuming single currency per merchant
                    Status = SettlementStatus.Pending,
                    TransactionCount = payments.Count
                };

                _context.Settlements.Add(settlement);
                settlements.Add(settlement);

                // Create settlement transactions
                var settlementTransactions = payments.Select(p => new SettlementTransaction
                {
                    SettlementId = settlement.Id,
                    PaymentId = p.Id,
                    Amount = p.Amount,
                    Fee = p.Fees,
                    NetAmount = p.Amount - p.Fees
                }).ToList();

                _context.SettlementTransactions.AddRange(settlementTransactions);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var responses = settlements.Select(s => _mapper.Map<SettlementSummary>(s)).ToList();

            _logger.LogInformation("Created {Count} settlements for date {SettlementDate}", settlements.Count, settlementDate);

            return ApiResponse<List<SettlementSummary>>.SuccessResult(responses, $"Created {settlements.Count} settlements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlements for date {SettlementDate}", settlementDate);
            return ApiResponse<List<SettlementSummary>>.ErrorResult("An error occurred while creating settlements", 500);
        }
    }

    public async Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Settlements
                .Where(s => s.MerchantId == merchantId)
                .OrderByDescending(s => s.SettlementDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var settlements = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = settlements.Select(s => _mapper.Map<SettlementSummary>(s)).ToList();

            return PagedApiResponse<SettlementSummary>.SuccessResult(responses, page, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlements for merchant {MerchantId}", merchantId);
            return new PagedApiResponse<SettlementSummary>
            {
                Success = false,
                Errors = new List<string> { "An error occurred while retrieving settlements" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse> ProcessPendingSettlementsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing pending settlements");

            var pendingSettlements = await _context.Settlements
                .Include(s => s.Merchant)
                .Where(s => s.Status == SettlementStatus.Pending)
                .OrderBy(s => s.SettlementDate)
                .ToListAsync(cancellationToken);

            var processedCount = 0;
            var failedCount = 0;

            foreach (var settlement in pendingSettlements)
            {
                try
                {
                    // Simulate bank transfer processing
                    var transferResult = await SimulateBankTransferAsync(settlement, cancellationToken);

                    if (transferResult.Success)
                    {
                        settlement.Status = SettlementStatus.Completed;
                        settlement.ProcessedAt = DateTime.UtcNow;
                        settlement.BankTransactionId = transferResult.TransactionId;
                        processedCount++;

                        _logger.LogInformation("Settlement {SettlementId} processed successfully", settlement.Id);
                    }
                    else
                    {
                        settlement.Status = SettlementStatus.Failed;
                        settlement.FailureReason = transferResult.FailureReason;
                        failedCount++;

                        _logger.LogWarning("Settlement {SettlementId} failed: {FailureReason}", 
                            settlement.Id, transferResult.FailureReason);
                    }
                }
                catch (Exception ex)
                {
                    settlement.Status = SettlementStatus.Failed;
                    settlement.FailureReason = "Processing error occurred";
                    failedCount++;

                    _logger.LogError(ex, "Error processing settlement {SettlementId}", settlement.Id);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var message = $"Processed {processedCount} settlements successfully, {failedCount} failed";
            _logger.LogInformation(message);

            return ApiResponse.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending settlements");
            return ApiResponse.ErrorResult("An error occurred while processing settlements", 500);
        }
    }

    public async Task<ApiResponse<SettlementSummary>> GetSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _context.Settlements
                .Include(s => s.Merchant)
                .Include(s => s.Transactions)
                    .ThenInclude(st => st.Payment)
                .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);

            if (settlement == null)
            {
                return ApiResponse<SettlementSummary>.ErrorResult("Settlement not found", 404);
            }

            var response = _mapper.Map<SettlementSummary>(settlement);
            return ApiResponse<SettlementSummary>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement {SettlementId}", settlementId);
            return ApiResponse<SettlementSummary>.ErrorResult("An error occurred while retrieving the settlement", 500);
        }
    }

    #region Private Methods

    private async Task<(bool Success, string? TransactionId, string? FailureReason)> SimulateBankTransferAsync(Settlement settlement, CancellationToken cancellationToken)
    {
        // Simulate processing delay
        await Task.Delay(100, cancellationToken);

        // Simulate high success rate for bank transfers (98%)
        var random = new Random();
        if (random.NextDouble() < 0.98)
        {
            var transactionId = $"BANK_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            return (true, transactionId, null);
        }
        else
        {
            var failureReasons = new[] 
            { 
                "Invalid bank account details", 
                "Bank account closed", 
                "Insufficient funds in settlement account",
                "Bank system temporarily unavailable"
            };
            var failureReason = failureReasons[random.Next(failureReasons.Length)];
            return (false, null, failureReason);
        }
    }

    #endregion
}