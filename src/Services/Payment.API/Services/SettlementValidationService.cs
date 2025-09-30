using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for comprehensive settlement validation and business rules
/// </summary>
public interface ISettlementValidationService
{
    Task<ValidationResult> ValidateSettlementBatchCreationAsync(CreateSettlementBatchRequest request, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateSettlementProcessingAsync(Guid settlementId, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateMerchantEligibilityAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateSettlementAmountAsync(decimal amount, Currency currency, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateBusinessRulesAsync(Settlement settlement, CancellationToken cancellationToken = default);
}

public class SettlementValidationService : ISettlementValidationService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<SettlementValidationService> _logger;
    private readonly IConfiguration _configuration;

    public SettlementValidationService(
        PaymentDbContext context,
        ILogger<SettlementValidationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ValidationResult> ValidateSettlementBatchCreationAsync(CreateSettlementBatchRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            // Basic validation
            if (request.MerchantId == Guid.Empty)
            {
                result.AddError("MerchantId", "Merchant ID is required");
            }

            if (request.FromDate >= request.ToDate)
            {
                result.AddError("DateRange", "FromDate must be before ToDate");
            }

            if (request.ToDate > DateTime.UtcNow.Date)
            {
                result.AddError("ToDate", "ToDate cannot be in the future");
            }

            var maxDateRange = _configuration.GetValue<int>("Settlement:MaxDateRangeDays", 90);
            if ((request.ToDate - request.FromDate).TotalDays > maxDateRange)
            {
                result.AddError("DateRange", $"Date range cannot exceed {maxDateRange} days");
            }

            if (request.MinimumAmount.HasValue && request.MinimumAmount.Value <= 0)
            {
                result.AddError("MinimumAmount", "Minimum amount must be greater than zero");
            }

            // Merchant validation
            var merchantValidation = await ValidateMerchantEligibilityAsync(request.MerchantId, cancellationToken);
            if (!merchantValidation.IsValid)
            {
                result.AddErrors(merchantValidation.Errors);
            }

            // Check for existing settlements in the date range
            var existingSettlements = await _context.Settlements
                .Where(s => s.MerchantId == request.MerchantId &&
                           s.SettlementDate >= request.FromDate &&
                           s.SettlementDate <= request.ToDate &&
                           s.Status != SettlementStatus.Cancelled)
                .CountAsync(cancellationToken);

            if (existingSettlements > 0)
            {
                result.AddWarning("ExistingSettlements", $"Found {existingSettlements} existing settlements in the date range");
            }

            // Check for eligible payments
            var eligiblePayments = await _context.Payments
                .Where(p => p.MerchantId == request.MerchantId &&
                           p.Status == PaymentStatus.Completed &&
                           p.ProcessedAt.HasValue &&
                           p.ProcessedAt.Value.Date >= request.FromDate.Date &&
                           p.ProcessedAt.Value.Date <= request.ToDate.Date &&
                           !_context.SettlementTransactions.Any(st => st.PaymentId == p.Id))
                .CountAsync(cancellationToken);

            if (eligiblePayments == 0)
            {
                result.AddError("NoEligiblePayments", "No eligible payments found for the specified date range");
            }

            // Validate minimum amount threshold
            if (request.MinimumAmount.HasValue && eligiblePayments > 0)
            {
                var totalAmount = await _context.Payments
                    .Where(p => p.MerchantId == request.MerchantId &&
                               p.Status == PaymentStatus.Completed &&
                               p.ProcessedAt.HasValue &&
                               p.ProcessedAt.Value.Date >= request.FromDate.Date &&
                               p.ProcessedAt.Value.Date <= request.ToDate.Date &&
                               !_context.SettlementTransactions.Any(st => st.PaymentId == p.Id))
                    .SumAsync(p => p.Amount - p.Fees, cancellationToken);

                if (totalAmount < request.MinimumAmount.Value)
                {
                    result.AddError("InsufficientAmount", $"Total settlement amount {totalAmount:C} is below minimum threshold {request.MinimumAmount.Value:C}");
                }
            }

            _logger.LogInformation("Settlement batch validation completed for merchant {MerchantId}: {IsValid}", 
                request.MerchantId, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating settlement batch creation for merchant {MerchantId}", request.MerchantId);
            result.AddError("ValidationError", "An error occurred during validation");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateSettlementProcessingAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            var settlement = await _context.Settlements
                .Include(s => s.Merchant)
                .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);

            if (settlement == null)
            {
                result.AddError("SettlementNotFound", "Settlement not found");
                return result;
            }

            // Status validation
            if (settlement.Status != SettlementStatus.Pending)
            {
                result.AddError("InvalidStatus", $"Settlement is not in pending status. Current status: {settlement.Status}");
            }

            // Merchant validation
            if (settlement.Merchant == null || !settlement.Merchant.IsActive)
            {
                result.AddError("InactiveMerchant", "Merchant is not active");
            }

            // Amount validation
            var amountValidation = await ValidateSettlementAmountAsync(settlement.NetAmount, settlement.Currency, cancellationToken);
            if (!amountValidation.IsValid)
            {
                result.AddErrors(amountValidation.Errors);
            }

            // Business rules validation
            var businessRulesValidation = await ValidateBusinessRulesAsync(settlement, cancellationToken);
            if (!businessRulesValidation.IsValid)
            {
                result.AddErrors(businessRulesValidation.Errors);
            }

            // Check for duplicate processing
            var processingSettlements = await _context.Settlements
                .Where(s => s.MerchantId == settlement.MerchantId &&
                           s.Status == SettlementStatus.Processing &&
                           s.Id != settlementId)
                .CountAsync(cancellationToken);

            if (processingSettlements > 0)
            {
                result.AddWarning("ConcurrentProcessing", $"Found {processingSettlements} other settlements currently processing for this merchant");
            }

            _logger.LogInformation("Settlement processing validation completed for settlement {SettlementId}: {IsValid}", 
                settlementId, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating settlement processing for settlement {SettlementId}", settlementId);
            result.AddError("ValidationError", "An error occurred during validation");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateMerchantEligibilityAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);

            if (merchant == null)
            {
                result.AddError("MerchantNotFound", "Merchant not found");
                return result;
            }

            // Basic eligibility checks
            if (!merchant.IsActive)
            {
                result.AddError("InactiveMerchant", "Merchant is not active");
            }

            if (!merchant.IsVerified)
            {
                result.AddError("UnverifiedMerchant", "Merchant is not verified");
            }

            // Check onboarding status
            if (!merchant.OnboardedAt.HasValue)
            {
                result.AddError("NotOnboarded", "Merchant is not fully onboarded");
            }
            else if (merchant.OnboardedAt.Value > DateTime.UtcNow.AddDays(-7))
            {
                result.AddWarning("RecentOnboarding", "Merchant was recently onboarded (less than 7 days ago)");
            }

            // Check for suspended or flagged status
            var suspensionCheck = await CheckMerchantSuspensionAsync(merchantId, cancellationToken);
            if (!suspensionCheck.IsValid)
            {
                result.AddErrors(suspensionCheck.Errors);
            }

            // Check settlement history
            var settlementHistory = await _context.Settlements
                .Where(s => s.MerchantId == merchantId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            var failureRate = settlementHistory.Count > 0 ? 
                (double)settlementHistory.Count(s => s.Status == SettlementStatus.Failed) / settlementHistory.Count : 0;

            var maxFailureRate = _configuration.GetValue<double>("Settlement:MaxFailureRate", 0.2);
            if (failureRate > maxFailureRate)
            {
                result.AddWarning("HighFailureRate", $"Merchant has a high settlement failure rate: {failureRate:P}");
            }

            _logger.LogInformation("Merchant eligibility validation completed for merchant {MerchantId}: {IsValid}", 
                merchantId, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating merchant eligibility for merchant {MerchantId}", merchantId);
            result.AddError("ValidationError", "An error occurred during validation");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateSettlementAmountAsync(decimal amount, Currency currency, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            // Minimum amount validation
            var minAmount = _configuration.GetValue<decimal>($"Settlement:MinAmount:{currency}", 0.01m);
            if (amount < minAmount)
            {
                result.AddError("AmountTooLow", $"Settlement amount {amount:C} is below minimum threshold {minAmount:C}");
            }

            // Maximum amount validation
            var maxAmount = _configuration.GetValue<decimal>($"Settlement:MaxAmount:{currency}", 1000000m);
            if (amount > maxAmount)
            {
                result.AddError("AmountTooHigh", $"Settlement amount {amount:C} exceeds maximum threshold {maxAmount:C}");
            }

            // Suspicious amount patterns
            if (await IsSuspiciousAmountAsync(amount, currency, cancellationToken))
            {
                result.AddWarning("SuspiciousAmount", "Settlement amount matches suspicious patterns");
            }

            _logger.LogDebug("Settlement amount validation completed for amount {Amount} {Currency}: {IsValid}", 
                amount, currency, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating settlement amount {Amount} {Currency}", amount, currency);
            result.AddError("ValidationError", "An error occurred during validation");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateBusinessRulesAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            // Rule 1: Settlement date cannot be too far in the past
            var maxPastDays = _configuration.GetValue<int>("Settlement:MaxPastDays", 30);
            if (settlement.SettlementDate < DateTime.UtcNow.Date.AddDays(-maxPastDays))
            {
                result.AddError("SettlementTooOld", $"Settlement date is more than {maxPastDays} days in the past");
            }

            // Rule 2: Check for weekend/holiday restrictions
            if (IsWeekendOrHoliday(settlement.SettlementDate))
            {
                var allowWeekends = _configuration.GetValue<bool>("Settlement:AllowWeekends", false);
                if (!allowWeekends)
                {
                    result.AddWarning("WeekendSettlement", "Settlement date falls on a weekend or holiday");
                }
            }

            // Rule 3: Validate fee calculations
            var expectedFees = await CalculateExpectedFeesAsync(settlement, cancellationToken);
            var feeVariance = Math.Abs(settlement.Fees - expectedFees) / Math.Max(expectedFees, 0.01m);
            var maxFeeVariance = _configuration.GetValue<decimal>("Settlement:MaxFeeVariance", 0.05m);

            if (feeVariance > maxFeeVariance)
            {
                result.AddWarning("FeeVariance", $"Settlement fees vary from expected by {feeVariance:P}");
            }

            // Rule 4: Check for rapid settlement patterns
            var recentSettlements = await _context.Settlements
                .Where(s => s.MerchantId == settlement.MerchantId &&
                           s.CreatedAt >= DateTime.UtcNow.AddHours(-24) &&
                           s.Id != settlement.Id)
                .CountAsync(cancellationToken);

            var maxDailySettlements = _configuration.GetValue<int>("Settlement:MaxDailySettlements", 10);
            if (recentSettlements >= maxDailySettlements)
            {
                result.AddWarning("HighFrequency", $"Merchant has created {recentSettlements} settlements in the last 24 hours");
            }

            // Rule 5: Validate transaction consistency
            var transactions = await _context.SettlementTransactions
                .Where(st => st.SettlementId == settlement.Id)
                .Include(st => st.Payment)
                .ToListAsync(cancellationToken);

            if (transactions.Any())
            {
                var calculatedGross = transactions.Sum(t => t.Amount);
                var calculatedFees = transactions.Sum(t => t.Fee);
                var calculatedNet = transactions.Sum(t => t.NetAmount);

                if (Math.Abs(settlement.GrossAmount - calculatedGross) > 0.01m)
                {
                    result.AddError("GrossAmountMismatch", "Settlement gross amount doesn't match transaction total");
                }

                if (Math.Abs(settlement.Fees - calculatedFees) > 0.01m)
                {
                    result.AddError("FeesMismatch", "Settlement fees don't match transaction total");
                }

                if (Math.Abs(settlement.NetAmount - calculatedNet) > 0.01m)
                {
                    result.AddError("NetAmountMismatch", "Settlement net amount doesn't match transaction total");
                }
            }

            _logger.LogInformation("Business rules validation completed for settlement {SettlementId}: {IsValid}", 
                settlement.Id, result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for settlement {SettlementId}", settlement.Id);
            result.AddError("ValidationError", "An error occurred during validation");
        }

        return result;
    }

    #region Private Helper Methods

    private async Task<ValidationResult> CheckMerchantSuspensionAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        // Check for recent failed settlements
        var recentFailures = await _context.Settlements
            .Where(s => s.MerchantId == merchantId &&
                       s.Status == SettlementStatus.Failed &&
                       s.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync(cancellationToken);

        var maxRecentFailures = _configuration.GetValue<int>("Settlement:MaxRecentFailures", 5);
        if (recentFailures >= maxRecentFailures)
        {
            result.AddError("TooManyFailures", $"Merchant has {recentFailures} failed settlements in the last 7 days");
        }

        // Check for fraud flags (if fraud detection is implemented)
        // This would integrate with a fraud detection service
        
        return result;
    }

    private async Task<bool> IsSuspiciousAmountAsync(decimal amount, Currency currency, CancellationToken cancellationToken)
    {
        // Check for round numbers that might indicate suspicious activity
        if (amount % 1000 == 0 && amount >= 10000)
        {
            return true;
        }

        // Check for amounts that are significantly higher than merchant's typical volume
        var avgAmount = await _context.Settlements
            .Where(s => s.Currency == currency &&
                       s.Status == SettlementStatus.Completed &&
                       s.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .AverageAsync(s => (decimal?)s.NetAmount, cancellationToken) ?? 0;

        if (avgAmount > 0 && amount > avgAmount * 10)
        {
            return true;
        }

        return false;
    }

    private bool IsWeekendOrHoliday(DateTime date)
    {
        // Check for weekends
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            return true;
        }

        // Check for Norwegian holidays (simplified)
        var norwegianHolidays = GetNorwegianHolidays(date.Year);
        return norwegianHolidays.Contains(date.Date);
    }

    private List<DateTime> GetNorwegianHolidays(int year)
    {
        var holidays = new List<DateTime>();

        // Fixed holidays
        holidays.Add(new DateTime(year, 1, 1));  // New Year's Day
        holidays.Add(new DateTime(year, 5, 1));  // Labour Day
        holidays.Add(new DateTime(year, 5, 17)); // Constitution Day
        holidays.Add(new DateTime(year, 12, 25)); // Christmas Day
        holidays.Add(new DateTime(year, 12, 26)); // Boxing Day

        // Easter-based holidays (simplified calculation)
        var easter = CalculateEaster(year);
        holidays.Add(easter.AddDays(-3)); // Maundy Thursday
        holidays.Add(easter.AddDays(-2)); // Good Friday
        holidays.Add(easter.AddDays(1));  // Easter Monday
        holidays.Add(easter.AddDays(39)); // Ascension Day
        holidays.Add(easter.AddDays(50)); // Whit Monday

        return holidays;
    }

    private DateTime CalculateEaster(int year)
    {
        // Simplified Easter calculation (Gregorian calendar)
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateTime(year, month, day);
    }

    private async Task<decimal> CalculateExpectedFeesAsync(Settlement settlement, CancellationToken cancellationToken)
    {
        // Get merchant's commission rate
        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.Id == settlement.MerchantId, cancellationToken);

        if (merchant == null)
        {
            return 0;
        }

        // Calculate expected fees based on commission rate
        return settlement.GrossAmount * merchant.CommissionRate;
    }

    #endregion
}

/// <summary>
/// Validation result with errors and warnings
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public Dictionary<string, List<string>> Errors { get; } = new();
    public Dictionary<string, List<string>> Warnings { get; } = new();

    public void AddError(string field, string message)
    {
        if (!Errors.ContainsKey(field))
        {
            Errors[field] = new List<string>();
        }
        Errors[field].Add(message);
    }

    public void AddWarning(string field, string message)
    {
        if (!Warnings.ContainsKey(field))
        {
            Warnings[field] = new List<string>();
        }
        Warnings[field].Add(message);
    }

    public void AddErrors(Dictionary<string, List<string>> errors)
    {
        foreach (var error in errors)
        {
            if (!Errors.ContainsKey(error.Key))
            {
                Errors[error.Key] = new List<string>();
            }
            Errors[error.Key].AddRange(error.Value);
        }
    }

    public void AddWarnings(Dictionary<string, List<string>> warnings)
    {
        foreach (var warning in warnings)
        {
            if (!Warnings.ContainsKey(warning.Key))
            {
                Warnings[warning.Key] = new List<string>();
            }
            Warnings[warning.Key].AddRange(warning.Value);
        }
    }

    public List<string> GetAllErrors()
    {
        return Errors.SelectMany(e => e.Value).ToList();
    }

    public List<string> GetAllWarnings()
    {
        return Warnings.SelectMany(w => w.Value).ToList();
    }
}