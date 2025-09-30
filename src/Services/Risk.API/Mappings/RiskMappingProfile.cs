using AutoMapper;
using YourCompanyBNPL.Risk.API.Models;
using YourCompanyBNPL.Risk.API.DTOs;

namespace YourCompanyBNPL.Risk.API.Mappings;

/// <summary>
/// AutoMapper profile for Risk API mappings
/// </summary>
public class RiskMappingProfile : Profile
{
    public RiskMappingProfile()
    {
        CreateMap<CreditAssessment, CreditAssessmentResponse>()
            .ForMember(dest => dest.RiskFactors, opt => opt.Ignore()) // Handled manually
            .ForMember(dest => dest.CreditBureauData, opt => opt.Ignore()); // Handled manually

        CreateMap<CustomerRiskProfile, CustomerRiskProfileResponse>();

        CreateMap<FraudDetection, FraudDetectionResponse>()
            .ForMember(dest => dest.TriggeredRules, opt => opt.Ignore()); // Handled manually

        CreateMap<RiskFactor, RiskFactorSummary>();

        CreateMap<FraudRule, FraudRuleSummary>();

        CreateMap<CreditAssessmentRequest, CreditAssessment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreditScore, opt => opt.Ignore())
            .ForMember(dest => dest.CreditRating, opt => opt.Ignore())
            .ForMember(dest => dest.RiskLevel, opt => opt.Ignore())
            .ForMember(dest => dest.RecommendedCreditLimit, opt => opt.Ignore())
            .ForMember(dest => dest.IsApproved, opt => opt.Ignore())
            .ForMember(dest => dest.DeclineReason, opt => opt.Ignore())
            .ForMember(dest => dest.InterestRate, opt => opt.Ignore())
            .ForMember(dest => dest.AssessmentDate, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalReferenceId, opt => opt.Ignore())
            .ForMember(dest => dest.RiskFactors, opt => opt.Ignore())
            .ForMember(dest => dest.CreditBureauResponses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.AdditionalData));

        CreateMap<FraudDetectionRequest, FraudDetection>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FraudRiskLevel, opt => opt.Ignore())
            .ForMember(dest => dest.FraudScore, opt => opt.Ignore())
            .ForMember(dest => dest.IsBlocked, opt => opt.Ignore())
            .ForMember(dest => dest.BlockReason, opt => opt.Ignore())
            .ForMember(dest => dest.DetectionDate, opt => opt.Ignore())
            .ForMember(dest => dest.TriggeredRules, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.AdditionalData));
    }
}