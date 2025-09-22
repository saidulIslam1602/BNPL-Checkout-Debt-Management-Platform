namespace RivertyBNPL.Common.Constants;

/// <summary>
/// Validation constants for consistent validation across the platform
/// </summary>
public static class ValidationConstants
{
    // General validation
    public const int MaxStringLength = 500;
    public const int MaxDescriptionLength = 2000;
    public const int MaxEmailLength = 254;
    public const int MaxPhoneLength = 20;
    public const int MaxNameLength = 100;
    
    // Financial validation
    public const decimal MinPaymentAmount = 0.01m;
    public const decimal MaxPaymentAmount = 1000000.00m;
    public const decimal MinInstallmentAmount = 1.00m;
    public const int MinInstallmentCount = 2;
    public const int MaxInstallmentCount = 24;
    
    // Credit and risk validation
    public const int MinCreditScore = 300;
    public const int MaxCreditScore = 850;
    public const decimal MinIncomeAmount = 0.00m;
    public const decimal MaxIncomeAmount = 10000000.00m;
    
    // Date validation
    public static readonly DateTime MinDate = new(1900, 1, 1);
    public static readonly DateTime MaxDate = new(2100, 12, 31);
    
    // Business rules
    public const int MaxPaymentPlansPerCustomer = 10;
    public const int PaymentReminderDaysBeforeDue = 3;
    public const int PaymentOverdueDays = 1;
    public const int MaxRetryAttempts = 3;
    
    // Regular expressions
    public const string EmailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string PhoneRegex = @"^\+?[1-9]\d{1,14}$";
    public const string NorwegianPhoneRegex = @"^(\+47|0047|47)?[2-9]\d{7}$";
    public const string NorwegianSSNRegex = @"^\d{11}$";
    public const string IBANRegex = @"^[A-Z]{2}\d{2}[A-Z0-9]{4}\d{7}([A-Z0-9]?){0,16}$";
    
    // Currency formatting
    public const string CurrencyFormat = "F2";
    public const string PercentageFormat = "P2";
    
    // API validation
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;
    
    // Security
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int TokenExpiryMinutes = 60;
    public const int RefreshTokenExpiryDays = 30;
    
    // File upload
    public const int MaxFileSize = 10 * 1024 * 1024; // 10MB
    public static readonly string[] AllowedFileExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    
    // Rate limiting
    public const int MaxRequestsPerMinute = 100;
    public const int MaxRequestsPerHour = 1000;
    public const int MaxRequestsPerDay = 10000;
}

/// <summary>
/// Error messages for validation failures
/// </summary>
public static class ValidationMessages
{
    // General messages
    public const string Required = "This field is required.";
    public const string InvalidFormat = "The format is invalid.";
    public const string InvalidLength = "The length is invalid.";
    public const string InvalidRange = "The value is out of range.";
    
    // Financial messages
    public const string InvalidAmount = "The amount must be between {0} and {1}.";
    public const string InvalidCurrency = "Invalid currency code.";
    public const string InvalidInstallmentCount = "Installment count must be between {0} and {1}.";
    public const string InsufficientFunds = "Insufficient funds for this transaction.";
    
    // Personal information messages
    public const string InvalidEmail = "Please enter a valid email address.";
    public const string InvalidPhone = "Please enter a valid phone number.";
    public const string InvalidSSN = "Please enter a valid social security number.";
    public const string InvalidDate = "Please enter a valid date.";
    public const string InvalidAge = "Age must be between 18 and 120 years.";
    
    // Business rule messages
    public const string MaxPaymentPlansExceeded = "Maximum number of payment plans exceeded.";
    public const string PaymentAlreadyProcessed = "This payment has already been processed.";
    public const string PaymentExpired = "This payment has expired.";
    public const string IneligibleForBNPL = "Customer is not eligible for BNPL services.";
    
    // Security messages
    public const string InvalidCredentials = "Invalid username or password.";
    public const string AccountLocked = "Account has been locked due to multiple failed attempts.";
    public const string TokenExpired = "Authentication token has expired.";
    public const string InsufficientPermissions = "Insufficient permissions to perform this action.";
    
    // File upload messages
    public const string FileTooLarge = "File size exceeds the maximum allowed size of {0}MB.";
    public const string InvalidFileType = "File type not allowed. Allowed types: {0}.";
    public const string FileUploadFailed = "File upload failed. Please try again.";
}