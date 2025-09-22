using AutoMapper;
using System.Text.Json;
using RivertyBNPL.Services.Notification.API.DTOs;
using RivertyBNPL.Services.Notification.API.Models;

namespace RivertyBNPL.Services.Notification.API.Mappings;

/// <summary>
/// AutoMapper profile for notification mappings
/// </summary>
public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // Notification mappings
        CreateMap<Models.Notification, NotificationResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RecipientId, opt => opt.MapFrom(src => src.RecipientId))
            .ForMember(dest => dest.RecipientEmail, opt => opt.MapFrom(src => src.RecipientEmail))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel))
            .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.ScheduledAt, opt => opt.MapFrom(src => src.ScheduledAt))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
            .ForMember(dest => dest.DeliveredAt, opt => opt.MapFrom(src => src.DeliveredAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.CorrelationId))
            .ForMember(dest => dest.BatchId, opt => opt.MapFrom(src => src.BatchId));

        // Template mappings
        CreateMap<NotificationTemplate, NotificationTemplateDto>()
            .ForMember(dest => dest.Variables, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Variables) 
                    ? JsonSerializer.Deserialize<List<string>>(src.Variables) 
                    : new List<string>()));

        CreateMap<CreateNotificationTemplateRequest, NotificationTemplate>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.Variables, opt => opt.MapFrom(src => 
                src.Variables != null ? JsonSerializer.Serialize(src.Variables) : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Versions, opt => opt.Ignore());

        // Preference mappings
        CreateMap<NotificationPreference, NotificationPreferenceDto>();

        CreateMap<NotificationPreferenceUpdate, NotificationPreference>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Metadata, opt => opt.Ignore());

        // Delivery attempt mappings
        CreateMap<NotificationDeliveryAttempt, NotificationDeliveryAttemptDto>();

        // Event mappings
        CreateMap<NotificationEvent, NotificationEventDto>()
            .ForMember(dest => dest.EventData, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.EventData) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(src.EventData) 
                    : new Dictionary<string, object>()));
    }
}

/// <summary>
/// DTO for notification delivery attempt
/// </summary>
public class NotificationDeliveryAttemptDto
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTime AttemptedAt { get; set; }
    public NotificationDeliveryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalId { get; set; }
    public string? Response { get; set; }
    public TimeSpan? ResponseTime { get; set; }
}

/// <summary>
/// DTO for notification event
/// </summary>
public class NotificationEventDto
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public Dictionary<string, object>? EventData { get; set; }
    public string? ExternalId { get; set; }
}