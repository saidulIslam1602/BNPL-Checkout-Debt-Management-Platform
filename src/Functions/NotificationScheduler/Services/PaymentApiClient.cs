using YourCompanyBNPL.Common.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace YourCompanyBNPL.NotificationScheduler.Functions.Services;

/// <summary>
/// Implementation of payment API client
/// </summary>
public class PaymentApiClient : IPaymentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentApiClient> _logger;

    public PaymentApiClient(HttpClient httpClient, ILogger<PaymentApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/payments/{paymentId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var payment = JsonSerializer.Deserialize<object>(content);
                
                _logger.LogInformation("Retrieved payment {PaymentId}", paymentId);
                return ApiResponse<object>.SuccessResult(payment, "Payment retrieved");
            }
            else
            {
                _logger.LogError("Failed to get payment. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<object>.ErrorResult($"Failed to get payment: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", paymentId);
            return ApiResponse<object>.ErrorResult($"Error getting payment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> GetInstallmentAsync(Guid installmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/installments/{installmentId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var installment = JsonSerializer.Deserialize<object>(content);
                
                _logger.LogInformation("Retrieved installment {InstallmentId}", installmentId);
                return ApiResponse<object>.SuccessResult(installment, "Installment retrieved");
            }
            else
            {
                _logger.LogError("Failed to get installment. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<object>.ErrorResult($"Failed to get installment: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installment {InstallmentId}", installmentId);
            return ApiResponse<object>.ErrorResult($"Error getting installment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<object>>> GetOverdueInstallmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/installments/overdue", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var installments = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
                
                _logger.LogInformation("Retrieved {Count} overdue installments", installments.Count);
                return ApiResponse<List<object>>.SuccessResult(installments, "Overdue installments retrieved");
            }
            else
            {
                _logger.LogError("Failed to get overdue installments. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<List<object>>.ErrorResult($"Failed to get overdue installments: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue installments");
            return ApiResponse<List<object>>.ErrorResult($"Error getting overdue installments: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customers/{customerId}", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var customer = JsonSerializer.Deserialize<object>(content);
                
                _logger.LogInformation("Retrieved customer {CustomerId}", customerId);
                return ApiResponse<object>.SuccessResult(customer, "Customer retrieved");
            }
            else
            {
                _logger.LogError("Failed to get customer. Status: {StatusCode}", response.StatusCode);
                return ApiResponse<object>.ErrorResult($"Failed to get customer: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            return ApiResponse<object>.ErrorResult($"Error getting customer: {ex.Message}");
        }
    }
}