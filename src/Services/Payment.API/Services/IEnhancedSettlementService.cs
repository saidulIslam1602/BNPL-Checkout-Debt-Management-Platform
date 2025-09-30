using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Enhanced interface for settlement operations with advanced features
/// </summary>
public interface IEnhancedSettlementService
{
    /// <summary>
    /// Creates a settlement batch for a merchant
    /// </summary>
    Task<ApiResponse<SettlementBatchResponse>> CreateSettlementBatchAsync(CreateSettlementBatchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a settlement batch
    /// </summary>
    Task<ApiResponse<SettlementBatchResponse>> ProcessSettlementBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement summary by ID
    /// </summary>
    Task<ApiResponse<SettlementSummary>> GetSettlementSummaryAsync(Guid settlementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets merchant settlements with search and filtering
    /// </summary>
    Task<PagedApiResponse<SettlementSummary>> GetMerchantSettlementsAsync(Guid merchantId, SettlementSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a settlement report for a merchant
    /// </summary>
    Task<ApiResponse<SettlementReportResponse>> GenerateSettlementReportAsync(SettlementReportRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes automatic settlements for eligible merchants
    /// </summary>
    Task<ApiResponse> ProcessAutomaticSettlementsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets settlement forecast for a merchant
    /// </summary>
    Task<ApiResponse<SettlementForecastResponse>> GetSettlementForecastAsync(Guid merchantId, int days, CancellationToken cancellationToken = default);
}