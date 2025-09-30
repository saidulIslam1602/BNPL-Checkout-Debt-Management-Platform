using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using MediatR;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Events.Payment;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Implementation of BNPL-specific operations
/// </summary>
public class BNPLService : IBNPLService
{
    private readonly PaymentDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<BNPLService> _logger;

    public BNPLService(
        PaymentDbContext context,
        IMapper mapper,
        IMediator mediator,
        ILogger<BNPLService> logger)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<BNPLCalculationResponse>> CalculateBNPLPlanAsync(BNPLCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating BNPL plan for amount {Amount}, plan type {PlanType}", request.Amount, request.PlanType);

            // Determine installment count
            var installmentCount = request.PlanType switch
            {
                BNPLPlanType.PayIn3 => 3,
                BNPLPlanType.PayIn4 => 4,
                BNPLPlanType.PayIn6 => 6,
                BNPLPlanType.PayIn12 => 12,
                BNPLPlanType.PayIn24 => 24,
                BNPLPlanType.Custom => request.CustomInstallmentCount ?? 4,
                _ => 4
            };

            // Validate installment count
            if (installmentCount < 2 || installmentCount > 24)
            {
                return ApiResponse<BNPLCalculationResponse>.ErrorResult("Invalid installment count. Must be between 2 and 24.", 400);
            }

            // Check customer eligibility if customer ID provided
            var isEligible = true;
            var ineligibilityReason = string.Empty;

            if (request.CustomerId.HasValue)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value, cancellationToken);

                if (customer != null)
                {
                    var eligibilityCheck = await CheckBNPLEligibilityAsync(customer, request.Amount, cancellationToken);
                    isEligible = eligibilityCheck.IsEligible;
                    ineligibilityReason = eligibilityCheck.Reason;
                }
            }

            // Calculate interest rate based on plan type and amount
            var interestRate = CalculateInterestRate(request.PlanType, request.Amount, installmentCount);

            // Calculate down payment
            var downPaymentAmount = request.DownPaymentAmount ?? CalculateDownPayment(request.Amount, request.PlanType);
            var financedAmount = request.Amount - downPaymentAmount;

            // Calculate total interest and fees
            var totalInterest = CalculateTotalInterest(financedAmount, interestRate, installmentCount);
            var totalFees = CalculateTotalFees(request.Amount, request.PlanType);
            var totalAmountWithInterest = financedAmount + totalInterest + totalFees;

            // Calculate installment amount
            var installmentAmount = Math.Round(totalAmountWithInterest / installmentCount, 2);

            // Adjust last installment for rounding differences
            var totalCalculated = installmentAmount * (installmentCount - 1);
            var lastInstallmentAmount = totalAmountWithInterest - totalCalculated;

            // Set first installment date
            var firstInstallmentDate = request.FirstInstallmentDate ?? DateTime.UtcNow.AddDays(30);

            // Generate installment schedule
            var installments = new List<InstallmentCalculation>();
            for (int i = 1; i <= installmentCount; i++)
            {
                var amount = i == installmentCount ? lastInstallmentAmount : installmentAmount;
                var principalAmount = Math.Round(financedAmount / installmentCount, 2);
                var interestAmount = Math.Round(totalInterest / installmentCount, 2);
                var feeAmount = Math.Round(totalFees / installmentCount, 2);

                // Adjust last installment for rounding
                if (i == installmentCount)
                {
                    var totalPrincipal = principalAmount * (installmentCount - 1);
                    var totalInterestCalc = interestAmount * (installmentCount - 1);
                    var totalFeesCalc = feeAmount * (installmentCount - 1);

                    principalAmount = financedAmount - totalPrincipal;
                    interestAmount = totalInterest - totalInterestCalc;
                    feeAmount = totalFees - totalFeesCalc;
                }

                installments.Add(new InstallmentCalculation
                {
                    InstallmentNumber = i,
                    Amount = amount,
                    PrincipalAmount = principalAmount,
                    InterestAmount = interestAmount,
                    FeeAmount = feeAmount,
                    DueDate = firstInstallmentDate.AddMonths(i - 1)
                });
            }

            var response = new BNPLCalculationResponse
            {
                TotalAmount = request.Amount,
                Currency = request.Currency,
                PlanType = request.PlanType,
                InstallmentCount = installmentCount,
                InstallmentAmount = installmentAmount,
                InterestRate = interestRate,
                TotalInterest = totalInterest,
                TotalFees = totalFees,
                DownPaymentAmount = downPaymentAmount,
                FirstInstallmentDate = firstInstallmentDate,
                Installments = installments,
                IsEligible = isEligible,
                IneligibilityReason = ineligibilityReason
            };

            _logger.LogInformation("BNPL plan calculated successfully. Installment amount: {InstallmentAmount}, Total interest: {TotalInterest}",
                installmentAmount, totalInterest);

            return ApiResponse<BNPLCalculationResponse>.SuccessResult(response, "BNPL plan calculated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating BNPL plan");
            return ApiResponse<BNPLCalculationResponse>.ErrorResult("An error occurred while calculating the BNPL plan", 500);
        }
    }

    public async Task<ApiResponse<BNPLPlanSummary>> CreateBNPLPlanAsync(Guid paymentId, BNPLCalculationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating BNPL plan for payment {PaymentId}", paymentId);

            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult("Payment not found", 404);
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult("BNPL plan can only be created for pending payments", 400);
            }

            // Check if BNPL plan already exists
            var existingPlan = await _context.BNPLPlans
                .FirstOrDefaultAsync(bp => bp.PaymentId == paymentId, cancellationToken);

            if (existingPlan != null)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult("BNPL plan already exists for this payment", 400);
            }

            // Calculate BNPL plan
            var calculationRequest = new BNPLCalculationRequest
            {
                Amount = payment.Amount,
                Currency = payment.Currency,
                PlanType = request.PlanType,
                CustomInstallmentCount = request.CustomInstallmentCount,
                DownPaymentAmount = request.DownPaymentAmount,
                FirstInstallmentDate = request.FirstInstallmentDate,
                CustomerId = payment.CustomerId
            };

            var calculationResult = await CalculateBNPLPlanAsync(calculationRequest, cancellationToken);
            if (!calculationResult.Success || calculationResult.Data == null)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult(calculationResult.Errors?.FirstOrDefault() ?? "Failed to calculate BNPL plan", 400);
            }

            var calculation = calculationResult.Data;

            if (!calculation.IsEligible)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult($"Customer not eligible for BNPL: {calculation.IneligibilityReason}", 400);
            }

            // Create BNPL plan
            var bnplPlan = new BNPLPlan
            {
                PaymentId = paymentId,
                CustomerId = payment.CustomerId,
                MerchantId = payment.MerchantId,
                TotalAmount = calculation.TotalAmount,
                Currency = calculation.Currency,
                PlanType = calculation.PlanType,
                InstallmentCount = calculation.InstallmentCount,
                InstallmentAmount = calculation.InstallmentAmount,
                FirstPaymentDate = calculation.FirstInstallmentDate,
                InterestRate = calculation.InterestRate,
                TotalInterest = calculation.TotalInterest,
                TotalFees = calculation.TotalFees,
                RemainingBalance = calculation.TotalAmount + calculation.TotalInterest + calculation.TotalFees - calculation.DownPaymentAmount,
                RemainingInstallments = calculation.InstallmentCount,
                Status = PaymentStatus.Pending
            };

            _context.BNPLPlans.Add(bnplPlan);
            await _context.SaveChangesAsync(cancellationToken);

            // Create installments
            var installments = new List<Installment>();
            foreach (var installmentCalc in calculation.Installments)
            {
                var installment = new Installment
                {
                    BNPLPlanId = bnplPlan.Id,
                    InstallmentNumber = installmentCalc.InstallmentNumber,
                    Amount = installmentCalc.Amount,
                    PrincipalAmount = installmentCalc.PrincipalAmount,
                    InterestAmount = installmentCalc.InterestAmount,
                    FeeAmount = installmentCalc.FeeAmount,
                    DueDate = installmentCalc.DueDate,
                    Status = PaymentStatus.Pending
                };

                installments.Add(installment);
            }

            _context.Installments.AddRange(installments);
            await _context.SaveChangesAsync(cancellationToken);

            // Update customer available credit
            if (payment.Customer != null)
            {
                payment.Customer.AvailableCredit -= bnplPlan.TotalAmount;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Publish BNPL plan created event
            var bnplPlanCreatedEvent = new BNPLPlanCreatedEvent
            {
                PlanId = bnplPlan.Id,
                PaymentId = paymentId,
                CustomerId = payment.CustomerId,
                MerchantId = payment.MerchantId,
                TotalAmount = bnplPlan.TotalAmount,
                Currency = bnplPlan.Currency,
                PlanType = bnplPlan.PlanType,
                InstallmentCount = bnplPlan.InstallmentCount,
                InstallmentAmount = bnplPlan.InstallmentAmount,
                FirstPaymentDate = bnplPlan.FirstPaymentDate,
                Installments = calculation.Installments.Select(i => new InstallmentInfo
                {
                    InstallmentNumber = i.InstallmentNumber,
                    Amount = i.Amount,
                    DueDate = i.DueDate,
                    PrincipalAmount = i.PrincipalAmount,
                    InterestAmount = i.InterestAmount,
                    FeeAmount = i.FeeAmount
                }).ToList(),
                InterestRate = bnplPlan.InterestRate,
                TotalInterest = bnplPlan.TotalInterest,
                AggregateId = bnplPlan.Id,
                UserId = payment.CustomerId.ToString()
            };

            await _mediator.Publish(bnplPlanCreatedEvent, cancellationToken);

            // Load the plan with installments for response
            var planWithInstallments = await _context.BNPLPlans
                .Include(bp => bp.Installments)
                .FirstAsync(bp => bp.Id == bnplPlan.Id, cancellationToken);

            var response = _mapper.Map<BNPLPlanSummary>(planWithInstallments);

            _logger.LogInformation("BNPL plan {PlanId} created successfully for payment {PaymentId}", bnplPlan.Id, paymentId);

            return ApiResponse<BNPLPlanSummary>.SuccessResult(response, "BNPL plan created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BNPL plan for payment {PaymentId}", paymentId);
            return ApiResponse<BNPLPlanSummary>.ErrorResult("An error occurred while creating the BNPL plan", 500);
        }
    }

    public async Task<ApiResponse<BNPLPlanSummary>> GetBNPLPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await _context.BNPLPlans
                .Include(bp => bp.Installments.OrderBy(i => i.InstallmentNumber))
                .Include(bp => bp.Customer)
                .Include(bp => bp.Merchant)
                .FirstOrDefaultAsync(bp => bp.Id == planId, cancellationToken);

            if (plan == null)
            {
                return ApiResponse<BNPLPlanSummary>.ErrorResult("BNPL plan not found", 404);
            }

            var response = _mapper.Map<BNPLPlanSummary>(plan);
            return ApiResponse<BNPLPlanSummary>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BNPL plan {PlanId}", planId);
            return ApiResponse<BNPLPlanSummary>.ErrorResult("An error occurred while retrieving the BNPL plan", 500);
        }
    }

    public async Task<PagedApiResponse<BNPLPlanSummary>> GetCustomerBNPLPlansAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.BNPLPlans
                .Include(bp => bp.Installments.OrderBy(i => i.InstallmentNumber))
                .Where(bp => bp.CustomerId == customerId)
                .OrderByDescending(bp => bp.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var plans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = plans.Select(p => _mapper.Map<BNPLPlanSummary>(p)).ToList();

            return PagedApiResponse<BNPLPlanSummary>.SuccessResult(responses, page, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving BNPL plans for customer {CustomerId}", customerId);
            return new PagedApiResponse<BNPLPlanSummary>
            {
                Success = false,
                Errors = new List<string> { "An error occurred while retrieving BNPL plans" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse<InstallmentResponse>> ProcessInstallmentAsync(ProcessInstallmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing installment {InstallmentId}", request.InstallmentId);

            var installment = await _context.Installments
                .Include(i => i.BNPLPlan)
                    .ThenInclude(bp => bp!.Customer)
                .FirstOrDefaultAsync(i => i.Id == request.InstallmentId, cancellationToken);

            if (installment == null)
            {
                return ApiResponse<InstallmentResponse>.ErrorResult("Installment not found", 404);
            }

            if (installment.Status != PaymentStatus.Pending)
            {
                return ApiResponse<InstallmentResponse>.ErrorResult($"Installment cannot be processed. Current status: {installment.Status}", 400);
            }

            // Check if installment is overdue and add late fees
            if (installment.DueDate < DateTime.UtcNow.Date && !installment.IsOverdue)
            {
                installment.IsOverdue = true;
                installment.DaysPastDue = (DateTime.UtcNow.Date - installment.DueDate.Date).Days;
                installment.LateFee = CalculateLateFee(installment.Amount, installment.DaysPastDue);
            }

            // Simulate installment payment processing
            var processingResult = await SimulateInstallmentProcessingAsync(installment, cancellationToken);

            if (processingResult.Success)
            {
                installment.Status = PaymentStatus.Completed;
                installment.PaidAt = DateTime.UtcNow;
                installment.TransactionId = processingResult.TransactionId;
                installment.PaymentMethod = request.PaymentMethod;

                // Update BNPL plan
                if (installment.BNPLPlan != null)
                {
                    installment.BNPLPlan.RemainingBalance -= installment.Amount;
                    installment.BNPLPlan.RemainingInstallments--;

                    if (installment.BNPLPlan.RemainingInstallments == 0)
                    {
                        installment.BNPLPlan.Status = PaymentStatus.Completed;
                        installment.BNPLPlan.CompletedAt = DateTime.UtcNow;

                        // Restore customer available credit
                        if (installment.BNPLPlan.Customer != null)
                        {
                            installment.BNPLPlan.Customer.AvailableCredit += installment.BNPLPlan.TotalAmount;
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Publish installment paid event
                var installmentPaidEvent = new InstallmentPaidEvent
                {
                    InstallmentId = installment.Id,
                    PlanId = installment.BNPLPlanId,
                    CustomerId = installment.BNPLPlan!.CustomerId,
                    Amount = installment.Amount,
                    Currency = installment.BNPLPlan.Currency,
                    PaidAt = installment.PaidAt.Value,
                    PaymentMethod = installment.PaymentMethod ?? PaymentMethod.BankTransfer,
                    TransactionId = installment.TransactionId ?? string.Empty,
                    InstallmentNumber = installment.InstallmentNumber,
                    RemainingInstallments = installment.BNPLPlan.RemainingInstallments,
                    RemainingBalance = installment.BNPLPlan.RemainingBalance,
                    AggregateId = installment.BNPLPlan.Id,
                    UserId = installment.BNPLPlan.CustomerId.ToString()
                };

                await _mediator.Publish(installmentPaidEvent, cancellationToken);

                _logger.LogInformation("Installment {InstallmentId} processed successfully", request.InstallmentId);
            }
            else
            {
                installment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Installment {InstallmentId} processing failed: {FailureReason}", 
                    request.InstallmentId, processingResult.FailureReason);
            }

            var response = _mapper.Map<InstallmentResponse>(installment);
            return ApiResponse<InstallmentResponse>.SuccessResult(response, "Installment processing completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing installment {InstallmentId}", request.InstallmentId);
            return ApiResponse<InstallmentResponse>.ErrorResult("An error occurred while processing the installment", 500);
        }
    }

    public async Task<PagedApiResponse<InstallmentResponse>> GetOverdueInstallmentsAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Installments
                .Include(i => i.BNPLPlan)
                .Where(i => i.Status == PaymentStatus.Pending && i.DueDate < DateTime.UtcNow.Date)
                .OrderBy(i => i.DueDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var installments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = installments.Select(i => _mapper.Map<InstallmentResponse>(i)).ToList();

            return PagedApiResponse<InstallmentResponse>.SuccessResult(responses, page, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue installments");
            return new PagedApiResponse<InstallmentResponse>
            {
                Success = false,
                Errors = new List<string> { "An error occurred while retrieving overdue installments" },
                StatusCode = 500
            };
        }
    }

    public async Task<ApiResponse> ProcessOverdueInstallmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing overdue installments");

            var overdueInstallments = await _context.Installments
                .Include(i => i.BNPLPlan)
                    .ThenInclude(bp => bp!.Customer)
                .Where(i => i.Status == PaymentStatus.Pending && 
                           i.DueDate < DateTime.UtcNow.Date && 
                           !i.IsOverdue)
                .ToListAsync(cancellationToken);

            var processedCount = 0;

            foreach (var installment in overdueInstallments)
            {
                installment.IsOverdue = true;
                installment.DaysPastDue = (DateTime.UtcNow.Date - installment.DueDate.Date).Days;
                installment.LateFee = CalculateLateFee(installment.Amount, installment.DaysPastDue);

                // Update customer collection status based on days past due
                if (installment.BNPLPlan?.Customer != null)
                {
                    var customer = installment.BNPLPlan.Customer;
                    customer.CollectionStatus = installment.DaysPastDue switch
                    {
                        <= 30 => CollectionStatus.EarlyDelinquency,
                        <= 60 => CollectionStatus.Delinquent,
                        <= 90 => CollectionStatus.LateDelinquent,
                        _ => CollectionStatus.ChargeOff
                    };
                }

                // Publish installment due event for notifications
                var installmentDueEvent = new InstallmentDueEvent
                {
                    InstallmentId = installment.Id,
                    PlanId = installment.BNPLPlanId,
                    CustomerId = installment.BNPLPlan!.CustomerId,
                    Amount = installment.Amount + installment.LateFee,
                    Currency = installment.BNPLPlan.Currency,
                    DueDate = installment.DueDate,
                    InstallmentNumber = installment.InstallmentNumber,
                    TotalInstallments = installment.BNPLPlan.InstallmentCount,
                    IsOverdue = true,
                    DaysPastDue = installment.DaysPastDue,
                    AggregateId = installment.BNPLPlan.Id,
                    UserId = installment.BNPLPlan.CustomerId.ToString()
                };

                await _mediator.Publish(installmentDueEvent, cancellationToken);
                processedCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processed {Count} overdue installments", processedCount);

            return ApiResponse.SuccessResponse($"Processed {processedCount} overdue installments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue installments");
            return ApiResponse.ErrorResult("An error occurred while processing overdue installments", 500);
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

    private static decimal CalculateInterestRate(BNPLPlanType planType, decimal amount, int installmentCount)
    {
        // Dynamic base interest rate calculation based on plan duration and risk
        var baseRate = CalculateBaseInterestRate(planType, installmentCount);
        
        // Dynamic rate adjustment based on amount tiers and risk factors
        var amountMultiplier = CalculateAmountBasedMultiplier(amount);
        var riskMultiplier = CalculateRiskBasedMultiplier(amount, installmentCount);
        
        return Math.Round(baseRate * amountMultiplier * riskMultiplier, 4);
    }

    private static decimal CalculateBaseInterestRate(BNPLPlanType planType, int installmentCount)
    {
        // Calculate base rate based on plan duration and Norwegian market conditions
        var durationMonths = planType switch
        {
            BNPLPlanType.PayIn3 => 3,
            BNPLPlanType.PayIn4 => 4,
            BNPLPlanType.PayIn6 => 6,
            BNPLPlanType.PayIn12 => 12,
            BNPLPlanType.PayIn24 => 24,
            BNPLPlanType.Custom => installmentCount,
            _ => 12
        };

        // Norwegian BNPL market rates: 0% for short-term, progressive for longer terms
        return durationMonths switch
        {
            <= 4 => 0.00m,  // No interest for short-term plans
            <= 6 => 0.05m,  // 5% APR for 6-month plans
            <= 12 => 0.12m, // 12% APR for 12-month plans
            _ => 0.18m      // 18% APR for 24-month plans
        };
    }

    private static decimal CalculateAmountBasedMultiplier(decimal amount)
    {
        // Dynamic rate adjustment based on transaction amount
        return amount switch
        {
            >= 100000 => 0.7m,  // 30% discount for high-value transactions
            >= 50000 => 0.8m,   // 20% discount for medium-high value
            >= 20000 => 0.9m,   // 10% discount for medium value
            >= 5000 => 1.0m,    // Standard rate
            _ => 1.1m           // 10% premium for small transactions
        };
    }

    private static decimal CalculateRiskBasedMultiplier(decimal amount, int installmentCount)
    {
        // Risk-based adjustment considering amount-to-duration ratio
        var riskRatio = amount / (installmentCount * 1000); // Amount per month per 1000 NOK
        
        return riskRatio switch
        {
            <= 1 => 0.9m,   // Low risk: 10% discount
            <= 3 => 1.0m,   // Standard risk
            <= 5 => 1.1m,   // Higher risk: 10% premium
            _ => 1.2m       // High risk: 20% premium
        };
    }

    private static decimal CalculateDownPayment(decimal amount, BNPLPlanType planType)
    {
        // Dynamic down payment calculation based on plan type and amount
        var basePercentage = CalculateBaseDownPaymentPercentage(planType);
        var amountAdjustment = CalculateAmountBasedDownPaymentAdjustment(amount);
        var finalPercentage = Math.Max(0.05m, Math.Min(0.5m, basePercentage * amountAdjustment));

        return Math.Round(amount * finalPercentage, 2);
    }

    private static decimal CalculateBaseDownPaymentPercentage(BNPLPlanType planType)
    {
        // Base down payment percentage based on plan duration
        return planType switch
        {
            BNPLPlanType.PayIn3 => 0.33m,  // 33% down for 3 installments
            BNPLPlanType.PayIn4 => 0.25m,  // 25% down for 4 installments
            BNPLPlanType.PayIn6 => 0.20m,  // 20% down for 6 installments
            BNPLPlanType.PayIn12 => 0.15m, // 15% down for 12 installments
            BNPLPlanType.PayIn24 => 0.10m, // 10% down for 24 installments
            _ => 0.25m
        };
    }

    private static decimal CalculateAmountBasedDownPaymentAdjustment(decimal amount)
    {
        // Adjust down payment based on transaction amount
        return amount switch
        {
            >= 100000 => 0.8m,  // 20% reduction for high-value transactions
            >= 50000 => 0.9m,   // 10% reduction for medium-high value
            >= 20000 => 1.0m,   // Standard rate
            >= 5000 => 1.1m,    // 10% increase for medium value
            _ => 1.2m           // 20% increase for small transactions
        };
    }

    private static decimal CalculateTotalInterest(decimal principalAmount, decimal annualRate, int installmentCount)
    {
        if (annualRate == 0)
            return 0;

        // Simple interest calculation for BNPL
        var monthlyRate = annualRate / 12;
        var totalInterest = principalAmount * monthlyRate * installmentCount;

        return Math.Round(totalInterest, 2);
    }

    private static decimal CalculateTotalFees(decimal amount, BNPLPlanType planType)
    {
        // Dynamic fee calculation based on plan type and amount
        var baseFee = CalculateBaseFee(planType);
        var amountBasedFee = CalculateAmountBasedFee(amount);
        var totalFee = baseFee + amountBasedFee;

        // Cap fees at 2% of amount to ensure affordability
        var maxFee = amount * 0.02m;
        return Math.Min(totalFee, maxFee);
    }

    private static decimal CalculateBaseFee(BNPLPlanType planType)
    {
        // Base fee structure for different plan types
        return planType switch
        {
            BNPLPlanType.PayIn3 => 0.00m,   // No fees for short-term plans
            BNPLPlanType.PayIn4 => 0.00m,   // No fees for short-term plans
            BNPLPlanType.PayIn6 => 15.00m,  // Reduced fee for medium-term plans
            BNPLPlanType.PayIn12 => 30.00m, // Moderate fee for long-term plans
            BNPLPlanType.PayIn24 => 60.00m, // Higher fee for extended plans
            _ => 20.00m
        };
    }

    private static decimal CalculateAmountBasedFee(decimal amount)
    {
        // Additional fee based on transaction amount (0.1% of amount)
        var percentageFee = amount * 0.001m;
        
        // Apply minimum and maximum caps
        return Math.Max(5.00m, Math.Min(percentageFee, 50.00m));
    }

    private static decimal CalculateLateFee(decimal installmentAmount, int daysPastDue)
    {
        // Progressive late fee structure
        var lateFeePercentage = daysPastDue switch
        {
            <= 7 => 0.00m,   // Grace period
            <= 15 => 0.02m,  // 2% after 7 days
            <= 30 => 0.05m,  // 5% after 15 days
            _ => 0.10m        // 10% after 30 days
        };

        var lateFee = installmentAmount * lateFeePercentage;
        
        // Cap late fee at 100 NOK
        return Math.Min(lateFee, 100.00m);
    }

    private async Task<(bool Success, string? TransactionId, string? FailureReason)> SimulateInstallmentProcessingAsync(Installment installment, CancellationToken cancellationToken)
    {
        // Simulate processing delay
        await Task.Delay(50, cancellationToken);

        // Simulate high success rate for installment payments (95%)
        var random = new Random();
        if (random.NextDouble() < 0.95)
        {
            return (true, $"INST_{Guid.NewGuid().ToString("N")[..12].ToUpper()}", null);
        }
        else
        {
            var failureReasons = new[] { "Insufficient funds", "Account closed", "Payment declined" };
            var failureReason = failureReasons[random.Next(failureReasons.Length)];
            return (false, null, failureReason);
        }
    }

    public async Task<ApiResponse<InstallmentResponse>> GetInstallmentAsync(Guid installmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting installment {InstallmentId}", installmentId);

            var installment = await _context.BNPLInstallments
                .Include(i => i.BNPLPlan)
                    .ThenInclude(p => p.Payment)
                        .ThenInclude(p => p.Customer)
                .Include(i => i.BNPLPlan)
                    .ThenInclude(p => p.Payment)
                        .ThenInclude(p => p.Merchant)
                .FirstOrDefaultAsync(i => i.Id == installmentId, cancellationToken);

            if (installment == null)
            {
                _logger.LogWarning("Installment {InstallmentId} not found", installmentId);
                return ApiResponse<InstallmentResponse>.ErrorResult("Installment not found", 404);
            }

            var response = _mapper.Map<InstallmentResponse>(installment);
            
            _logger.LogInformation("Successfully retrieved installment {InstallmentId}", installmentId);
            return ApiResponse<InstallmentResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installment {InstallmentId}", installmentId);
            return ApiResponse<InstallmentResponse>.ErrorResult("Failed to get installment", 500);
        }
    }

    #endregion
}