using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RivertyBNPL.Notification.API.Data;
using RivertyBNPL.Notification.API.DTOs;
using RivertyBNPL.Notification.API.Models;
using RivertyBNPL.Common.Models;

namespace RivertyBNPL.Notification.API.Services;

/// <summary>
/// Preference service implementation
/// </summary>
public class PreferenceService : IPreferenceService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<PreferenceService> _logger;

    public PreferenceService(NotificationDbContext context, ILogger<PreferenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(Guid customerId, UpdatePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

            if (preference == null)
            {
                preference = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(preference);
            }

            preference.Preferences = JsonSerializer.Serialize(request.Preferences);
            preference.QuietHoursStart = request.QuietHoursStart;
            preference.QuietHoursEnd = request.QuietHoursEnd;
            preference.TimeZone = request.TimeZone;
            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var response = new PreferencesResponse
            {
                CustomerId = customerId,
                Preferences = request.Preferences,
                QuietHoursStart = request.QuietHoursStart,
                QuietHoursEnd = request.QuietHoursEnd,
                TimeZone = request.TimeZone,
                UpdatedAt = preference.UpdatedAt
            };

            return ApiResponse<PreferencesResponse>.Success(response, "Preferences updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preferences for customer {CustomerId}", customerId);
            return ApiResponse<PreferencesResponse>.Failure($"Failed to update preferences: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PreferencesResponse>> GetPreferencesAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

            if (preference == null)
            {
                // Return default preferences
                var defaultResponse = new PreferencesResponse
                {
                    CustomerId = customerId,
                    Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>(),
                    UpdatedAt = DateTime.UtcNow
                };
                return ApiResponse<PreferencesResponse>.Success(defaultResponse);
            }

            var preferences = !string.IsNullOrEmpty(preference.Preferences)
                ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<NotificationChannel, bool>>>(preference.Preferences)
                : new Dictionary<string, Dictionary<NotificationChannel, bool>>();

            var response = new PreferencesResponse
            {
                CustomerId = customerId,
                Preferences = preferences ?? new Dictionary<string, Dictionary<NotificationChannel, bool>>(),
                QuietHoursStart = preference.QuietHoursStart,
                QuietHoursEnd = preference.QuietHoursEnd,
                TimeZone = preference.TimeZone,
                UpdatedAt = preference.UpdatedAt
            };

            return ApiResponse<PreferencesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get preferences for customer {CustomerId}", customerId);
            return ApiResponse<PreferencesResponse>.Failure($"Failed to get preferences: {ex.Message}");
        }
    }

    public async Task<bool> IsOptedInAsync(Guid customerId, string notificationType, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

            if (preference == null || string.IsNullOrEmpty(preference.Preferences))
            {
                // Default to opted in if no preferences are set
                return true;
            }

            var preferences = JsonSerializer.Deserialize<Dictionary<string, Dictionary<NotificationChannel, bool>>>(preference.Preferences);
            
            if (preferences != null && 
                preferences.TryGetValue(notificationType, out var channelPreferences) &&
                channelPreferences.TryGetValue(channel, out var isOptedIn))
            {
                return isOptedIn;
            }

            // Default to opted in if specific preference not found
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check opt-in status for customer {CustomerId}", customerId);
            // Default to opted in on error to avoid blocking notifications
            return true;
        }
    }
}

/// <summary>
/// Campaign service implementation
/// </summary>
public class CampaignService : ICampaignService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(NotificationDbContext context, ILogger<CampaignService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<CampaignResponse>> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = new Campaign
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Channel = request.Channel,
                TemplateId = request.TemplateId,
                Status = CampaignStatus.Draft,
                ScheduledAt = request.ScheduledAt,
                TargetCriteria = request.TargetCriteria,
                Settings = request.Settings != null ? JsonSerializer.Serialize(request.Settings) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(campaign);
            return ApiResponse<CampaignResponse>.Success(response, "Campaign created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create campaign");
            return ApiResponse<CampaignResponse>.Failure($"Failed to create campaign: {ex.Message}");
        }
    }

    public async Task<ApiResponse<CampaignResponse>> GetCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return ApiResponse<CampaignResponse>.Failure("Campaign not found");
            }

            var response = MapToResponse(campaign);
            return ApiResponse<CampaignResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get campaign {CampaignId}", campaignId);
            return ApiResponse<CampaignResponse>.Failure($"Failed to get campaign: {ex.Message}");
        }
    }

    public async Task<PagedApiResponse<CampaignResponse>> ListCampaignsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _context.Campaigns.CountAsync(cancellationToken);

            var campaigns = await _context.Campaigns
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var responses = campaigns.Select(MapToResponse).ToList();

            return PagedApiResponse<CampaignResponse>.Success(responses, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list campaigns");
            return PagedApiResponse<CampaignResponse>.Failure($"Failed to list campaigns: {ex.Message}");
        }
    }

    public async Task<ApiResponse> StartCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return ApiResponse.Failure("Campaign not found");
            }

            if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Paused)
            {
                return ApiResponse.Failure("Only draft or paused campaigns can be started");
            }

            campaign.Status = CampaignStatus.Running;
            campaign.StartedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success("Campaign started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start campaign {CampaignId}", campaignId);
            return ApiResponse.Failure($"Failed to start campaign: {ex.Message}");
        }
    }

    public async Task<ApiResponse> PauseCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return ApiResponse.Failure("Campaign not found");
            }

            if (campaign.Status != CampaignStatus.Running)
            {
                return ApiResponse.Failure("Only running campaigns can be paused");
            }

            campaign.Status = CampaignStatus.Paused;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success("Campaign paused successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause campaign {CampaignId}", campaignId);
            return ApiResponse.Failure($"Failed to pause campaign: {ex.Message}");
        }
    }

    public async Task<ApiResponse> CancelCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return ApiResponse.Failure("Campaign not found");
            }

            if (campaign.Status == CampaignStatus.Completed || campaign.Status == CampaignStatus.Cancelled)
            {
                return ApiResponse.Failure("Campaign is already completed or cancelled");
            }

            campaign.Status = CampaignStatus.Cancelled;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse.Success("Campaign cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel campaign {CampaignId}", campaignId);
            return ApiResponse.Failure($"Failed to cancel campaign: {ex.Message}");
        }
    }

    private static CampaignResponse MapToResponse(Campaign campaign)
    {
        return new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Type = campaign.Type,
            Channel = campaign.Channel,
            TemplateId = campaign.TemplateId,
            Status = campaign.Status,
            ScheduledAt = campaign.ScheduledAt,
            StartedAt = campaign.StartedAt,
            CompletedAt = campaign.CompletedAt,
            TotalRecipients = campaign.TotalRecipients,
            SentCount = campaign.SentCount,
            DeliveredCount = campaign.DeliveredCount,
            FailedCount = campaign.FailedCount,
            OpenedCount = campaign.OpenedCount,
            ClickedCount = campaign.ClickedCount,
            TotalCost = campaign.TotalCost,
            Currency = campaign.Currency,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
        };
    }
}