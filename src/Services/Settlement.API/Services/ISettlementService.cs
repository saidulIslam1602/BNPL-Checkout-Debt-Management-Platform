using YourCompanyBNPL.Settlement.API.DTOs;

namespace YourCompanyBNPL.Settlement.API.Services;

public interface ISettlementService
{
    Task<SettlementResponse> CreateSettlementAsync(CreateSettlementRequest request, CancellationToken cancellationToken = default);
    Task<SettlementResponse?> GetSettlementByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SettlementResponse?> GetSettlementByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<List<SettlementResponse>> GetSettlementsByMerchantAsync(Guid merchantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<SettlementResponse> ProcessSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default);
    Task<SettlementBatchResponse> CreateBatchSettlementAsync(List<Guid> settlementIds, CancellationToken cancellationToken = default);
    Task<SettlementSummaryResponse> GetSettlementSummaryAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<MerchantAccountResponse> RegisterMerchantAccountAsync(MerchantAccountRequest request, CancellationToken cancellationToken = default);
    Task<MerchantAccountResponse?> GetMerchantAccountAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<bool> VerifyMerchantAccountAsync(Guid merchantId, CancellationToken cancellationToken = default);
}