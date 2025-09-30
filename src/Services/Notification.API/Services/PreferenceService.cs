using YourCompanyBNPL.Common.Enums;
using YourCompanyBNPL.Notification.API.DTOs;

namespace YourCompanyBNPL.Notification.API.Services;

public class PreferenceService : IPreferenceService
{
    private readonly ILogger<PreferenceService> _logger;

    public PreferenceService(ILogger<PreferenceService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Updated preferences for user {UserId}", userId);
        return true;
    }

    public async Task<NotificationPreferences?> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new NotificationPreferences { UserId = userId };
    }

    public async Task<bool> IsOptedInAsync(Guid userId, string notificationType, NotificationChannel channel, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return true; // Default to opted-in
    }
}