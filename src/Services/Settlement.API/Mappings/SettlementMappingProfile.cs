using AutoMapper;
using YourCompanyBNPL.Settlement.API.Models;
using YourCompanyBNPL.Settlement.API.DTOs;

namespace YourCompanyBNPL.Settlement.API.Mappings;

public class SettlementMappingProfile : Profile
{
    public SettlementMappingProfile()
    {
        // Settlement Transaction mappings
        CreateMap<CreateSettlementRequest, SettlementTransaction>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => GenerateReference()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<SettlementTransaction, SettlementResponse>();

        // Merchant Account mappings
        CreateMap<MerchantAccountRequest, MerchantAccount>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false));

        CreateMap<MerchantAccount, MerchantAccountResponse>();

        // Settlement Batch mappings
        CreateMap<SettlementBatch, SettlementBatchResponse>();
    }

    private static string GenerateReference()
    {
        return $"SETT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}