using YourCompanyBNPL.Common.Enums;
using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.DTOs;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Common.Models;
using YourCompanyBNPL.Common.Enums;
using System.Text.Json;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Service for managing settlement scheduling and frequency
/// </summary>
public interface ISettlementSchedulingService
{
    Task<ApiResponse<SettlementScheduleConfig>> CreateScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SettlementScheduleConfig>> UpdateScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SettlementScheduleConfig>> GetScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SettlementScheduleConfig>>> GetAllSchedulesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<SettlementScheduleConfig>>> GetDueSchedulesAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> ProcessScheduledSettlementsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DateTime?>> GetNextScheduledDateAsync(Guid merchantId, CancellationToken cancellationToken = default);
}

public class SettlementSchedulingService : ISettlementSchedulingService
{
    private readonly PaymentDbContext _context;
    private readonly IEnhancedSettlementService _settlementService;
    private readonly ILogger<SettlementSchedulingService> _logger;
    private readonly IConfiguration _configuration;

    public SettlementSchedulingService(
        PaymentDbContext context,
        IEnhancedSettlementService settlementService,
        ILogger<SettlementSchedulingService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _settlementService = settlementService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<SettlementScheduleConfig>> CreateScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating settlement schedule for merchant {MerchantId}", merchantId);

            // Validate merchant exists
            var merchant = await _context.Merchants
                .FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);

            if (merchant == null)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult("Merchant not found", 404);
            }

            // Check if schedule already exists
            var existingSchedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId, cancellationToken);

            if (existingSchedule != null)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult("Settlement schedule already exists for this merchant", 409);
            }

            // Validate schedule configuration
            var validationResult = ValidateScheduleConfiguration(request);
            if (!validationResult.IsValid)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult(
                    string.Join("; ", validationResult.GetAllErrors()), 400);
            }

            // Create schedule
            var schedule = new SettlementSchedule
            {
                MerchantId = merchantId,
                Frequency = request.Frequency,
                DayOfWeek = request.DayOfWeek,
                DayOfMonth = request.DayOfMonth,
                ProcessingHour = request.ProcessingHour,
                ProcessingMinute = request.ProcessingMinute,
                MinimumAmount = request.MinimumAmount,
                AutoProcess = request.AutoProcess,
                IsActive = request.IsActive,
                Notes = request.Notes,
                CreatedBy = "System" // TODO: Get from current user context
            };

            // Calculate next scheduled date
            schedule.NextScheduledDate = CalculateNextScheduledDate(schedule);

            _context.SettlementSchedules.Add(schedule);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new SettlementScheduleConfig
            {
                Id = schedule.Id,
                MerchantId = schedule.MerchantId,
                Frequency = schedule.Frequency,
                DayOfWeek = schedule.DayOfWeek,
                DayOfMonth = schedule.DayOfMonth,
                ProcessingHour = schedule.ProcessingHour,
                ProcessingMinute = schedule.ProcessingMinute,
                MinimumAmount = schedule.MinimumAmount,
                AutoProcess = schedule.AutoProcess,
                IsActive = schedule.IsActive,
                Notes = schedule.Notes,
                NextScheduledDate = schedule.NextScheduledDate,
                LastProcessedDate = schedule.LastProcessedDate,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt,
                CreatedBy = schedule.CreatedBy,
                UpdatedBy = schedule.UpdatedBy
            };

            _logger.LogInformation("Created settlement schedule {ScheduleId} for merchant {MerchantId}", 
                schedule.Id, merchantId);

            return ApiResponse<SettlementScheduleConfig>.SuccessResult(response, "Settlement schedule created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement schedule for merchant {MerchantId}", merchantId);
            return ApiResponse<SettlementScheduleConfig>.ErrorResult("An error occurred while creating the settlement schedule", 500);
        }
    }

    public async Task<ApiResponse<SettlementScheduleConfig>> UpdateScheduleAsync(Guid merchantId, SettlementScheduleConfigRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating settlement schedule for merchant {MerchantId}", merchantId);

            var schedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId, cancellationToken);

            if (schedule == null)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult("Settlement schedule not found", 404);
            }

            // Validate schedule configuration
            var validationResult = ValidateScheduleConfiguration(request);
            if (!validationResult.IsValid)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult(
                    string.Join("; ", validationResult.GetAllErrors()), 400);
            }

            // Update schedule
            var oldFrequency = schedule.Frequency;
            schedule.Frequency = request.Frequency;
            schedule.DayOfWeek = request.DayOfWeek;
            schedule.DayOfMonth = request.DayOfMonth;
            schedule.ProcessingHour = request.ProcessingHour;
            schedule.ProcessingMinute = request.ProcessingMinute;
            schedule.MinimumAmount = request.MinimumAmount;
            schedule.AutoProcess = request.AutoProcess;
            schedule.IsActive = request.IsActive;
            schedule.Notes = request.Notes;
            schedule.UpdatedBy = "System"; // TODO: Get from current user context
            schedule.UpdatedAt = DateTime.UtcNow;

            // Recalculate next scheduled date if frequency changed
            if (oldFrequency != request.Frequency || !schedule.NextScheduledDate.HasValue)
            {
                schedule.NextScheduledDate = CalculateNextScheduledDate(schedule);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = new SettlementScheduleConfig
            {
                Id = schedule.Id,
                MerchantId = schedule.MerchantId,
                Frequency = schedule.Frequency,
                DayOfWeek = schedule.DayOfWeek,
                DayOfMonth = schedule.DayOfMonth,
                ProcessingHour = schedule.ProcessingHour,
                ProcessingMinute = schedule.ProcessingMinute,
                MinimumAmount = schedule.MinimumAmount,
                AutoProcess = schedule.AutoProcess,
                IsActive = schedule.IsActive,
                Notes = schedule.Notes,
                NextScheduledDate = schedule.NextScheduledDate,
                LastProcessedDate = schedule.LastProcessedDate,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt,
                CreatedBy = schedule.CreatedBy,
                UpdatedBy = schedule.UpdatedBy
            };

            _logger.LogInformation("Updated settlement schedule {ScheduleId} for merchant {MerchantId}", 
                schedule.Id, merchantId);

            return ApiResponse<SettlementScheduleConfig>.SuccessResult(response, "Settlement schedule updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement schedule for merchant {MerchantId}", merchantId);
            return ApiResponse<SettlementScheduleConfig>.ErrorResult("An error occurred while updating the settlement schedule", 500);
        }
    }

    public async Task<ApiResponse<SettlementScheduleConfig>> GetScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId, cancellationToken);

            if (schedule == null)
            {
                return ApiResponse<SettlementScheduleConfig>.ErrorResult("Settlement schedule not found", 404);
            }

            var response = new SettlementScheduleConfig
            {
                Id = schedule.Id,
                MerchantId = schedule.MerchantId,
                Frequency = schedule.Frequency,
                DayOfWeek = schedule.DayOfWeek,
                DayOfMonth = schedule.DayOfMonth,
                ProcessingHour = schedule.ProcessingHour,
                ProcessingMinute = schedule.ProcessingMinute,
                MinimumAmount = schedule.MinimumAmount,
                AutoProcess = schedule.AutoProcess,
                IsActive = schedule.IsActive,
                Notes = schedule.Notes,
                NextScheduledDate = schedule.NextScheduledDate,
                LastProcessedDate = schedule.LastProcessedDate,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt,
                CreatedBy = schedule.CreatedBy,
                UpdatedBy = schedule.UpdatedBy
            };

            return ApiResponse<SettlementScheduleConfig>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement schedule for merchant {MerchantId}", merchantId);
            return ApiResponse<SettlementScheduleConfig>.ErrorResult("An error occurred while retrieving the settlement schedule", 500);
        }
    }

    public async Task<ApiResponse> DeleteScheduleAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId, cancellationToken);

            if (schedule == null)
            {
                return ApiResponse.ErrorResult("Settlement schedule not found", 404);
            }

            _context.SettlementSchedules.Remove(schedule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted settlement schedule {ScheduleId} for merchant {MerchantId}", 
                schedule.Id, merchantId);

            return ApiResponse.SuccessResponse("Settlement schedule deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting settlement schedule for merchant {MerchantId}", merchantId);
            return ApiResponse.ErrorResult("An error occurred while deleting the settlement schedule", 500);
        }
    }

    public async Task<ApiResponse<List<SettlementScheduleConfig>>> GetAllSchedulesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SettlementSchedules.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(s => s.IsActive);
            }

            var schedules = await query
                .OrderBy(s => s.MerchantId)
                .ToListAsync(cancellationToken);

            var response = schedules.Select(s => new SettlementScheduleConfig
            {
                Id = s.Id,
                MerchantId = s.MerchantId,
                Frequency = s.Frequency,
                DayOfWeek = s.DayOfWeek,
                DayOfMonth = s.DayOfMonth,
                ProcessingHour = s.ProcessingHour,
                ProcessingMinute = s.ProcessingMinute,
                MinimumAmount = s.MinimumAmount,
                AutoProcess = s.AutoProcess,
                IsActive = s.IsActive,
                Notes = s.Notes,
                NextScheduledDate = s.NextScheduledDate,
                LastProcessedDate = s.LastProcessedDate,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy
            }).ToList();

            return ApiResponse<List<SettlementScheduleConfig>>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all settlement schedules");
            return ApiResponse<List<SettlementScheduleConfig>>.ErrorResult("An error occurred while retrieving settlement schedules", 500);
        }
    }

    public async Task<ApiResponse<List<SettlementScheduleConfig>>> GetDueSchedulesAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var checkDate = asOfDate ?? DateTime.UtcNow;

            var dueSchedules = await _context.SettlementSchedules
                .Where(s => s.IsActive &&
                           s.NextScheduledDate.HasValue &&
                           s.NextScheduledDate.Value <= checkDate)
                .OrderBy(s => s.NextScheduledDate)
                .ToListAsync(cancellationToken);

            var response = dueSchedules.Select(s => new SettlementScheduleConfig
            {
                Id = s.Id,
                MerchantId = s.MerchantId,
                Frequency = s.Frequency,
                DayOfWeek = s.DayOfWeek,
                DayOfMonth = s.DayOfMonth,
                ProcessingHour = s.ProcessingHour,
                ProcessingMinute = s.ProcessingMinute,
                MinimumAmount = s.MinimumAmount,
                AutoProcess = s.AutoProcess,
                IsActive = s.IsActive,
                Notes = s.Notes,
                NextScheduledDate = s.NextScheduledDate,
                LastProcessedDate = s.LastProcessedDate,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy
            }).ToList();

            _logger.LogInformation("Found {Count} due settlement schedules as of {Date}", response.Count, checkDate);

            return ApiResponse<List<SettlementScheduleConfig>>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving due settlement schedules");
            return ApiResponse<List<SettlementScheduleConfig>>.ErrorResult("An error occurred while retrieving due schedules", 500);
        }
    }

    public async Task<ApiResponse> ProcessScheduledSettlementsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing scheduled settlements");

            var dueSchedulesResult = await GetDueSchedulesAsync(cancellationToken: cancellationToken);
            if (!dueSchedulesResult.Success || !dueSchedulesResult.Data!.Any())
            {
                _logger.LogInformation("No due settlement schedules found");
                return ApiResponse.SuccessResponse("No due settlement schedules found");
            }

            var processedCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var scheduleConfig in dueSchedulesResult.Data)
            {
                try
                {
                    await ProcessSingleScheduleAsync(scheduleConfig, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMessage = $"Failed to process schedule for merchant {scheduleConfig.MerchantId}: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Error processing scheduled settlement for merchant {MerchantId}", scheduleConfig.MerchantId);
                }
            }

            var message = $"Processed {processedCount} scheduled settlements successfully";
            if (errorCount > 0)
            {
                message += $", {errorCount} failed";
            }

            _logger.LogInformation("Scheduled settlement processing completed: {Message}", message);

            return errorCount > 0 ? 
                ApiResponse.ErrorResult(errors, 207) : // 207 Multi-Status
                ApiResponse.SuccessResponse(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled settlements");
            return ApiResponse.ErrorResult("An error occurred while processing scheduled settlements", 500);
        }
    }

    public async Task<ApiResponse<DateTime?>> GetNextScheduledDateAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.IsActive, cancellationToken);

            if (schedule == null)
            {
                return ApiResponse<DateTime?>.ErrorResult("No active settlement schedule found for merchant", 404);
            }

            return ApiResponse<DateTime?>.SuccessResult(schedule.NextScheduledDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next scheduled date for merchant {MerchantId}", merchantId);
            return ApiResponse<DateTime?>.ErrorResult("An error occurred while retrieving the next scheduled date", 500);
        }
    }

    #region Private Helper Methods

    private ValidationResult ValidateScheduleConfiguration(SettlementScheduleConfigRequest request)
    {
        var result = new ValidationResult();

        // Validate frequency-specific requirements
        switch (request.Frequency)
        {
            case SettlementFrequency.Weekly:
                if (!request.DayOfWeek.HasValue || request.DayOfWeek < 1 || request.DayOfWeek > 7)
                {
                    result.AddError("DayOfWeek", "DayOfWeek must be specified for weekly frequency (1-7)");
                }
                break;

            case SettlementFrequency.BiWeekly:
                if (!request.DayOfWeek.HasValue || request.DayOfWeek < 1 || request.DayOfWeek > 7)
                {
                    result.AddError("DayOfWeek", "DayOfWeek must be specified for bi-weekly frequency (1-7)");
                }
                break;

            case SettlementFrequency.Monthly:
                if (!request.DayOfMonth.HasValue || request.DayOfMonth < 1 || request.DayOfMonth > 31)
                {
                    result.AddError("DayOfMonth", "DayOfMonth must be specified for monthly frequency (1-31)");
                }
                break;
        }

        // Validate processing time
        if (request.ProcessingHour < 0 || request.ProcessingHour > 23)
        {
            result.AddError("ProcessingHour", "ProcessingHour must be between 0 and 23");
        }

        if (request.ProcessingMinute < 0 || request.ProcessingMinute > 59)
        {
            result.AddError("ProcessingMinute", "ProcessingMinute must be between 0 and 59");
        }

        // Validate minimum amount
        if (request.MinimumAmount.HasValue && request.MinimumAmount <= 0)
        {
            result.AddError("MinimumAmount", "MinimumAmount must be greater than zero");
        }

        return result;
    }

    private DateTime? CalculateNextScheduledDate(SettlementSchedule schedule)
    {
        var now = DateTime.UtcNow;
        var processingTime = new TimeSpan(schedule.ProcessingHour, schedule.ProcessingMinute, 0);

        return schedule.Frequency switch
        {
            SettlementFrequency.Daily => GetNextDailyDate(now, processingTime),
            SettlementFrequency.Weekly => GetNextWeeklyDate(now, schedule.DayOfWeek!.Value, processingTime),
            SettlementFrequency.BiWeekly => GetNextBiWeeklyDate(now, schedule.DayOfWeek!.Value, processingTime, schedule.LastProcessedDate),
            SettlementFrequency.Monthly => GetNextMonthlyDate(now, schedule.DayOfMonth!.Value, processingTime),
            SettlementFrequency.Manual => null,
            _ => null
        };
    }

    private DateTime GetNextDailyDate(DateTime now, TimeSpan processingTime)
    {
        var today = now.Date.Add(processingTime);
        return today > now ? today : today.AddDays(1);
    }

    private DateTime GetNextWeeklyDate(DateTime now, int dayOfWeek, TimeSpan processingTime)
    {
        var targetDayOfWeek = (DayOfWeek)(dayOfWeek == 7 ? 0 : dayOfWeek); // Convert 7 to Sunday (0)
        var daysUntilTarget = ((int)targetDayOfWeek - (int)now.DayOfWeek + 7) % 7;
        
        var targetDate = now.Date.AddDays(daysUntilTarget).Add(processingTime);
        
        // If target is today but time has passed, schedule for next week
        if (daysUntilTarget == 0 && targetDate <= now)
        {
            targetDate = targetDate.AddDays(7);
        }
        
        return targetDate;
    }

    private DateTime GetNextBiWeeklyDate(DateTime now, int dayOfWeek, TimeSpan processingTime, DateTime? lastProcessed)
    {
        var nextWeekly = GetNextWeeklyDate(now, dayOfWeek, processingTime);
        
        // If no last processed date, start with next weekly
        if (!lastProcessed.HasValue)
        {
            return nextWeekly;
        }
        
        // If last processed was less than 2 weeks ago, add another week
        if ((now - lastProcessed.Value).TotalDays < 14)
        {
            return nextWeekly.AddDays(7);
        }
        
        return nextWeekly;
    }

    private DateTime GetNextMonthlyDate(DateTime now, int dayOfMonth, TimeSpan processingTime)
    {
        var currentMonth = now.Month;
        var currentYear = now.Year;
        
        // Try current month first
        var daysInCurrentMonth = DateTime.DaysInMonth(currentYear, currentMonth);
        var targetDay = Math.Min(dayOfMonth, daysInCurrentMonth);
        var targetDate = new DateTime(currentYear, currentMonth, targetDay).Add(processingTime);
        
        // If target date is in the future, use it
        if (targetDate > now)
        {
            return targetDate;
        }
        
        // Otherwise, move to next month
        var nextMonth = currentMonth == 12 ? 1 : currentMonth + 1;
        var nextYear = currentMonth == 12 ? currentYear + 1 : currentYear;
        var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
        var nextTargetDay = Math.Min(dayOfMonth, daysInNextMonth);
        
        return new DateTime(nextYear, nextMonth, nextTargetDay).Add(processingTime);
    }

    private async Task ProcessSingleScheduleAsync(SettlementScheduleConfig scheduleConfig, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing scheduled settlement for merchant {MerchantId}", scheduleConfig.MerchantId);

        // Calculate date range for settlement batch
        var toDate = DateTime.UtcNow.Date.AddDays(-1); // T-1 settlement
        var fromDate = scheduleConfig.LastProcessedDate?.Date.AddDays(1) ?? toDate.AddDays(-7); // Default to last 7 days

        // Create settlement batch request
        var batchRequest = new CreateSettlementBatchRequest
        {
            MerchantId = scheduleConfig.MerchantId,
            FromDate = fromDate,
            ToDate = toDate,
            MinimumAmount = scheduleConfig.MinimumAmount,
            AutoProcess = scheduleConfig.AutoProcess,
            Notes = $"Scheduled settlement - {scheduleConfig.Frequency}"
        };

        // Create settlement batch
        var result = await _settlementService.CreateSettlementBatchAsync(batchRequest, cancellationToken);

        if (result.Success)
        {
            // Update schedule with last processed date and next scheduled date
            var schedule = await _context.SettlementSchedules
                .FirstOrDefaultAsync(s => s.Id == scheduleConfig.Id, cancellationToken);

            if (schedule != null)
            {
                schedule.LastProcessedDate = DateTime.UtcNow;
                schedule.NextScheduledDate = CalculateNextScheduledDate(schedule);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully processed scheduled settlement for merchant {MerchantId}, batch ID: {BatchId}", 
                scheduleConfig.MerchantId, result.Data?.Id);
        }
        else
        {
            _logger.LogWarning("Failed to process scheduled settlement for merchant {MerchantId}: {Errors}", 
                scheduleConfig.MerchantId, string.Join("; ", result.Errors));
            throw new InvalidOperationException($"Settlement batch creation failed: {string.Join("; ", result.Errors)}");
        }
    }

    #endregion
}