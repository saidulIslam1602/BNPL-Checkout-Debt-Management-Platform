using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YourCompanyBNPL.Risk.API.Services;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Risk.API.Controllers;

/// <summary>
/// Controller for machine learning model management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ModelManagementController : ControllerBase
{
    private readonly IMachineLearningService _machineLearningService;
    private readonly ILogger<ModelManagementController> _logger;

    public ModelManagementController(
        IMachineLearningService machineLearningService,
        ILogger<ModelManagementController> logger)
    {
        _machineLearningService = machineLearningService;
        _logger = logger;
    }

    /// <summary>
    /// Predicts credit risk using ML model
    /// </summary>
    /// <param name="request">Credit risk prediction request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credit risk score</returns>
    [HttpPost("predict/credit-risk")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<decimal>>> PredictCreditRisk(
        [FromBody] CreditRiskPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Predicting credit risk for customer {CustomerId}", request.CustomerId);

            var features = ConvertToFeatureDictionary(request);
            var result = await _machineLearningService.PredictCreditRiskAsync(features, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Credit risk prediction completed for customer {CustomerId}. Risk Score: {RiskScore}",
                    request.CustomerId, result.Data);
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting credit risk for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred during credit risk prediction", 500));
        }
    }

    /// <summary>
    /// Predicts fraud risk using ML model
    /// </summary>
    /// <param name="request">Fraud risk prediction request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fraud risk score</returns>
    [HttpPost("predict/fraud-risk")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<decimal>>> PredictFraudRisk(
        [FromBody] FraudRiskPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Predicting fraud risk for transaction {TransactionId}", request.TransactionId);

            var features = ConvertToFeatureDictionary(request);
            var result = await _machineLearningService.PredictFraudRiskAsync(features, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Fraud risk prediction completed for transaction {TransactionId}. Fraud Score: {FraudScore}",
                    request.TransactionId, result.Data);
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting fraud risk for transaction {TransactionId}", request.TransactionId);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred during fraud risk prediction", 500));
        }
    }

    /// <summary>
    /// Gets model performance metrics
    /// </summary>
    /// <param name="modelName">Name of the model</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model performance metrics</returns>
    [HttpGet("performance/{modelName}")]
    [ProducesResponseType(typeof(ApiResponse<ModelPerformanceMetrics>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ModelPerformanceMetrics>>> GetModelPerformance(
        [FromRoute] string modelName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return BadRequest(ApiResponse.ErrorResult("Model name is required", 400));
            }

            _logger.LogInformation("Retrieving performance metrics for model {ModelName}", modelName);

            var result = await _machineLearningService.GetModelPerformanceAsync(modelName, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics for model {ModelName}", modelName);
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving model performance", 500));
        }
    }

    /// <summary>
    /// Retrains models with new data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Retraining result</returns>
    [HttpPost("retrain")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse>> RetrainModels(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting model retraining process");

            var result = await _machineLearningService.RetrainModelsAsync(cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Model retraining completed successfully");
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model retraining");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred during model retraining", 500));
        }
    }

    /// <summary>
    /// Gets list of available models
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available models</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(ApiResponse<List<ModelInfo>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<List<ModelInfo>>>> GetAvailableModels(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving available models");

            // This would typically come from the database or model registry
            var models = new List<ModelInfo>
            {
                new()
                {
                    Name = "BNPL_Credit_Risk_Model",
                    DisplayName = "BNPL Credit Risk Assessment Model",
                    Version = "2.1.0",
                    ModelType = "FastTree Binary Classification",
                    IsActive = true,
                    TrainedDate = DateTime.UtcNow.AddDays(-3),
                    DeployedDate = DateTime.UtcNow.AddDays(-1),
                    Accuracy = 0.87m,
                    Description = "Machine learning model for assessing credit risk in BNPL applications"
                },
                new()
                {
                    Name = "BNPL_Fraud_Risk_Model",
                    DisplayName = "BNPL Fraud Detection Model",
                    Version = "1.8.0",
                    ModelType = "FastTree Binary Classification",
                    IsActive = true,
                    TrainedDate = DateTime.UtcNow.AddDays(-5),
                    DeployedDate = DateTime.UtcNow.AddDays(-2),
                    Accuracy = 0.92m,
                    Description = "Machine learning model for real-time fraud detection in transactions"
                }
            };

            return Ok(ApiResponse<List<ModelInfo>>.SuccessResult(models, "Available models retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving models", 500));
        }
    }

    /// <summary>
    /// Gets model training status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training status information</returns>
    [HttpGet("training-status")]
    [ProducesResponseType(typeof(ApiResponse<ModelTrainingStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<ActionResult<ApiResponse<ModelTrainingStatus>>> GetTrainingStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving model training status");

            // This would typically track actual training jobs
            var status = new ModelTrainingStatus
            {
                IsTraining = false,
                LastTrainingStarted = DateTime.UtcNow.AddDays(-3),
                LastTrainingCompleted = DateTime.UtcNow.AddDays(-3).AddHours(2),
                NextScheduledTraining = DateTime.UtcNow.AddDays(4),
                TrainingDataSize = 75000,
                ModelsInTraining = new List<string>(),
                RecentTrainingJobs = new List<TrainingJobInfo>
                {
                    new()
                    {
                        JobId = Guid.NewGuid(),
                        ModelName = "BNPL_Credit_Risk_Model",
                        Status = "Completed",
                        StartTime = DateTime.UtcNow.AddDays(-3),
                        EndTime = DateTime.UtcNow.AddDays(-3).AddHours(1.5),
                        Accuracy = 0.87m,
                        DataSize = 50000
                    },
                    new()
                    {
                        JobId = Guid.NewGuid(),
                        ModelName = "BNPL_Fraud_Risk_Model",
                        Status = "Completed",
                        StartTime = DateTime.UtcNow.AddDays(-5),
                        EndTime = DateTime.UtcNow.AddDays(-5).AddHours(2.2),
                        Accuracy = 0.92m,
                        DataSize = 25000
                    }
                }
            };

            return Ok(ApiResponse<ModelTrainingStatus>.SuccessResult(status, "Training status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model training status");
            return StatusCode(500, ApiResponse.ErrorResult("An unexpected error occurred while retrieving training status", 500));
        }
    }

    private static Dictionary<string, object> ConvertToFeatureDictionary(CreditRiskPredictionRequest request)
    {
        return new Dictionary<string, object>
        {
            { "CreditScore", request.CreditScore },
            { "AnnualIncome", request.AnnualIncome },
            { "ExistingDebt", request.ExistingDebt },
            { "PaymentHistoryMonths", request.PaymentHistoryMonths },
            { "LatePaymentsLast12Months", request.LatePaymentsLast12Months },
            { "ExistingCreditAccounts", request.ExistingCreditAccounts },
            { "RequestedAmount", request.RequestedAmount },
            { "Age", request.Age },
            { "EmploymentLength", request.EmploymentLength },
            { "ResidentialStability", request.ResidentialStability },
            { "DebtToIncomeRatio", request.DebtToIncomeRatio },
            { "HasBankruptcy", request.HasBankruptcy },
            { "HasCollections", request.HasCollections }
        };
    }

    private static Dictionary<string, object> ConvertToFeatureDictionary(FraudRiskPredictionRequest request)
    {
        return new Dictionary<string, object>
        {
            { "TransactionAmount", request.TransactionAmount },
            { "HourOfDay", request.HourOfDay },
            { "DayOfWeek", request.DayOfWeek },
            { "DeviceRiskScore", request.DeviceRiskScore },
            { "LocationRiskScore", request.LocationRiskScore },
            { "VelocityScore", request.VelocityScore },
            { "CustomerAge", request.CustomerAge },
            { "AccountAge", request.AccountAge },
            { "PreviousTransactionCount", request.PreviousTransactionCount },
            { "AverageTransactionAmount", request.AverageTransactionAmount },
            { "IsNewDevice", request.IsNewDevice },
            { "IsNewLocation", request.IsNewLocation },
            { "IsWeekend", request.IsWeekend }
        };
    }
}

/// <summary>
/// Credit risk prediction request
/// </summary>
public class CreditRiskPredictionRequest
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Range(300, 850)]
    public int CreditScore { get; set; }
    
    [Range(0, 10000000)]
    public decimal AnnualIncome { get; set; }
    
    [Range(0, 10000000)]
    public decimal ExistingDebt { get; set; }
    
    [Range(0, 600)]
    public int PaymentHistoryMonths { get; set; }
    
    [Range(0, 12)]
    public int LatePaymentsLast12Months { get; set; }
    
    [Range(0, 50)]
    public int ExistingCreditAccounts { get; set; }
    
    [Range(0.01, 1000000)]
    public decimal RequestedAmount { get; set; }
    
    [Range(18, 100)]
    public int Age { get; set; }
    
    [Range(0, 50)]
    public int EmploymentLength { get; set; }
    
    [Range(0, 20)]
    public int ResidentialStability { get; set; }
    
    [Range(0, 10)]
    public decimal DebtToIncomeRatio { get; set; }
    
    public bool HasBankruptcy { get; set; }
    public bool HasCollections { get; set; }
}

/// <summary>
/// Fraud risk prediction request
/// </summary>
public class FraudRiskPredictionRequest
{
    [Required]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    public Guid CustomerId { get; set; }
    
    [Range(0.01, 1000000)]
    public decimal TransactionAmount { get; set; }
    
    [Range(0, 23)]
    public int HourOfDay { get; set; }
    
    [Range(0, 6)]
    public int DayOfWeek { get; set; }
    
    [Range(0, 100)]
    public decimal DeviceRiskScore { get; set; }
    
    [Range(0, 100)]
    public decimal LocationRiskScore { get; set; }
    
    [Range(0, 100)]
    public decimal VelocityScore { get; set; }
    
    [Range(18, 100)]
    public int CustomerAge { get; set; }
    
    [Range(0, 3650)]
    public int AccountAge { get; set; }
    
    [Range(0, 10000)]
    public int PreviousTransactionCount { get; set; }
    
    [Range(0, 1000000)]
    public decimal AverageTransactionAmount { get; set; }
    
    public bool IsNewDevice { get; set; }
    public bool IsNewLocation { get; set; }
    public bool IsWeekend { get; set; }
}

/// <summary>
/// Model information
/// </summary>
public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime TrainedDate { get; set; }
    public DateTime DeployedDate { get; set; }
    public decimal Accuracy { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Model training status
/// </summary>
public class ModelTrainingStatus
{
    public bool IsTraining { get; set; }
    public DateTime? LastTrainingStarted { get; set; }
    public DateTime? LastTrainingCompleted { get; set; }
    public DateTime? NextScheduledTraining { get; set; }
    public int TrainingDataSize { get; set; }
    public List<string> ModelsInTraining { get; set; } = new();
    public List<TrainingJobInfo> RecentTrainingJobs { get; set; } = new();
}

/// <summary>
/// Training job information
/// </summary>
public class TrainingJobInfo
{
    public Guid JobId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Accuracy { get; set; }
    public int DataSize { get; set; }
}