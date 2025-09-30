using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

// Note: IEnhancedSettlementService interface is defined in IEnhancedSettlementService.cs

public class EnhancedSettlementService : IEnhancedSettlementService
{
    private readonly PaymentDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EnhancedSettlementService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EnhancedSettlementService(
        PaymentDbContext context,
        IMapper mapper,
        ILogger<EnhancedSettlementService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResponse<SettlementBatchResponse>> CreateSettlementBatchAsync(CreateSettlementBatchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating settlement batch for merchant {MerchantId}", request.MerchantId);

            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == request.MerchantId, cancellationToken);

            if (merchant == null)
            {
                return ApiResponse<SettlementBatchResponse>.ErrorResult("Merchant not found", 404);
            }

            // Get eligible payments for settlement
            var eligiblePayments = await GetEligiblePaymentsAsync(request.MerchantId, request.FromDate, request.ToDate, cancellationToken);

            if (!eligiblePayments.Any())
            {
                return ApiResponse<SettlementBatchResponse>.ErrorResult("No eligible payments found for settlement", 400);
            }

            // Calculate settlement amounts
            var grossAmount = eligiblePayments.Sum(p => p.Amount);
            var totalFees = eligiblePayments.Sum(p => p.Fees);
            var netAmount = grossAmount - totalFees;

            // Create settlement batch
            var settlementBatch = new SettlementBatch
            {
                MerchantId = request.MerchantId,
                BatchReference = GenerateBatchReference(),
                Currency = eligiblePayments.First().Currency,
                GrossAmount = grossAmount,
                TotalFees = totalFees,
                NetAmount = netAmount,
                TransactionCount = eligiblePayments.Count,
                SettlementDate = request.SettlementDate,
                Status = SettlementStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.SettlementBatches.Add(settlementBatch);
            await _context.SaveChangesAsync(cancellationToken);

            // Create settlement items
            var settlementItems = eligiblePayments.Select(payment => new SettlementItem
            {
                SettlementBatchId = settlementBatch.Id,
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Fees = payment.Fees,
                NetAmount = payment.Amount - payment.Fees,
                TransactionType = SettlementTransactionType.Payment
            }).ToList();

            // Add refund items if any
            var refunds = await GetEligibleRefundsAsync(request.MerchantId, request.FromDate, request.ToDate, cancellationToken);
            var refundItems = refunds.Select(refund => new SettlementItem
            {
                SettlementBatchId = settlementBatch.Id,
                PaymentId = refund.PaymentId,
                RefundId = refund.Id,
                Amount = -refund.Amount, // Negative for refunds
                Fees = 0,
                NetAmount = -refund.Amount,
                TransactionType = SettlementTransactionType.Refund
            }).ToList();

            settlementItems.AddRange(refundItems);
            _context.SettlementItems.AddRange(settlementItems);

            // Update net amount with refunds
            settlementBatch.NetAmount -= refunds.Sum(r => r.Amount);
            settlementBatch.TransactionCount += refunds.Count;

            await _context.SaveChangesAsync(cancellationToken);

            var response = new SettlementBatchResponse
            {
                Id = settlementBatch.Id,
                BatchReference = settlementBatch.BatchReference,
                MerchantId = settlementBatch.MerchantId,
                Currency = settlementBatch.Currency,
                GrossAmount = settlementBatch.GrossAmount,
                TotalFees = settlementBatch.TotalFees,
                NetAmount = settlementBatch.NetAmount,
                TransactionCount = settlementBatch.TransactionCount,
                SettlementDate = settlementBatch.SettlementDate,
                Status = settlementBatch.Status,
                CreatedAt = settlementBatch.CreatedAt
            };

            _logger.LogInformation("Settlement batch {BatchId} created successfully with {Count} transactions", 
                settlementBatch.Id, settlementBatch.TransactionCount);

            return ApiResponse<SettlementBatchResponse>.SuccessResult(response, "Settlement batch created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement batch for merchant {MerchantId}", request.MerchantId);
            return ApiResponse<SettlementBatchResponse>.ErrorResult("Failed to create settlement batch", 500);
        }
    }

    public async Task<ApiResponse<SettlementBatchResponse>> ProcessSettlementBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing settlement batch {BatchId}", batchId);

            var batch = await _context.SettlementBatches
                .Include(sb => sb.Merchant)
                .Include(sb => sb.Items)
                .FirstOrDefaultAsync(sb => sb.Id == batchId, cancellationToken);

            if (batch == null)
            {
                return ApiResponse<SettlementBatchResponse>.ErrorResult("Settlement batch not found", 404);
            }

            if (batch.Status != SettlementStatus.Pending)
            {
                return ApiResponse<SettlementBatchResponse>.ErrorResult($"Settlement batch cannot be processed. Current status: {batch.Status}", 400);
            }

            // Process settlement through banking system
            var processingResult = await ProcessSettlementThroughBankAsync(batch, cancellationToken);

            if (processingResult.Success)
            {
                batch.Status = SettlementStatus.Completed;
                batch.ProcessedAt = DateTime.UtcNow;
                batch.BankTransactionId = processingResult.TransactionId;

                // Mark payments as settled
                var paymentIds = batch.Items.Where(i => i.PaymentId.HasValue).Select(i => i.PaymentId!.Value).ToList();
                var payments = await _context.Payments
                    .Where(p => paymentIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                foreach (var payment in payments)
                {
                    payment.SettlementStatus = SettlementStatus.Completed;
                    payment.SettledAt = DateTime.UtcNow;
                }

                // Mark refunds as settled
                var refundIds = batch.Items.Where(i => i.RefundId.HasValue).Select(i => i.RefundId!.Value).ToList();
                var refunds = await _context.PaymentRefunds
                    .Where(r => refundIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);

                foreach (var refund in refunds)
                {
                    refund.SettlementStatus = SettlementStatus.Completed;
                    refund.SettledAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Settlement batch {BatchId} processed successfully", batchId);
            }
            else
            {
                batch.Status = SettlementStatus.Failed;
                batch.FailureReason = processingResult.FailureReason;
                
                _logger.LogWarning("Settlement batch {BatchId} processing failed: {Reason}", batchId, processingResult.FailureReason);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<SettlementBatchResponse>(batch);
            return ApiResponse<SettlementBatchResponse>.SuccessResult(response, "Settlement batch processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing settlement batch {BatchId}", batchId);
            return ApiResponse<SettlementBatchResponse>.ErrorResult("Failed to process settlement batch", 500);
        }
    }

    public async Task<ApiResponse<SettlementSummary>> GetSettlementSummaryAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settlement = await _context.SettlementBatches
                .Include(sb => sb.Merchant)
                .Include(sb => sb.Items)
                    .ThenInclude(si => si.Payment)
                .Include(sb => sb.Items)
                    .ThenInclude(si => si.Refund)
                .FirstOrDefaultAsync(sb => sb.Id == settlementId, cancellationToken);

            if (settlement == null)
            {
                return ApiResponse<SettlementSummary>.ErrorResult("Settlement not found", 404);
            }

            var summary = _mapper.Map<SettlementSummary>(settlement);
            return ApiResponse<SettlementSummary>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement summary for {SettlementId}", settlementId);
            return ApiResponse<SettlementSummary>.ErrorResult("Failed to retrieve settlement summary", 500);
        }
    }

    public async Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, SettlementSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SettlementBatches
                .Include(sb => sb.Merchant)
                .Where(sb => sb.MerchantId == merchantId);

            // Apply filters
            if (request.Status.HasValue)
                query = query.Where(sb => sb.Status == request.Status.Value);

            if (request.FromDate.HasValue)
                query = query.Where(sb => sb.SettlementDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(sb => sb.SettlementDate <= request.ToDate.Value);

            if (request.Currency.HasValue)
                query = query.Where(sb => sb.Currency == request.Currency.Value);

            // Apply sorting
            query = request.SortBy switch
            {
                SettlementSortBy.Amount => request.SortDescending ? query.OrderByDescending(sb => sb.NetAmount) : query.OrderBy(sb => sb.NetAmount),
                SettlementSortBy.Status => request.SortDescending ? query.OrderByDescending(sb => sb.Status) : query.OrderBy(sb => sb.Status),
                SettlementSortBy.MerchantId => request.SortDescending ? query.OrderByDescending(sb => sb.MerchantId) : query.OrderBy(sb => sb.MerchantId),
                SettlementSortBy.Currency => request.SortDescending ? query.OrderByDescending(sb => sb.Currency) : query.OrderBy(sb => sb.Currency),
                _ => request.SortDescending ? query.OrderByDescending(sb => sb.CreatedAt) : query.OrderBy(sb => sb.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var settlements = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var summaries = settlements.Select(s => _mapper.Map<SettlementSummary>(s)).ToList();

            return PagedApiResponse<SettlementSummary>.SuccessResult(summaries, request.Page, request.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlements for merchant {MerchantId}", merchantId);
            return new PagedApiResponse<SettlementSummary>
            {
                Success = false,
                Errors = new List<string> { "Failed to retrieve settlements" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse<SettlementReportResponse>> GenerateSettlementReportAsync(SettlementReportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating settlement report for merchant {MerchantId}", request.MerchantId);

            var settlements = await _context.SettlementBatches
                .Include(sb => sb.Items)
                .Where(sb => sb.MerchantId == request.MerchantId &&
                           sb.SettlementDate >= request.FromDate &&
                           sb.SettlementDate <= request.ToDate)
                .ToListAsync(cancellationToken);

            var report = new SettlementReportResponse
            {
                MerchantId = request.MerchantId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Summary = new SettlementReportSummary
                {
                    TotalSettlements = settlements.Count,
                    TotalGrossAmount = settlements.Sum(s => s.GrossAmount),
                    TotalFees = settlements.Sum(s => s.TotalFees),
                    TotalNetAmount = settlements.Sum(s => s.NetAmount),
                    TotalTransactions = settlements.Sum(s => s.TransactionCount),
                    StatusBreakdown = settlements.GroupBy(s => s.Status).ToDictionary(g => g.Key, g => g.Count()),
                    CurrencyBreakdown = settlements.GroupBy(s => s.Currency).ToDictionary(g => g.Key, g => g.Sum(s => s.NetAmount))
                },
                DailySettlements = settlements
                    .GroupBy(s => s.SettlementDate.Date)
                    .Select(g => new DailySettlementSummary
                    {
                        Date = g.Key,
                        SettlementCount = g.Count(),
                        GrossAmount = g.Sum(s => s.GrossAmount),
                        NetAmount = g.Sum(s => s.NetAmount),
                        Fees = g.Sum(s => s.TotalFees),
                        TransactionCount = g.Sum(s => s.TransactionCount)
                    }).ToList(),
                GeneratedAt = DateTime.UtcNow
            };

            return ApiResponse<SettlementReportResponse>.SuccessResult(report, "Settlement report generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating settlement report for merchant {MerchantId}", request.MerchantId);
            return ApiResponse<SettlementReportResponse>.ErrorResult("Failed to generate settlement report", 500);
        }
    }

    public async Task<ApiResponse> ProcessAutomaticSettlementsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing automatic settlements");

            var merchants = await _context.Merchants
                .Where(m => m.IsActive && m.AutoSettlementEnabled)
                .ToListAsync(cancellationToken);

            var processedCount = 0;

            foreach (var merchant in merchants)
            {
                try
                {
                    var cutoffDate = DateTime.UtcNow.Date.AddDays(-merchant.SettlementDelayDays);
                    var eligiblePayments = await GetEligiblePaymentsAsync(merchant.Id, null, cutoffDate, cancellationToken);

                    if (eligiblePayments.Any())
                    {
                        var request = new CreateSettlementBatchRequest
                        {
                            MerchantId = merchant.Id,
                            ToDate = cutoffDate,
                            SettlementDate = DateTime.UtcNow.Date.AddDays(1)
                        };

                        var batchResult = await CreateSettlementBatchAsync(request, cancellationToken);
                        if (batchResult.Success && batchResult.Data != null)
                        {
                            await ProcessSettlementBatchAsync(batchResult.Data.Id, cancellationToken);
                            processedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing automatic settlement for merchant {MerchantId}", merchant.Id);
                }
            }

            _logger.LogInformation("Processed {Count} automatic settlements", processedCount);
            return ApiResponse.SuccessResponse($"Processed {processedCount} automatic settlements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing automatic settlements");
            return ApiResponse.ErrorResult("Failed to process automatic settlements", 500);
        }
    }

    public async Task<ApiResponse<SettlementForecastResponse>> GetSettlementForecastAsync(Guid merchantId, int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);

            if (merchant == null)
            {
                return ApiResponse<SettlementForecastResponse>.ErrorResult("Merchant not found", 404);
            }

            var forecastDate = DateTime.UtcNow.Date.AddDays(days);
            var eligiblePayments = await GetEligiblePaymentsAsync(merchantId, null, forecastDate, cancellationToken);

            var forecast = new SettlementForecastResponse
            {
                MerchantId = merchantId,
                ForecastDate = forecastDate,
                ForecastDays = days,
                Summary = new SettlementForecastSummary
                {
                    EstimatedTotalAmount = eligiblePayments.Sum(p => p.Amount),
                    EstimatedTotalFees = eligiblePayments.Sum(p => p.Fees),
                    EstimatedNetAmount = eligiblePayments.Sum(p => p.Amount - p.Fees),
                    EstimatedTransactionCount = eligiblePayments.Count
                },
                DailyForecasts = eligiblePayments
                    .GroupBy(p => p.CreatedAt.Date.AddDays(merchant.SettlementDelayDays))
                    .Where(g => g.Key <= forecastDate)
                    .Select(g => new SettlementForecastItem
                    {
                        Date = g.Key,
                        EstimatedTransactionCount = g.Count(),
                        EstimatedAmount = g.Sum(p => p.Amount),
                        EstimatedFees = g.Sum(p => p.Fees),
                        EstimatedNetAmount = g.Sum(p => p.Amount - p.Fees),
                        ConfidenceLevel = 0.85 // Default confidence level
                    }).ToList()
            };

            return ApiResponse<SettlementForecastResponse>.SuccessResult(forecast, "Settlement forecast generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating settlement forecast for merchant {MerchantId}", merchantId);
            return ApiResponse<SettlementForecastResponse>.ErrorResult("Failed to generate settlement forecast", 500);
        }
    }

    #region Private Methods

    private async Task<List<Models.Payment>> GetEligiblePaymentsAsync(Guid merchantId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        var query = _context.Payments
            .Where(p => p.MerchantId == merchantId &&
                       p.Status == PaymentStatus.Completed &&
                       p.SettlementStatus != SettlementStatus.Completed);

        if (fromDate.HasValue)
            query = query.Where(p => p.ProcessedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.ProcessedAt <= toDate.Value);

        return await query.ToListAsync(cancellationToken);
    }

    private async Task<List<PaymentRefund>> GetEligibleRefundsAsync(Guid merchantId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        var query = _context.PaymentRefunds
            .Include(r => r.Payment)
            .Where(r => r.Payment.MerchantId == merchantId &&
                       r.Status == PaymentStatus.Completed &&
                       r.SettlementStatus != SettlementStatus.Completed);

        if (fromDate.HasValue)
            query = query.Where(r => r.ProcessedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.ProcessedAt <= toDate.Value);

        return await query.ToListAsync(cancellationToken);
    }

    private async Task<(bool Success, string? TransactionId, string? FailureReason)> ProcessSettlementThroughBankAsync(SettlementBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            // Simulate bank processing
            await Task.Delay(100, cancellationToken);

            // High success rate for settlements
            var random = new Random();
            if (random.NextDouble() < 0.98)
            {
                return (true, $"SETTLE_{Guid.NewGuid().ToString("N")[..12].ToUpper()}", null);
            }
            else
            {
                return (false, null, "Bank processing failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing settlement through bank");
            return (false, null, "Bank communication error");
        }
    }

    private static string GenerateBatchReference()
    {
        return $"BATCH_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    #endregion
}

// Note: All DTOs are defined in PaymentDTOs.cs
