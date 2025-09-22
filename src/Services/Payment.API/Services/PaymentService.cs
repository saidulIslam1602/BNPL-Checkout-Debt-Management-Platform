using Microsoft.EntityFrameworkCore;
using AutoMapper;
using MediatR;
using RivertyBNPL.Payment.API.Data;
using RivertyBNPL.Payment.API.DTOs;
using RivertyBNPL.Payment.API.Models;
using RivertyBNPL.Common.Models;
using RivertyBNPL.Common.Enums;
using RivertyBNPL.Events.Payment;

namespace RivertyBNPL.Payment.API.Services;

/// <summary>
/// Implementation of payment processing operations
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentDbContext context,
        IMapper mapper,
        IMediator mediator,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating payment for customer {CustomerId}, merchant {MerchantId}, amount {Amount}",
                request.CustomerId, request.MerchantId, request.Amount);

            // Validate customer exists and is eligible
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.IsActive, cancellationToken);

            if (customer == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Customer not found or inactive", 404);
            }

            // Validate merchant exists and is active
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == request.MerchantId && m.IsActive, cancellationToken);

            if (merchant == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Merchant not found or inactive", 404);
            }

            // Check if BNPL is requested and validate eligibility
            if (request.EnableBNPL && request.BNPLPlanType.HasValue)
            {
                var eligibilityCheck = await CheckBNPLEligibilityAsync(customer, request.Amount, cancellationToken);
                if (!eligibilityCheck.IsEligible)
                {
                    return ApiResponse<PaymentResponse>.ErrorResult($"Customer not eligible for BNPL: {eligibilityCheck.Reason}", 400);
                }
            }

            // Create payment entity
            var payment = new Models.Payment
            {
                CustomerId = request.CustomerId,
                MerchantId = request.MerchantId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                OrderReference = request.OrderReference,
                Description = request.Description,
                Status = PaymentStatus.Pending,
                TransactionId = GenerateTransactionId(),
                NetAmount = request.Amount, // Will be updated after fee calculation
                ExpiresAt = DateTime.UtcNow.AddHours(24), // 24-hour expiry
                Metadata = request.Metadata
            };

            // Calculate fees
            var fees = CalculatePaymentFees(payment.Amount, merchant.CommissionRate, request.PaymentMethod);
            payment.Fees = fees;
            payment.NetAmount = payment.Amount - fees;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            // Create payment event
            var paymentEvent = new PaymentEvent
            {
                PaymentId = payment.Id,
                EventType = "PaymentCreated",
                FromStatus = PaymentStatus.Pending,
                ToStatus = PaymentStatus.Pending,
                Description = "Payment created successfully",
                CreatedBy = "System"
            };

            _context.PaymentEvents.Add(paymentEvent);
            await _context.SaveChangesAsync(cancellationToken);

            // Publish payment initiated event
            var paymentInitiatedEvent = new PaymentInitiatedEvent
            {
                PaymentId = payment.Id,
                CustomerId = payment.CustomerId,
                MerchantId = payment.MerchantId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                BNPLPlanType = request.BNPLPlanType,
                InstallmentCount = request.InstallmentCount,
                OrderReference = payment.OrderReference ?? string.Empty,
                Description = payment.Description,
                AggregateId = payment.Id,
                UserId = payment.CustomerId.ToString()
            };

            await _mediator.Publish(paymentInitiatedEvent, cancellationToken);

            // Map to response
            var response = _mapper.Map<PaymentResponse>(payment);
            response.Customer = _mapper.Map<CustomerSummary>(customer);
            response.Merchant = _mapper.Map<MerchantSummary>(merchant);

            _logger.LogInformation("Payment {PaymentId} created successfully", payment.Id);

            return ApiResponse<PaymentResponse>.SuccessResult(response, "Payment created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for customer {CustomerId}", request.CustomerId);
            return ApiResponse<PaymentResponse>.ErrorResult("An error occurred while creating the payment", 500);
        }
    }

    public async Task<ApiResponse<PaymentResponse>> ProcessPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing payment {PaymentId}", paymentId);

            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Payment not found", 404);
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<PaymentResponse>.ErrorResult($"Payment cannot be processed. Current status: {payment.Status}", 400);
            }

            if (payment.ExpiresAt.HasValue && payment.ExpiresAt < DateTime.UtcNow)
            {
                payment.Status = PaymentStatus.Expired;
                await UpdatePaymentStatusAsync(payment, PaymentStatus.Expired, "Payment expired", cancellationToken);
                return ApiResponse<PaymentResponse>.ErrorResult("Payment has expired", 400);
            }

            // Process payment through real payment gateway
            var processingResult = await ProcessPaymentThroughGatewayAsync(payment, cancellationToken);

            if (processingResult.Success)
            {
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.AuthorizationCode = processingResult.AuthorizationCode;
                payment.GatewayTransactionId = processingResult.GatewayTransactionId;

                await UpdatePaymentStatusAsync(payment, PaymentStatus.Completed, "Payment processed successfully", cancellationToken);

                // Publish payment completed event
                var paymentCompletedEvent = new PaymentCompletedEvent
                {
                    PaymentId = payment.Id,
                    CustomerId = payment.CustomerId,
                    MerchantId = payment.MerchantId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionId = payment.TransactionId ?? string.Empty,
                    PaymentMethod = payment.PaymentMethod,
                    ProcessedAt = payment.ProcessedAt.Value,
                    Fees = payment.Fees,
                    NetAmount = payment.NetAmount,
                    AggregateId = payment.Id,
                    UserId = payment.CustomerId.ToString()
                };

                await _mediator.Publish(paymentCompletedEvent, cancellationToken);

                _logger.LogInformation("Payment {PaymentId} processed successfully", paymentId);
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = processingResult.FailureReason;
                payment.ErrorCode = processingResult.ErrorCode;
                payment.IsRetryable = processingResult.IsRetryable;

                if (processingResult.IsRetryable && payment.RetryCount < 3)
                {
                    payment.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, payment.RetryCount) * 5); // Exponential backoff
                    payment.RetryCount++;
                }

                await UpdatePaymentStatusAsync(payment, PaymentStatus.Failed, processingResult.FailureReason, cancellationToken);

                // Publish payment failed event
                var paymentFailedEvent = new PaymentFailedEvent
                {
                    PaymentId = payment.Id,
                    CustomerId = payment.CustomerId,
                    MerchantId = payment.MerchantId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    FailureReason = processingResult.FailureReason ?? "Unknown error",
                    ErrorCode = processingResult.ErrorCode,
                    IsRetryable = processingResult.IsRetryable,
                    NextRetryAt = payment.NextRetryAt,
                    AggregateId = payment.Id,
                    UserId = payment.CustomerId.ToString()
                };

                await _mediator.Publish(paymentFailedEvent, cancellationToken);

                _logger.LogWarning("Payment {PaymentId} failed: {FailureReason}", paymentId, processingResult.FailureReason);
            }

            var response = _mapper.Map<PaymentResponse>(payment);
            response.Customer = _mapper.Map<CustomerSummary>(payment.Customer);
            response.Merchant = _mapper.Map<MerchantSummary>(payment.Merchant);

            return ApiResponse<PaymentResponse>.SuccessResult(response, "Payment processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
            return ApiResponse<PaymentResponse>.ErrorResult("An error occurred while processing the payment", 500);
        }
    }

    public async Task<ApiResponse<PaymentResponse>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Merchant)
                .Include(p => p.BNPLPlan)
                    .ThenInclude(bp => bp!.Installments)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Payment not found", 404);
            }

            var response = _mapper.Map<PaymentResponse>(payment);
            response.Customer = _mapper.Map<CustomerSummary>(payment.Customer);
            response.Merchant = _mapper.Map<MerchantSummary>(payment.Merchant);

            if (payment.BNPLPlan != null)
            {
                response.BNPLPlan = _mapper.Map<BNPLPlanSummary>(payment.BNPLPlan);
            }

            return ApiResponse<PaymentResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", paymentId);
            return ApiResponse<PaymentResponse>.ErrorResult("An error occurred while retrieving the payment", 500);
        }
    }

    public async Task<PagedApiResponse<PaymentResponse>> SearchPaymentsAsync(PaymentSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Merchant)
                .AsQueryable();

            // Apply filters
            if (request.CustomerId.HasValue)
                query = query.Where(p => p.CustomerId == request.CustomerId.Value);

            if (request.MerchantId.HasValue)
                query = query.Where(p => p.MerchantId == request.MerchantId.Value);

            if (request.Status.HasValue)
                query = query.Where(p => p.Status == request.Status.Value);

            if (request.PaymentMethod.HasValue)
                query = query.Where(p => p.PaymentMethod == request.PaymentMethod.Value);

            if (request.Currency.HasValue)
                query = query.Where(p => p.Currency == request.Currency.Value);

            if (request.MinAmount.HasValue)
                query = query.Where(p => p.Amount >= request.MinAmount.Value);

            if (request.MaxAmount.HasValue)
                query = query.Where(p => p.Amount <= request.MaxAmount.Value);

            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

            if (!string.IsNullOrEmpty(request.OrderReference))
                query = query.Where(p => p.OrderReference!.Contains(request.OrderReference));

            if (!string.IsNullOrEmpty(request.TransactionId))
                query = query.Where(p => p.TransactionId == request.TransactionId);

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "amount" => request.SortDescending ? query.OrderByDescending(p => p.Amount) : query.OrderBy(p => p.Amount),
                "status" => request.SortDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
                "processedat" => request.SortDescending ? query.OrderByDescending(p => p.ProcessedAt) : query.OrderBy(p => p.ProcessedAt),
                _ => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var payments = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = payments.Select(p =>
            {
                var response = _mapper.Map<PaymentResponse>(p);
                response.Customer = _mapper.Map<CustomerSummary>(p.Customer);
                response.Merchant = _mapper.Map<MerchantSummary>(p.Merchant);
                return response;
            }).ToList();

            return PagedApiResponse<PaymentResponse>.SuccessResult(
                responses, request.Page, request.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching payments");
            return new PagedApiResponse<PaymentResponse>
            {
                Success = false,
                Errors = new List<string> { "An error occurred while searching payments" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse<PaymentResponse>> CancelPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<PaymentResponse>.ErrorResult("Payment not found", 404);
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<PaymentResponse>.ErrorResult($"Payment cannot be cancelled. Current status: {payment.Status}", 400);
            }

            payment.Status = PaymentStatus.Cancelled;
            await UpdatePaymentStatusAsync(payment, PaymentStatus.Cancelled, reason, cancellationToken);

            var response = _mapper.Map<PaymentResponse>(payment);
            response.Customer = _mapper.Map<CustomerSummary>(payment.Customer);
            response.Merchant = _mapper.Map<MerchantSummary>(payment.Merchant);

            _logger.LogInformation("Payment {PaymentId} cancelled: {Reason}", paymentId, reason);

            return ApiResponse<PaymentResponse>.SuccessResult(response, "Payment cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return ApiResponse<PaymentResponse>.ErrorResult("An error occurred while cancelling the payment", 500);
        }
    }

    public async Task<ApiResponse<RefundResponse>> CreateRefundAsync(CreateRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<RefundResponse>.ErrorResult("Payment not found", 404);
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return ApiResponse<RefundResponse>.ErrorResult("Only completed payments can be refunded", 400);
            }

            // Check if refund amount is valid
            var existingRefunds = await _context.PaymentRefunds
                .Where(r => r.PaymentId == request.PaymentId && r.Status == PaymentStatus.Completed)
                .SumAsync(r => r.Amount, cancellationToken);

            if (existingRefunds + request.Amount > payment.Amount)
            {
                return ApiResponse<RefundResponse>.ErrorResult("Refund amount exceeds available refund balance", 400);
            }

            var refund = new PaymentRefund
            {
                PaymentId = request.PaymentId,
                Amount = request.Amount,
                Currency = payment.Currency,
                Reason = request.Reason,
                Status = PaymentStatus.Pending,
                Metadata = request.Metadata
            };

            _context.PaymentRefunds.Add(refund);
            await _context.SaveChangesAsync(cancellationToken);

            // Simulate refund processing
            var refundResult = await SimulateRefundProcessingAsync(refund, cancellationToken);

            if (refundResult.Success)
            {
                refund.Status = PaymentStatus.Completed;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.RefundTransactionId = refundResult.TransactionId;

                // Update payment status if fully refunded
                if (existingRefunds + request.Amount >= payment.Amount)
                {
                    payment.Status = PaymentStatus.Refunded;
                    await UpdatePaymentStatusAsync(payment, PaymentStatus.Refunded, "Payment fully refunded", cancellationToken);
                }
                else
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                    await UpdatePaymentStatusAsync(payment, PaymentStatus.PartiallyRefunded, "Payment partially refunded", cancellationToken);
                }
            }
            else
            {
                refund.Status = PaymentStatus.Failed;
                refund.FailureReason = refundResult.FailureReason;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<RefundResponse>(refund);

            _logger.LogInformation("Refund {RefundId} created for payment {PaymentId}", refund.Id, request.PaymentId);

            return ApiResponse<RefundResponse>.SuccessResult(response, "Refund processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for payment {PaymentId}", request.PaymentId);
            return ApiResponse<RefundResponse>.ErrorResult("An error occurred while processing the refund", 500);
        }
    }

    public async Task<ApiResponse<PaymentAnalytics>> GetPaymentAnalyticsAsync(Guid merchantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _context.Payments
                .Where(p => p.MerchantId == merchantId && p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);

            var completedPayments = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();

            var analytics = new PaymentAnalytics
            {
                TotalVolume = completedPayments.Sum(p => p.Amount),
                TotalTransactions = payments.Count,
                AverageTransactionValue = completedPayments.Any() ? completedPayments.Average(p => p.Amount) : 0,
                SuccessRate = payments.Any() ? (decimal)completedPayments.Count / payments.Count * 100 : 0,
                StatusDistribution = payments.GroupBy(p => p.Status).ToDictionary(g => g.Key, g => g.Count()),
                VolumeByMethod = completedPayments.GroupBy(p => p.PaymentMethod).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
                VolumeByCurrency = completedPayments.GroupBy(p => p.Currency).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
                DailyVolume = completedPayments
                    .GroupBy(p => p.CreatedAt.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
            };

            // BNPL specific analytics
            var bnplPayments = await _context.BNPLPlans
                .Where(bp => bp.MerchantId == merchantId && bp.CreatedAt >= fromDate && bp.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);

            analytics.BNPLVolume = bnplPayments.Sum(bp => bp.TotalAmount);
            analytics.BNPLTransactions = bnplPayments.Count;
            analytics.BNPLPercentage = analytics.TotalTransactions > 0 ? (decimal)analytics.BNPLTransactions / analytics.TotalTransactions * 100 : 0;

            return ApiResponse<PaymentAnalytics>.SuccessResult(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment analytics for merchant {MerchantId}", merchantId);
            return ApiResponse<PaymentAnalytics>.ErrorResult("An error occurred while retrieving payment analytics", 500);
        }
    }

    #region Private Methods

    private async Task<(bool IsEligible, string Reason)> CheckBNPLEligibilityAsync(Customer customer, decimal amount, CancellationToken cancellationToken)
    {
        // Check customer risk level
        if (customer.RiskLevel >= RiskLevel.High)
        {
            return (false, "Customer risk level too high");
        }

        // Check credit rating
        if (customer.CreditRating == CreditRating.VeryPoor)
        {
            return (false, "Insufficient credit rating");
        }

        // Check available credit
        if (customer.AvailableCredit < amount)
        {
            return (false, "Insufficient available credit");
        }

        // Check existing BNPL plans
        var activePlans = await _context.BNPLPlans
            .CountAsync(bp => bp.CustomerId == customer.Id && bp.Status == PaymentStatus.Pending, cancellationToken);

        if (activePlans >= 5) // Max 5 active plans
        {
            return (false, "Maximum number of active BNPL plans reached");
        }

        // Check collection status
        if (customer.CollectionStatus != CollectionStatus.Current)
        {
            return (false, "Customer has overdue payments");
        }

        return (true, string.Empty);
    }

    private static decimal CalculatePaymentFees(decimal amount, decimal commissionRate, PaymentMethod paymentMethod)
    {
        var baseFee = amount * commissionRate;

        // Add method-specific fees
        var methodFee = paymentMethod switch
        {
            PaymentMethod.CreditCard => 2.50m,
            PaymentMethod.DebitCard => 1.50m,
            PaymentMethod.BankTransfer => 0.50m,
            PaymentMethod.BNPL => 0.00m, // No additional fee for BNPL
            _ => 1.00m
        };

        return baseFee + methodFee;
    }

    private static string GenerateTransactionId()
    {
        return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private async Task UpdatePaymentStatusAsync(Models.Payment payment, PaymentStatus newStatus, string reason, CancellationToken cancellationToken)
    {
        var oldStatus = payment.Status;
        payment.Status = newStatus;
        payment.UpdatedAt = DateTime.UtcNow;

        var paymentEvent = new PaymentEvent
        {
            PaymentId = payment.Id,
            EventType = $"Status{newStatus}",
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Description = reason,
            CreatedBy = "System"
        };

        _context.PaymentEvents.Add(paymentEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(bool Success, string? AuthorizationCode, string? GatewayTransactionId, string? FailureReason, string? ErrorCode, bool IsRetryable)> ProcessPaymentThroughGatewayAsync(Models.Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing payment {PaymentId} through gateway for method {PaymentMethod}", 
                payment.Id, payment.PaymentMethod);

            // Use the appropriate payment gateway based on payment method
            var gatewayResult = payment.PaymentMethod switch
            {
                PaymentMethod.CreditCard => await _paymentGatewayService.ProcessCreditCardPaymentAsync(payment, cancellationToken),
                PaymentMethod.DebitCard => await _paymentGatewayService.ProcessDebitCardPaymentAsync(payment, cancellationToken),
                PaymentMethod.BankTransfer => await _paymentGatewayService.ProcessBankTransferAsync(payment, cancellationToken),
                PaymentMethod.Vipps => await _paymentGatewayService.ProcessVippsPaymentAsync(payment, cancellationToken),
                PaymentMethod.Klarna => await _paymentGatewayService.ProcessKlarnaPaymentAsync(payment, cancellationToken),
                PaymentMethod.BNPL => await _paymentGatewayService.ProcessBNPLPaymentAsync(payment, cancellationToken),
                _ => throw new NotSupportedException($"Payment method {payment.PaymentMethod} is not supported")
            };

            if (gatewayResult.IsSuccess)
            {
                _logger.LogInformation("Payment {PaymentId} processed successfully. Gateway Transaction ID: {TransactionId}", 
                    payment.Id, gatewayResult.TransactionId);

                return (true, gatewayResult.AuthorizationCode, gatewayResult.TransactionId, null, null, false);
            }
            else
            {
                _logger.LogWarning("Payment {PaymentId} failed. Error: {ErrorCode} - {ErrorMessage}", 
                    payment.Id, gatewayResult.ErrorCode, gatewayResult.ErrorMessage);

                // Determine if the error is retryable
                var isRetryable = gatewayResult.ErrorCode switch
                {
                    "NETWORK_ERROR" => true,
                    "TIMEOUT" => true,
                    "TEMPORARY_UNAVAILABLE" => true,
                    "RATE_LIMIT_EXCEEDED" => true,
                    _ => false
                };

                return (false, null, null, gatewayResult.ErrorMessage, gatewayResult.ErrorCode, isRetryable);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while processing payment {PaymentId}", payment.Id);
            return (false, null, null, "Network error occurred", "NETWORK_ERROR", true);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while processing payment {PaymentId}", payment.Id);
            return (false, null, null, "Request timeout", "TIMEOUT", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing payment {PaymentId}", payment.Id);
            return (false, null, null, "Payment processing failed", "INTERNAL_ERROR", false);
        }
    }

    private async Task<(bool Success, string? TransactionId, string? FailureReason)> SimulateRefundProcessingAsync(PaymentRefund refund, CancellationToken cancellationToken)
    {
        // Simulate processing delay
        await Task.Delay(50, cancellationToken);

        // Simulate high success rate for refunds
        var random = new Random();
        if (random.NextDouble() < 0.98)
        {
            return (true, $"REF_{Guid.NewGuid().ToString("N")[..12].ToUpper()}", null);
        }
        else
        {
            return (false, null, "Refund processing failed");
        }
    }

    #endregion
}