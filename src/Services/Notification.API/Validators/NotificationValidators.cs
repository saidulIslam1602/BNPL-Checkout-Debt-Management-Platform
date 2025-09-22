using FluentValidation;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Models;

namespace RivertyBNPL.Notification.API.Validators;

/// <summary>
/// Validator for SendNotificationRequest
/// </summary>
public class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Subject)
            .MaximumLength(200)
            .When(x => x.Channel == NotificationChannel.Email);

        RuleFor(x => x.Content)
            .NotEmpty()
            .When(x => !x.TemplateId.HasValue)
            .WithMessage("Content is required when no template is specified");

        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.Content))
            .WithMessage("Template ID is required when no content is specified");

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future");
    }
}

/// <summary>
/// Validator for SendBulkNotificationRequest
/// </summary>
public class SendBulkNotificationRequestValidator : AbstractValidator<SendBulkNotificationRequest>
{
    public SendBulkNotificationRequestValidator()
    {
        RuleFor(x => x.Notifications)
            .NotEmpty()
            .Must(x => x.Count <= 1000)
            .WithMessage("Maximum 1000 notifications allowed per bulk request");

        RuleForEach(x => x.Notifications)
            .SetValidator(new SendNotificationRequestValidator());

        RuleFor(x => x.BatchId)
            .MaximumLength(100);
    }
}

/// <summary>
/// Validator for CreateTemplateRequest
/// </summary>
public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
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
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.HtmlContent)
            .NotEmpty()
            .When(x => x.Channel == NotificationChannel.Email)
            .WithMessage("HTML content is required for email templates");

        RuleFor(x => x.SmsContent)
            .NotEmpty()
            .When(x => x.Channel == NotificationChannel.Sms)
            .WithMessage("SMS content is required for SMS templates");

        RuleFor(x => x.PushContent)
            .NotEmpty()
            .When(x => x.Channel == NotificationChannel.Push)
            .WithMessage("Push content is required for push notification templates");

        RuleFor(x => x.Language)
            .NotEmpty()
            .MaximumLength(10)
            .Matches(@"^[a-z]{2}(-[A-Z]{2})?$")
            .WithMessage("Language must be in format 'xx' or 'xx-XX' (e.g., 'en' or 'en-US')");
    }
}

/// <summary>
/// Validator for UpdatePreferencesRequest
/// </summary>
public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.Preferences)
            .NotNull();

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
/// Validator for CreateCampaignRequest
/// </summary>
public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x.TemplateId)
            .NotEmpty();

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ScheduledAt.HasValue)
            .WithMessage("Scheduled time must be in the future");
    }
}