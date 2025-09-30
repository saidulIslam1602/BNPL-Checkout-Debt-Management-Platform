using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Risk.API.Services;

/// <summary>
/// Interface for risk assessment operations
/// </summary>
public interface IRiskAssessmentService
{
    /// <summary>
    /// Performs comprehensive credit assessment for BNPL eligibility
    /// </summary>
    Task<ApiResponse<CreditAssessmentResponse>> AssessCreditRiskAsync(CreditAssessmentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets existing credit assessment by ID
    /// </summary>
    Task<ApiResponse<CreditAssessmentResponse>> GetCreditAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets customer's credit assessments with pagination
    /// </summary>
    Task<PagedApiResponse<CreditAssessmentResponse>> GetCustomerAssessmentsAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches credit assessments with filtering
    /// </summary>
    Task<PagedApiResponse<CreditAssessmentResponse>> SearchAssessmentsAsync(RiskAssessmentSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates customer risk profile based on payment behavior
    /// </summary>
    Task<ApiResponse<CustomerRiskProfileResponse>> UpdateRiskProfileAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets customer risk profile
    /// </summary>
    Task<ApiResponse<CustomerRiskProfileResponse>> GetRiskProfileAsync(Guid customerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets risk analytics for reporting
    /// </summary>
    Task<ApiResponse<RiskAnalytics>> GetRiskAnalyticsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for fraud detection operations
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// Performs real-time fraud detection on transaction
    /// </summary>
    Task<ApiResponse<FraudDetectionResponse>> DetectFraudAsync(FraudDetectionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets fraud detection result by ID
    /// </summary>
    Task<ApiResponse<FraudDetectionResponse>> GetFraudDetectionAsync(Guid detectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches fraud detections with filtering
    /// </summary>
    Task<PagedApiResponse<FraudDetectionResponse>> SearchFraudDetectionsAsync(FraudDetectionSearchRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates fraud rules and thresholds
    /// </summary>
    Task<ApiResponse> UpdateFraudRulesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for credit bureau integration
/// </summary>
public interface ICreditBureauService
{
    /// <summary>
    /// Gets credit report from Norwegian credit bureau (Experian Norway)
    /// </summary>
    Task<ApiResponse<CreditBureauSummary>> GetCreditReportAsync(CreditBureauRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates Norwegian social security number
    /// </summary>
    Task<ApiResponse<bool>> ValidateSSNAsync(string socialSecurityNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks for bankruptcy records
    /// </summary>
    Task<ApiResponse<bool>> CheckBankruptcyAsync(string socialSecurityNumber, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets payment history from credit bureau
    /// </summary>
    Task<ApiResponse<Dictionary<string, object>>> GetPaymentHistoryAsync(string socialSecurityNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for machine learning risk scoring
/// </summary>
public interface IMachineLearningService
{
    /// <summary>
    /// Predicts credit risk using ML model
    /// </summary>
    Task<ApiResponse<decimal>> PredictCreditRiskAsync(Dictionary<string, object> features, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predicts fraud probability using ML model
    /// </summary>
    Task<ApiResponse<decimal>> PredictFraudRiskAsync(Dictionary<string, object> features, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrains models with new data
    /// </summary>
    Task<ApiResponse> RetrainModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets model performance metrics
    /// </summary>
    Task<ApiResponse<ModelPerformanceMetrics>> GetModelPerformanceAsync(string modelName, CancellationToken cancellationToken = default);
}