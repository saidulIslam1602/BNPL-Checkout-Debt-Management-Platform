using YourCompanyBNPL.Common.Enums;
using AutoMapper;
using System.Text.Json;
using YourCompanyBNPL.Notification.API.DTOs;
using YourCompanyBNPL.Notification.API.Models;

namespace YourCompanyBNPL.Notification.API.Mappings;

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
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channel))
            .ForMember(dest => dest.Recipient, opt => opt.MapFrom(src => src.Recipient))
            .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.ScheduledAt, opt => opt.MapFrom(src => src.ScheduledAt))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
            .ForMember(dest => dest.DeliveredAt, opt => opt.MapFrom(src => src.DeliveredAt))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt))
            .ForMember(dest => dest.RetryCount, opt => opt.MapFrom(src => src.RetryCount))
            .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.ErrorMessage))
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.ExternalId))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.MerchantId, opt => opt.MapFrom(src => src.MerchantId))
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.PaymentId))
            .ForMember(dest => dest.InstallmentId, opt => opt.MapFrom(src => src.InstallmentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Template mappings
        CreateMap<NotificationTemplate, TemplateResponse>()
            .ForMember(dest => dest.Variables, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Variables) 
                    ? JsonSerializer.Deserialize<List<string>>(src.Variables, (JsonSerializerOptions?)null) 
                    : new List<string>()));

        CreateMap<CreateTemplateRequest, NotificationTemplate>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.Variables, opt => opt.MapFrom(src => 
                src.Variables != null ? JsonSerializer.Serialize(src.Variables, (JsonSerializerOptions?)null) : null))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => 
                src.Metadata != null ? JsonSerializer.Serialize(src.Metadata, (JsonSerializerOptions?)null) : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Campaign mappings
        CreateMap<Campaign, CampaignResponse>();

        CreateMap<CreateCampaignRequest, Campaign>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CampaignStatus.Draft))
            .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => 
                src.Settings != null ? JsonSerializer.Serialize(src.Settings, (JsonSerializerOptions?)null) : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}