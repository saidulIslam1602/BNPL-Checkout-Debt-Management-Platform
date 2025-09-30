using System.Text;
using System.Text.Json;
using YourCompanyBNPL.Risk.API.DTOs;
using YourCompanyBNPL.Common.Models;
using Microsoft.Extensions.Options;

namespace YourCompanyBNPL.Risk.API.Services;

/// <summary>
/// Real integration with Norwegian credit bureaus (Experian, Bisnode, etc.)
/// </summary>
public class CreditBureauService : ICreditBureauService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CreditBureauService> _logger;
    private readonly CreditBureauOptions _options;

    public CreditBureauService(
        HttpClient httpClient,
        ILogger<CreditBureauService> logger,
        IOptions<CreditBureauOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ApiResponse<CreditBureauSummary>> GetCreditReportAsync(CreditBureauRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting credit report from {Bureau} for SSN {SSN}", 
                request.BureauName, MaskSSN(request.SocialSecurityNumber));

            // Validate Norwegian SSN format
            if (!IsValidNorwegianSSN(request.SocialSecurityNumber))
            {
                return ApiResponse<CreditBureauSummary>.ErrorResult("Invalid Norwegian social security number format", 400);
            }

            var bureauConfig = GetBureauConfiguration(request.BureauName);
            if (bureauConfig == null)
            {
                return ApiResponse<CreditBureauSummary>.ErrorResult($"Unsupported credit bureau: {request.BureauName}", 400);
            }

            // Prepare request based on bureau type
            var creditReport = request.BureauName.ToLower() switch
            {
                "experian" => await GetExperianCreditReportAsync(request, bureauConfig, cancellationToken),
                "bisnode" => await GetBisnodeCreditReportAsync(request, bureauConfig, cancellationToken),
                "lindorff" => await GetLindorffCreditReportAsync(request, bureauConfig, cancellationToken),
                _ => throw new NotSupportedException($"Bureau {request.BureauName} not supported")
            };

            _logger.LogInformation("Successfully retrieved credit report from {Bureau}, Score: {Score}", 
                request.BureauName, creditReport.CreditScore);

            return ApiResponse<CreditBureauSummary>.SuccessResult(creditReport, "Credit report retrieved successfully");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while requesting credit report from {Bureau}", request.BureauName);
            return ApiResponse<CreditBureauSummary>.ErrorResult("Credit bureau service temporarily unavailable", 503);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while requesting credit report from {Bureau}", request.BureauName);
            return ApiResponse<CreditBureauSummary>.ErrorResult("Credit bureau request timeout", 408);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting credit report from {Bureau}", request.BureauName);
            return ApiResponse<CreditBureauSummary>.ErrorResult("An error occurred while retrieving credit report", 500);
        }
    }

    public async Task<ApiResponse<bool>> ValidateSSNAsync(string socialSecurityNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating Norwegian SSN {SSN}", MaskSSN(socialSecurityNumber));

            // Basic format validation
            if (!IsValidNorwegianSSN(socialSecurityNumber))
            {
                return ApiResponse<bool>.SuccessResult(false, "Invalid SSN format");
            }

            // Real validation against Norwegian Population Register (Folkeregisteret)
            var validationRequest = new
            {
                ssn = socialSecurityNumber,
                validateChecksum = true,
                validateAge = true,
                validateStatus = true
            };

            var json = JsonSerializer.Serialize(validationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.FolkeregisterApiKey}");
            _httpClient.DefaultRequestHeaders.Add("X-API-Version", "2.0");

            var response = await _httpClient.PostAsync($"{_options.FolkeregisterBaseUrl}/validate-ssn", content, cancellationToken);

                if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var validationResult = JsonSerializer.Deserialize<FolkeregisterValidationResponse>(responseContent);
                
                _logger.LogInformation("SSN validation result: {IsValid}", validationResult?.IsValid);
                return ApiResponse<bool>.SuccessResult(validationResult?.IsValid ?? false);
            }
            else
            {
                _logger.LogWarning("SSN validation failed with status {StatusCode}", response.StatusCode);
                return ApiResponse<bool>.ErrorResult("SSN validation service error", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SSN");
            return ApiResponse<bool>.ErrorResult("An error occurred while validating SSN", 500);
        }
    }

    public async Task<ApiResponse<bool>> CheckBankruptcyAsync(string socialSecurityNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking bankruptcy records for SSN {SSN}", MaskSSN(socialSecurityNumber));

            // Real integration with Norwegian Bankruptcy Register (Konkursregisteret)
            var bankruptcyRequest = new
            {
                ssn = socialSecurityNumber,
                includeHistorical = true,
                yearsBack = 7
            };

            var json = JsonSerializer.Serialize(bankruptcyRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.KonkursregisterApiKey}");

            var response = await _httpClient.PostAsync($"{_options.KonkursregisterBaseUrl}/check-bankruptcy", content, cancellationToken);

                if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var bankruptcyResult = JsonSerializer.Deserialize<BankruptcyCheckResponse>(responseContent);
                
                var hasBankruptcy = bankruptcyResult?.HasActiveBankruptcy == true || 
                                   (bankruptcyResult?.HistoricalBankruptcies?.Any() == true);

                _logger.LogInformation("Bankruptcy check result: {HasBankruptcy}", hasBankruptcy);
                return ApiResponse<bool>.SuccessResult(hasBankruptcy);
            }
            else
            {
                _logger.LogWarning("Bankruptcy check failed with status {StatusCode}", response.StatusCode);
                return ApiResponse<bool>.ErrorResult("Bankruptcy check service error", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bankruptcy records");
            return ApiResponse<bool>.ErrorResult("An error occurred while checking bankruptcy records", 500);
        }
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetPaymentHistoryAsync(string socialSecurityNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving payment history for SSN {SSN}", MaskSSN(socialSecurityNumber));

            // Real integration with Norwegian Credit Information Services
            var historyRequest = new
            {
                ssn = socialSecurityNumber,
                includePositiveHistory = true,
                includeNegativeHistory = true,
                monthsBack = 24
            };

            var json = JsonSerializer.Serialize(historyRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.CreditHistoryApiKey}");

            var response = await _httpClient.PostAsync($"{_options.CreditHistoryBaseUrl}/payment-history", content, cancellationToken);

                if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var historyData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                
                _logger.LogInformation("Successfully retrieved payment history");
                return ApiResponse<Dictionary<string, object>>.SuccessResult(historyData ?? new Dictionary<string, object>());
            }
            else
            {
                _logger.LogWarning("Payment history request failed with status {StatusCode}", response.StatusCode);
                return ApiResponse<Dictionary<string, object>>.ErrorResult("Payment history service error", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history");
            return ApiResponse<Dictionary<string, object>>.ErrorResult("An error occurred while retrieving payment history", 500);
        }
    }

    #region Private Methods

    private async Task<CreditBureauSummary> GetExperianCreditReportAsync(CreditBureauRequest request, BureauConfiguration config, CancellationToken cancellationToken)
    {
        // Real Experian Norway API integration
        var experianRequest = new
        {
            consumer = new
            {
                personalDetails = new
                {
                    forename = request.FirstName,
                    surname = request.LastName,
                    dateOfBirth = request.DateOfBirth.ToString("yyyy-MM-dd"),
                    nationalId = request.SocialSecurityNumber
                },
                addresses = new[]
                {
                    new
                    {
                        addressLine1 = request.Address,
                        postalCode = request.PostalCode,
                        city = request.City,
                        countryCode = "NO"
                    }
                }
            },
            options = new
            {
                includeScore = true,
                includeAccountDetails = request.IncludeFullReport,
                includePaymentHistory = request.IncludeFullReport,
                scoreModel = "FICO_V9"
            }
        };

        var json = JsonSerializer.Serialize(experianRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("X-API-Version", "3.0");

        var response = await _httpClient.PostAsync($"{config.BaseUrl}/credit-report", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var experianResponse = JsonSerializer.Deserialize<ExperianCreditResponse>(responseContent);

        return new CreditBureauSummary
        {
            BureauName = "Experian",
            CreditScore = experianResponse?.CreditScore ?? 0,
            ScoreModel = experianResponse?.ScoreModel ?? "FICO_V9",
            ScoreDate = experianResponse?.ScoreDate ?? DateTime.UtcNow,
            TotalDebt = experianResponse?.TotalDebt ?? 0,
            AvailableCredit = experianResponse?.AvailableCredit ?? 0,
            NumberOfAccounts = experianResponse?.NumberOfAccounts ?? 0,
            NumberOfInquiries = experianResponse?.NumberOfInquiries ?? 0,
            HasBankruptcy = experianResponse?.HasBankruptcy ?? false,
            HasCollections = experianResponse?.HasCollections ?? false,
            HasLatePayments = experianResponse?.HasLatePayments ?? false
        };
    }

    private async Task<CreditBureauSummary> GetBisnodeCreditReportAsync(CreditBureauRequest request, BureauConfiguration config, CancellationToken cancellationToken)
    {
        // Real Bisnode (Dun & Bradstreet Norway) API integration
        var bisnodeRequest = new
        {
            subject = new
            {
                firstName = request.FirstName,
                lastName = request.LastName,
                birthDate = request.DateOfBirth.ToString("yyyy-MM-dd"),
                socialSecurityNumber = request.SocialSecurityNumber,
                address = new
                {
                    street = request.Address,
                    postalCode = request.PostalCode,
                    city = request.City,
                    country = "Norway"
                }
            },
            reportType = request.IncludeFullReport ? "COMPREHENSIVE" : "BASIC",
            language = "NO"
        };

        var json = JsonSerializer.Serialize(bisnodeRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.ApiKey}"))}");

        var response = await _httpClient.PostAsync($"{config.BaseUrl}/consumer-credit-report", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var bisnodeResponse = JsonSerializer.Deserialize<BisnodeCreditResponse>(responseContent);

        return new CreditBureauSummary
        {
            BureauName = "Bisnode",
            CreditScore = bisnodeResponse?.RiskScore ?? 0,
            ScoreModel = "Bisnode Risk Model",
            ScoreDate = bisnodeResponse?.ReportDate ?? DateTime.UtcNow,
            TotalDebt = bisnodeResponse?.TotalLiabilities ?? 0,
            AvailableCredit = bisnodeResponse?.AvailableCredit ?? 0,
            NumberOfAccounts = bisnodeResponse?.ActiveAccounts ?? 0,
            NumberOfInquiries = bisnodeResponse?.RecentInquiries ?? 0,
            HasBankruptcy = bisnodeResponse?.BankruptcyIndicator ?? false,
            HasCollections = bisnodeResponse?.CollectionIndicator ?? false,
            HasLatePayments = bisnodeResponse?.PaymentDefaultIndicator ?? false
        };
    }

    private async Task<CreditBureauSummary> GetLindorffCreditReportAsync(CreditBureauRequest request, BureauConfiguration config, CancellationToken cancellationToken)
    {
        // Real Lindorff (Intrum Norway) API integration
        var lindorffRequest = new
        {
            personalNumber = request.SocialSecurityNumber,
            firstName = request.FirstName,
            lastName = request.LastName,
            dateOfBirth = request.DateOfBirth.ToString("ddMMyyyy"),
            requestType = request.IncludeFullReport ? "FULL_REPORT" : "SCORE_ONLY"
        };

        var json = JsonSerializer.Serialize(lindorffRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Client-ID", config.ClientId);

        var response = await _httpClient.PostAsync($"{config.BaseUrl}/credit-assessment", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var lindorffResponse = JsonSerializer.Deserialize<LindorffCreditResponse>(responseContent);

        return new CreditBureauSummary
        {
            BureauName = "Lindorff",
            CreditScore = lindorffResponse?.CreditRating ?? 0,
            ScoreModel = "Lindorff Credit Model",
            ScoreDate = lindorffResponse?.AssessmentDate ?? DateTime.UtcNow,
            TotalDebt = lindorffResponse?.TotalDebt ?? 0,
            AvailableCredit = lindorffResponse?.CreditCapacity ?? 0,
            NumberOfAccounts = lindorffResponse?.NumberOfCreditors ?? 0,
            NumberOfInquiries = lindorffResponse?.InquiryCount ?? 0,
            HasBankruptcy = lindorffResponse?.BankruptcyFlag ?? false,
            HasCollections = lindorffResponse?.CollectionFlag ?? false,
            HasLatePayments = lindorffResponse?.DefaultFlag ?? false
        };
    }

    private static bool IsValidNorwegianSSN(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn) || ssn.Length != 11)
            return false;

        if (!ssn.All(char.IsDigit))
            return false;

        // Validate Norwegian SSN checksum algorithm
        var digits = ssn.Select(c => int.Parse(c.ToString())).ToArray();
        
        // First control digit
        var sum1 = (3 * digits[0]) + (7 * digits[1]) + (6 * digits[2]) + (1 * digits[3]) + 
                   (8 * digits[4]) + (9 * digits[5]) + (4 * digits[6]) + (5 * digits[7]) + (2 * digits[8]);
        var control1 = 11 - (sum1 % 11);
        if (control1 == 11) control1 = 0;
        if (control1 == 10) return false;
        if (control1 != digits[9]) return false;

        // Second control digit
        var sum2 = (5 * digits[0]) + (4 * digits[1]) + (3 * digits[2]) + (2 * digits[3]) + 
                   (7 * digits[4]) + (6 * digits[5]) + (5 * digits[6]) + (4 * digits[7]) + 
                   (3 * digits[8]) + (2 * digits[9]);
        var control2 = 11 - (sum2 % 11);
        if (control2 == 11) control2 = 0;
        if (control2 == 10) return false;
        if (control2 != digits[10]) return false;

        return true;
    }

    private static string MaskSSN(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn) || ssn.Length < 4)
            return "****";
        
        return ssn[..2] + "****" + ssn[^2..];
    }

    private BureauConfiguration? GetBureauConfiguration(string bureauName)
    {
        return _options.Bureaus?.FirstOrDefault(b => 
            string.Equals(b.Name, bureauName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}

#region Configuration Classes

public class CreditBureauOptions
{
    public List<BureauConfiguration> Bureaus { get; set; } = new();
    public string FolkeregisterBaseUrl { get; set; } = string.Empty;
    public string FolkeregisterApiKey { get; set; } = string.Empty;
    public string KonkursregisterBaseUrl { get; set; } = string.Empty;
    public string KonkursregisterApiKey { get; set; } = string.Empty;
    public string CreditHistoryBaseUrl { get; set; } = string.Empty;
    public string CreditHistoryApiKey { get; set; } = string.Empty;
}

public class BureauConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? ClientId { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

#endregion

#region Response Models

public class FolkeregisterValidationResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ValidationDetails { get; set; }
}

public class BankruptcyCheckResponse
{
    public bool HasActiveBankruptcy { get; set; }
    public List<BankruptcyRecord>? HistoricalBankruptcies { get; set; }
}

public class BankruptcyRecord
{
    public DateTime FilingDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ExperianCreditResponse
{
    public int CreditScore { get; set; }
    public string ScoreModel { get; set; } = string.Empty;
    public DateTime ScoreDate { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal AvailableCredit { get; set; }
    public int NumberOfAccounts { get; set; }
    public int NumberOfInquiries { get; set; }
    public bool HasBankruptcy { get; set; }
    public bool HasCollections { get; set; }
    public bool HasLatePayments { get; set; }
}

public class BisnodeCreditResponse
{
    public int RiskScore { get; set; }
    public DateTime ReportDate { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal AvailableCredit { get; set; }
    public int ActiveAccounts { get; set; }
    public int RecentInquiries { get; set; }
    public bool BankruptcyIndicator { get; set; }
    public bool CollectionIndicator { get; set; }
    public bool PaymentDefaultIndicator { get; set; }
}

public class LindorffCreditResponse
{
    public int CreditRating { get; set; }
    public DateTime AssessmentDate { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal CreditCapacity { get; set; }
    public int NumberOfCreditors { get; set; }
    public int InquiryCount { get; set; }
    public bool BankruptcyFlag { get; set; }
    public bool CollectionFlag { get; set; }
    public bool DefaultFlag { get; set; }
}

#endregion