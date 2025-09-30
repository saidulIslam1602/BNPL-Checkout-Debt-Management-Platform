using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

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

    public async Task<ApiResponse<List<SettlementSummary>>> CreateSettlementsAsync(CreateSettlementRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating settlements for date {SettlementDate}", request.SettlementDate);

            // Validate request
            var validationResult = await ValidateCreateSettlementRequestAsync(request, cancellationToken);
            if (!validationResult.Success)
            {
                return ApiResponse<List<SettlementSummary>>.ErrorResult(validationResult.Errors.First(), 400);
            }

            // Get all merchants with eligible transactions
            var eligiblePayments = await _context.Payments
                .Include(p => p.Merchant)
                .Where(p => p.Status == PaymentStatus.Completed &&
                           p.ProcessedAt.HasValue &&
                           p.ProcessedAt.Value.Date <= request.SettlementDate.Date &&
                           !_context.SettlementTransactions.Any(st => st.PaymentId == p.Id))
                .GroupBy(p => p.MerchantId)
                .ToListAsync(cancellationToken);

            // Apply merchant filter if specified
            if (request.MerchantIds != null && request.MerchantIds.Any())
            {
                var filteredPayments = eligiblePayments.Where(g => request.MerchantIds.Contains(g.Key)).ToList();
                eligiblePayments = filteredPayments;
            }

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

                // Apply minimum amount threshold
                if (request.MinimumAmount.HasValue && netAmount < request.MinimumAmount.Value)
                {
                    _logger.LogInformation("Settlement amount {Amount} below minimum threshold {MinAmount} for merchant {MerchantId}", 
                        netAmount, request.MinimumAmount.Value, merchantId);
                    continue;
                }

                // Create settlement
                var settlement = new Settlement
                {
                    MerchantId = merchantId,
                    SettlementDate = request.SettlementDate,
                    GrossAmount = grossAmount,
                    Fees = totalFees,
                    NetAmount = netAmount,
                    Currency = payments.First().Currency, // Assuming single currency per merchant
                    Status = request.ProcessImmediately ? SettlementStatus.Processing : SettlementStatus.Pending,
                    TransactionCount = payments.Count,
                    RetryCount = 0
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

            _logger.LogInformation("Created {Count} settlements for date {SettlementDate}", settlements.Count, request.SettlementDate);

            return ApiResponse<List<SettlementSummary>>.SuccessResult(responses, $"Created {settlements.Count} settlements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlements for date {SettlementDate}", request.SettlementDate);
            return ApiResponse<List<SettlementSummary>>.ErrorResult("An error occurred while creating settlements", 500);
        }
    }

    public async Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, SettlementFilterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Settlements
                .Include(s => s.Merchant)
                .Where(s => s.MerchantId == merchantId);

            query = ApplySettlementFilters(query, request);
            query = ApplySettlementSorting(query, request.SortBy, request.SortDirection);

            var totalCount = await query.CountAsync(cancellationToken);

            var settlements = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = settlements.Select(s => 
            {
                var summary = _mapper.Map<SettlementSummary>(s);
                summary.MerchantName = s.Merchant?.Name ?? "";
                return summary;
            }).ToList();

            return PagedApiResponse<SettlementSummary>.SuccessResult(responses, request.Page, request.PageSize, totalCount);
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

            return ApiResponse.SuccessResponse(message);
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

    private async Task<ApiResponse> ValidateCreateSettlementRequestAsync(CreateSettlementRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (request.SettlementDate > DateTime.UtcNow.Date)
        {
            errors.Add("Settlement date cannot be in the future");
        }

        if (request.MerchantIds != null && request.MerchantIds.Any())
        {
            var existingMerchants = await _context.Merchants
                .Where(m => request.MerchantIds.Contains(m.Id))
                .CountAsync(cancellationToken);

            if (existingMerchants != request.MerchantIds.Count)
            {
                errors.Add("One or more merchant IDs are invalid");
            }
        }

        return errors.Any() ? 
            ApiResponse.ErrorResult(string.Join("; ", errors), 400) : 
            ApiResponse.SuccessResponse();
    }

    private IQueryable<Settlement> ApplySettlementFilters(IQueryable<Settlement> query, SettlementFilterRequest request)
    {
        if (request.FromDate.HasValue)
        {
            query = query.Where(s => s.SettlementDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(s => s.SettlementDate <= request.ToDate.Value);
        }

        if (request.Statuses != null && request.Statuses.Any())
        {
            query = query.Where(s => request.Statuses.Contains(s.Status));
        }

        if (request.MerchantIds != null && request.MerchantIds.Any())
        {
            query = query.Where(s => request.MerchantIds.Contains(s.MerchantId));
        }

        if (request.Currencies != null && request.Currencies.Any())
        {
            query = query.Where(s => request.Currencies.Contains(s.Currency));
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(s => s.NetAmount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(s => s.NetAmount <= request.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(s => s.Merchant!.Name.Contains(request.SearchTerm) ||
                                    s.BankTransactionId!.Contains(request.SearchTerm));
        }

        return query;
    }

    private IQueryable<Settlement> ApplySettlementSorting(IQueryable<Settlement> query, SettlementSortBy sortBy, SortDirection direction)
    {
        System.Linq.Expressions.Expression<Func<Settlement, object>> sortExpression = sortBy switch
        {
            SettlementSortBy.SettlementDate => s => s.SettlementDate,
            SettlementSortBy.CreatedAt => s => s.CreatedAt,
            SettlementSortBy.ProcessedAt => s => s.ProcessedAt ?? DateTime.MinValue,
            SettlementSortBy.GrossAmount => s => s.GrossAmount,
            SettlementSortBy.Amount => s => s.GrossAmount, // Alias for GrossAmount
            SettlementSortBy.NetAmount => s => s.NetAmount,
            SettlementSortBy.Status => s => s.Status,
            SettlementSortBy.MerchantName => s => s.Merchant!.Name,
            SettlementSortBy.MerchantId => s => s.MerchantId,
            SettlementSortBy.Currency => s => s.Currency,
            SettlementSortBy.TransactionCount => s => s.TransactionCount,
            _ => s => s.SettlementDate
        };

        return direction == SortDirection.Ascending ? 
            query.OrderBy(sortExpression) : 
            query.OrderByDescending(sortExpression);
    }

    #endregion

    // Placeholder implementations for new interface methods
    public Task<PagedApiResponse<SettlementSummary>> GetAllSettlementsAsync(SettlementFilterRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementDetails>> GetSettlementDetailsAsync(Guid settlementId, bool includeTransactions = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementProcessingResult>> ProcessPendingSettlementsAsync(ProcessSettlementsRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementSummary>> ProcessSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementSummary>> CancelSettlementAsync(Guid settlementId, CancelSettlementRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementSummary>> RetrySettlementAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implementation in progress");
    }

    public Task<ApiResponse<SettlementAnalytics>> GetSettlementAnalyticsAsync(SettlementAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Analytics implementation coming next");
    }

    public Task<ApiResponse<byte[]>> ExportSettlementsAsync(SettlementExportRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Export implementation coming next");
    }

    public Task<ApiResponse<SettlementReconciliationReport>> GetReconciliationReportAsync(SettlementReconciliationRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Reconciliation implementation coming next");
    }

    public Task<ApiResponse<SettlementScheduleConfig>> ConfigureSettlementScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Scheduling implementation coming next");
    }

    public Task<ApiResponse<SettlementScheduleConfig>> GetSettlementScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Scheduling implementation coming next");
    }

    public Task<ApiResponse> UpdateSettlementStatusAsync(Guid settlementId, SettlementStatus status, string? bankTransactionId = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Status update implementation coming next");
    }

    public Task<ApiResponse<bool>> ValidateSettlementEligibilityAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Eligibility validation implementation coming next");
    }
}