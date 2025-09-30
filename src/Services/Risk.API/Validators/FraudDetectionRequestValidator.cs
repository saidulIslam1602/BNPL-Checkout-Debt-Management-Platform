using FluentValidation;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Enums;
using System.Net;

namespace YourCompanyBNPL.Risk.API.Validators;

/// <summary>
/// Validator for fraud detection requests
/// </summary>
public class FraudDetectionRequestValidator : AbstractValidator<FraudDetectionRequest>
{
    public FraudDetectionRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required")
            .MaximumLength(100)
            .WithMessage("Transaction ID cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("Transaction ID contains invalid characters");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP address is required")
            .Must(BeValidIpAddress)
            .WithMessage("Invalid IP address format");

        RuleFor(x => x.UserAgent)
            .MaximumLength(500)
            .WithMessage("User agent cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));

        RuleFor(x => x.DeviceFingerprint)
            .MaximumLength(100)
            .WithMessage("Device fingerprint cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .When(x => !string.IsNullOrEmpty(x.DeviceFingerprint))
            .WithMessage("Device fingerprint contains invalid characters");

        RuleFor(x => x.CountryCode)
            .Length(2)
            .WithMessage("Country code must be exactly 2 characters")
            .Matches(@"^[A-Z]{2}$")
            .WithMessage("Country code must be uppercase letters")
            .When(x => !string.IsNullOrEmpty(x.CountryCode));

        RuleFor(x => x.TransactionAmount)
            .GreaterThan(0)
            .WithMessage("Transaction amount must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Transaction amount cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency")
            .Must(BeSupportedCurrency)
            .WithMessage("Currency not supported for fraud detection");

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveReasonableTransactionAmount)
            .WithMessage("Transaction amount is unusually high and requires additional verification")
            .WithName("TransactionAmount");

        RuleFor(x => x.UserAgent)
            .Must(NotBeSuspiciousUserAgent)
            .When(x => !string.IsNullOrEmpty(x.UserAgent))
            .WithMessage("User agent appears to be from an automated system")
            .WithName("UserAgent");
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return IPAddress.TryParse(ipAddress, out _);
    }

    private static bool BeSupportedCurrency(Currency currency)
    {
        return Enum.IsDefined(typeof(Currency), currency);
    }

    private static bool HaveReasonableTransactionAmount(FraudDetectionRequest request)
    {
        // Flag transactions over 100,000 as requiring additional verification
        return request.TransactionAmount <= 100000;
    }

    private static bool NotBeSuspiciousUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return true;

        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python", "java",
            "automated", "script", "headless", "phantom"
        };

        return !suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}