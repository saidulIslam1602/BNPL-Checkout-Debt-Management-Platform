using FluentValidation;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Risk.API.Validators;

/// <summary>
/// Validator for credit assessment requests
/// </summary>
public class CreditAssessmentRequestValidator : AbstractValidator<CreditAssessmentRequest>
{
    public CreditAssessmentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.SocialSecurityNumber)
            .NotEmpty()
            .WithMessage("Social Security Number is required")
            .Length(11)
            .WithMessage("Norwegian SSN must be exactly 11 digits")
            .Matches(@"^\d{11}$")
            .WithMessage("SSN must contain only digits")
            .Must(BeValidNorwegianSSN)
            .WithMessage("Invalid Norwegian Social Security Number");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZæøåÆØÅ\s\-']+$")
            .WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZæøåÆØÅ\s\-']+$")
            .WithMessage("Last name contains invalid characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Date of birth is required")
            .Must(BeValidAge)
            .WithMessage("Customer must be between 18 and 100 years old");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(254)
            .WithMessage("Email cannot exceed 254 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+47)?[0-9]{8}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid Norwegian phone number format");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0)
            .WithMessage("Requested amount must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Requested amount cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency")
            .Must(BeSupportedCurrency)
            .WithMessage("Currency not supported for Norwegian market");

        RuleFor(x => x.PlanType)
            .IsInEnum()
            .WithMessage("Invalid BNPL plan type");

        RuleFor(x => x.AnnualIncome)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Annual income cannot be negative")
            .LessThanOrEqualTo(10000000)
            .WithMessage("Annual income seems unrealistic");

        RuleFor(x => x.MonthlyExpenses)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Monthly expenses cannot be negative")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Monthly expenses seem unrealistic");

        RuleFor(x => x.ExistingDebt)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Existing debt cannot be negative")
            .LessThanOrEqualTo(10000000)
            .WithMessage("Existing debt seems unrealistic");

        RuleFor(x => x.ExistingCreditAccounts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Number of credit accounts cannot be negative")
            .LessThanOrEqualTo(50)
            .WithMessage("Number of credit accounts seems unrealistic");

        RuleFor(x => x.PaymentHistoryMonths)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Payment history months cannot be negative")
            .LessThanOrEqualTo(600)
            .WithMessage("Payment history months seems unrealistic");

        RuleFor(x => x.LatePaymentsLast12Months)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Late payments count cannot be negative")
            .LessThanOrEqualTo(12)
            .WithMessage("Late payments cannot exceed 12 in a 12-month period");

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveReasonableDebtToIncomeRatio)
            .WithMessage("Debt-to-income ratio is too high for assessment")
            .WithName("DebtToIncomeRatio");

        RuleFor(x => x)
            .Must(HaveReasonableRequestedAmountToIncome)
            .WithMessage("Requested amount is too high relative to annual income")
            .WithName("RequestedAmountToIncome");
    }

    private static bool BeValidNorwegianSSN(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn) || ssn.Length != 11 || !ssn.All(char.IsDigit))
            return false;

        var digits = ssn.Select(c => int.Parse(c.ToString())).ToArray();
        
        // First control digit
        var sum1 = (3 * digits[0]) + (7 * digits[1]) + (6 * digits[2]) + (1 * digits[3]) + 
                   (8 * digits[4]) + (9 * digits[5]) + (4 * digits[6]) + (5 * digits[7]) + (2 * digits[8]);
        var control1 = 11 - (sum1 % 11);
        if (control1 == 11) control1 = 0;
        if (control1 == 10) return false;
        if (control1 != digits[9]) return false;

        // Second control digit
        var sum2 = (5 * digits[0]) + (4 * digits[1]) + (3 * digits[2]) + (2 * digits[3]) + 
                   (7 * digits[4]) + (6 * digits[5]) + (5 * digits[6]) + (4 * digits[7]) + 
                   (3 * digits[8]) + (2 * digits[9]);
        var control2 = 11 - (sum2 % 11);
        if (control2 == 11) control2 = 0;
        if (control2 == 10) return false;
        if (control2 != digits[10]) return false;

        return true;
    }

    private static bool BeValidAge(DateTime dateOfBirth)
    {
        var age = DateTime.Today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        return age >= 18 && age <= 100;
    }

    private static bool BeSupportedCurrency(Currency currency)
    {
        return currency == Currency.NOK; // Only Norwegian Kroner supported
    }

    private static bool HaveReasonableDebtToIncomeRatio(CreditAssessmentRequest request)
    {
        if (request.AnnualIncome <= 0) return true; // Will be caught by other validators
        
        var totalDebt = request.ExistingDebt + request.RequestedAmount;
        var debtToIncomeRatio = totalDebt / request.AnnualIncome;
        
        return debtToIncomeRatio <= 0.8m; // Max 80% debt-to-income ratio
    }

    private static bool HaveReasonableRequestedAmountToIncome(CreditAssessmentRequest request)
    {
        if (request.AnnualIncome <= 0) return true; // Will be caught by other validators
        
        var requestedAmountToIncomeRatio = request.RequestedAmount / request.AnnualIncome;
        
        return requestedAmountToIncomeRatio <= 0.5m; // Max 50% of annual income
    }
}