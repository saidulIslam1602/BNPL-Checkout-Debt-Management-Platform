using FluentValidation;
using RivertyBNPL.Payment.API.DTOs;
using RivertyBNPL.Common.Enums;
using RivertyBNPL.Common.Constants;

namespace RivertyBNPL.Payment.API.Validators;

/// <summary>
/// Validator for CreatePaymentRequest
/// </summary>
public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(ValidationConstants.MinPaymentAmount)
            .LessThanOrEqualTo(ValidationConstants.MaxPaymentAmount)
            .WithMessage($"Amount must be between {ValidationConstants.MinPaymentAmount} and {ValidationConstants.MaxPaymentAmount}");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method");

        RuleFor(x => x.OrderReference)
            .MaximumLength(100)
            .WithMessage("Order reference cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        // BNPL specific validations
        When(x => x.EnableBNPL, () =>
        {
            RuleFor(x => x.BNPLPlanType)
                .NotNull()
                .WithMessage("BNPL plan type is required when BNPL is enabled");

            RuleFor(x => x.InstallmentCount)
                .GreaterThanOrEqualTo(ValidationConstants.MinInstallmentCount)
                .LessThanOrEqualTo(ValidationConstants.MaxInstallmentCount)
                .When(x => x.BNPLPlanType == BNPLPlanType.Custom)
                .WithMessage($"Installment count must be between {ValidationConstants.MinInstallmentCount} and {ValidationConstants.MaxInstallmentCount}");

            RuleFor(x => x.DownPaymentAmount)
                .GreaterThanOrEqualTo(0)
                .LessThan(x => x.Amount)
                .When(x => x.DownPaymentAmount.HasValue)
                .WithMessage("Down payment amount must be less than total amount");

            RuleFor(x => x.FirstInstallmentDate)
                .GreaterThan(DateTime.UtcNow.Date)
                .When(x => x.FirstInstallmentDate.HasValue)
                .WithMessage("First installment date must be in the future");
        });
    }
}

/// <summary>
/// Validator for BNPLCalculationRequest
/// </summary>
public class BNPLCalculationRequestValidator : AbstractValidator<BNPLCalculationRequest>
{
    public BNPLCalculationRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(ValidationConstants.MinPaymentAmount)
            .LessThanOrEqualTo(ValidationConstants.MaxPaymentAmount)
            .WithMessage($"Amount must be between {ValidationConstants.MinPaymentAmount} and {ValidationConstants.MaxPaymentAmount}");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency");

        RuleFor(x => x.PlanType)
            .IsInEnum()
            .WithMessage("Invalid BNPL plan type");

        RuleFor(x => x.CustomInstallmentCount)
            .GreaterThanOrEqualTo(ValidationConstants.MinInstallmentCount)
            .LessThanOrEqualTo(ValidationConstants.MaxInstallmentCount)
            .When(x => x.PlanType == BNPLPlanType.Custom && x.CustomInstallmentCount.HasValue)
            .WithMessage($"Custom installment count must be between {ValidationConstants.MinInstallmentCount} and {ValidationConstants.MaxInstallmentCount}");

        RuleFor(x => x.DownPaymentAmount)
            .GreaterThanOrEqualTo(0)
            .LessThan(x => x.Amount)
            .When(x => x.DownPaymentAmount.HasValue)
            .WithMessage("Down payment amount must be less than total amount");

        RuleFor(x => x.FirstInstallmentDate)
            .GreaterThan(DateTime.UtcNow.Date)
            .When(x => x.FirstInstallmentDate.HasValue)
            .WithMessage("First installment date must be in the future");
    }
}

/// <summary>
/// Validator for CreateRefundRequest
/// </summary>
public class CreateRefundRequestValidator : AbstractValidator<CreateRefundRequest>
{
    public CreateRefundRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(ValidationConstants.MaxPaymentAmount)
            .WithMessage($"Refund amount must be between 0 and {ValidationConstants.MaxPaymentAmount}");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Refund reason is required and cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for ProcessInstallmentRequest
/// </summary>
public class ProcessInstallmentRequestValidator : AbstractValidator<ProcessInstallmentRequest>
{
    public ProcessInstallmentRequestValidator()
    {
        RuleFor(x => x.InstallmentId)
            .NotEmpty()
            .WithMessage("Installment ID is required");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .When(x => x.PaymentMethod.HasValue)
            .WithMessage("Invalid payment method");
    }
}

/// <summary>
/// Validator for PaymentSearchRequest
/// </summary>
public class PaymentSearchRequestValidator : AbstractValidator<PaymentSearchRequest>
{
    public PaymentSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(ValidationConstants.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {ValidationConstants.MaxPageSize}");

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount.HasValue)
            .WithMessage("Minimum amount must be greater than or equal to 0");

        RuleFor(x => x.MaxAmount)
            .GreaterThan(x => x.MinAmount)
            .When(x => x.MaxAmount.HasValue && x.MinAmount.HasValue)
            .WithMessage("Maximum amount must be greater than minimum amount");

        RuleFor(x => x.FromDate)
            .LessThan(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be before to date");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.ToDate.HasValue)
            .WithMessage("To date cannot be in the future");

        RuleFor(x => x.OrderReference)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.OrderReference))
            .WithMessage("Order reference cannot exceed 100 characters");

        RuleFor(x => x.TransactionId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.TransactionId))
            .WithMessage("Transaction ID cannot exceed 100 characters");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrEmpty(x.SortBy))
            .WithMessage("Invalid sort field");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validSortFields = new[] { "createdAt", "amount", "status", "processedAt", "customerId", "merchantId" };
        return validSortFields.Contains(sortBy.ToLower());
    }
}