using YourCompanyBNPL.Common.Enums;
using AutoMapper;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;

namespace YourCompanyBNPL.Payment.API.Mappings;

/// <summary>
/// AutoMapper profile for Payment API mappings
/// </summary>
public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // Payment mappings
        CreateMap<Models.Payment, PaymentResponse>()
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.Merchant, opt => opt.Ignore())
            .ForMember(dest => dest.BNPLPlan, opt => opt.Ignore());

        CreateMap<CreatePaymentRequest, Models.Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => YourCompanyBNPL.Common.Enums.PaymentStatus.Pending))
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => YourCompanyBNPL.Common.Enums.TransactionType.Payment))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.NetAmount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Fees, opt => opt.MapFrom(src => 0m));

        // Customer mappings
        CreateMap<Customer, CustomerSummary>();

        // Merchant mappings
        CreateMap<Merchant, MerchantSummary>();

        // BNPL Plan mappings
        CreateMap<BNPLPlan, BNPLPlanSummary>()
            .ForMember(dest => dest.Installments, opt => opt.MapFrom(src => src.Installments.OrderBy(i => i.InstallmentNumber)));

        // Installment mappings
        CreateMap<Installment, InstallmentResponse>();

        CreateMap<InstallmentCalculation, Installment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.BNPLPlanId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => YourCompanyBNPL.Common.Enums.PaymentStatus.Pending))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Refund mappings
        CreateMap<PaymentRefund, RefundResponse>();

        CreateMap<CreateRefundRequest, PaymentRefund>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => YourCompanyBNPL.Common.Enums.PaymentStatus.Pending))
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Settlement mappings
        CreateMap<Settlement, SettlementSummary>()
            .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Merchant != null ? src.Merchant.Name : ""));
        CreateMap<Settlement, SettlementDetails>()
            .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Merchant != null ? src.Merchant.Name : ""));
        CreateMap<SettlementTransaction, SettlementTransactionSummary>()
            .ForMember(dest => dest.PaymentReference, opt => opt.MapFrom(src => src.Payment != null ? src.Payment.OrderReference : ""))
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Payment != null ? src.Payment.ProcessedAt ?? src.Payment.CreatedAt : DateTime.MinValue))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.Payment != null ? src.Payment.PaymentMethod : PaymentMethod.Card))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Payment != null && src.Payment.Customer != null ? src.Payment.Customer.Email : ""));
        CreateMap<Models.SettlementEvent, DTOs.SettlementEvent>();
        CreateMap<SettlementSchedule, SettlementScheduleConfig>();
        CreateMap<SettlementBatch, SettlementBatchResponse>()
            .ForMember(dest => dest.MerchantName, opt => opt.MapFrom(src => src.Merchant != null ? src.Merchant.Name : ""));

        // Address mappings
        CreateMap<CustomerAddress, CustomerAddress>();
        CreateMap<MerchantAddress, MerchantAddress>();

        // Event mappings
        CreateMap<PaymentEvent, PaymentEvent>();
    }
}