using FluentValidation;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Shared.Common.Enums;

namespace RivertyBNPL.Services.Notification.API.Validators;

/// <summary>
/// Validator for SendNotificationRequest
/// </summary>
public class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecipientEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.RecipientPhone)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.RecipientPhone))
            .WithMessage("Phone number must be in valid international format");

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Channel == NotificationChannel.Email);

        RuleFor(x => x.Content)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.TemplateName))
            .WithMessage("Content is required when no template is specified");

        RuleFor(x => x.TemplateName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => string.IsNullOrEmpty(x.Content))
            .WithMessage("Template name is required when no content is specified");

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future");

        RuleFor(x => x.RecipientPhone)
            .NotEmpty()
            .When(x => x.Channel == NotificationChannel.SMS)
            .WithMessage("Phone number is required for SMS notifications");

        RuleFor(x => x.RecipientDeviceToken)
            .NotEmpty()
            .When(x => x.Channel == NotificationChannel.Push)
            .WithMessage("Device token is required for push notifications");
    }
}

/// <summary>
/// Validator for SendBulkNotificationRequest
/// </summary>
public class SendBulkNotificationRequestValidator : AbstractValidator<SendBulkNotificationRequest>
{
    public SendBulkNotificationRequestValidator()
    {
        RuleFor(x => x.Recipients)
            .NotEmpty()
            .Must(x => x.Count <= 1000)
            .WithMessage("Maximum 1000 recipients allowed per bulk request");

        RuleForEach(x => x.Recipients)
            .SetValidator(new BulkNotificationRecipientValidator());

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Channel == NotificationChannel.Email);

        RuleFor(x => x.Content)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.TemplateName))
            .WithMessage("Content is required when no template is specified");

        RuleFor(x => x.TemplateName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => string.IsNullOrEmpty(x.Content))
            .WithMessage("Template name is required when no content is specified");

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future");
    }
}

/// <summary>
/// Validator for BulkNotificationRecipient
/// </summary>
public class BulkNotificationRecipientValidator : AbstractValidator<BulkNotificationRecipient>
{
    public BulkNotificationRecipientValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecipientEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.RecipientPhone)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.RecipientPhone))
            .WithMessage("Phone number must be in valid international format");
    }
}

/// <summary>
/// Validator for CreateNotificationTemplateRequest
/// </summary>
public class CreateNotificationTemplateRequestValidator : AbstractValidator<CreateNotificationTemplateRequest>
{
    public CreateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9_]+$")
            .WithMessage("Template name must contain only lowercase letters, numbers, and underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BodyTemplate)
            .NotEmpty()
            .Must(BeValidTemplate)
            .WithMessage("Template must contain valid placeholder syntax");

        RuleFor(x => x.HtmlTemplate)
            .Must(BeValidHtmlTemplate)
            .When(x => !string.IsNullOrEmpty(x.HtmlTemplate))
            .WithMessage("HTML template must be valid HTML");

        RuleFor(x => x.Language)
            .NotEmpty()
            .MaximumLength(10)
            .Matches(@"^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Language must be in format 'xx-XX' (e.g., 'nb-NO')");
    }

    private static bool BeValidTemplate(string template)
    {
        // Basic validation for template placeholders
        if (string.IsNullOrEmpty(template))
            return false;

        // Check for balanced braces
        var openBraces = template.Count(c => c == '{');
        var closeBraces = template.Count(c => c == '}');
        
        return openBraces == closeBraces && openBraces % 2 == 0;
    }

    private static bool BeValidHtmlTemplate(string? htmlTemplate)
    {
        if (string.IsNullOrEmpty(htmlTemplate))
            return true;

        // Basic HTML validation - check for balanced tags
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml($"<root>{htmlTemplate}</root>");
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for UpdateNotificationPreferencesRequest
/// </summary>
public class UpdateNotificationPreferencesRequestValidator : AbstractValidator<UpdateNotificationPreferencesRequest>
{
    public UpdateNotificationPreferencesRequestValidator()
    {
        RuleFor(x => x.Preferences)
            .NotEmpty();

        RuleForEach(x => x.Preferences)
            .SetValidator(new NotificationPreferenceUpdateValidator());
    }
}

/// <summary>
/// Validator for NotificationPreferenceUpdate
/// </summary>
public class NotificationPreferenceUpdateValidator : AbstractValidator<NotificationPreferenceUpdate>
{
    public NotificationPreferenceUpdateValidator()
    {
        RuleFor(x => x.NotificationType)
            .IsInEnum();

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.QuietHoursStart)
            .LessThan(x => x.QuietHoursEnd)
            .When(x => x.QuietHoursStart.HasValue && x.QuietHoursEnd.HasValue)
            .WithMessage("Quiet hours start must be before quiet hours end");

        RuleFor(x => x.TimeZone)
            .Must(BeValidTimeZone)
            .When(x => !string.IsNullOrEmpty(x.TimeZone))
            .WithMessage("Invalid time zone identifier");
    }

    private static bool BeValidTimeZone(string? timeZone)
    {
        if (string.IsNullOrEmpty(timeZone))
            return true;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for NotificationQueryParams
/// </summary>
public class NotificationQueryParamsValidator : AbstractValidator<NotificationQueryParams>
{
    public NotificationQueryParamsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be before or equal to to date");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrEmpty(x.SortBy))
            .WithMessage("Invalid sort field");

        RuleFor(x => x.SortOrder)
            .Must(x => x == "asc" || x == "desc")
            .When(x => !string.IsNullOrEmpty(x.SortOrder))
            .WithMessage("Sort order must be 'asc' or 'desc'");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        var validFields = new[] { "CreatedAt", "UpdatedAt", "SentAt", "DeliveredAt", "Type", "Channel", "Status", "Priority" };
        return validFields.Contains(sortBy);
    }
}