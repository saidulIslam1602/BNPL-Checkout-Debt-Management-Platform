
using YourCompanyBNPL.Common.Enums;

namespace YourCompanyBNPL.Notification.API.Providers;

/// <summary>
/// Factory for creating notification providers
/// </summary>
public class NotificationProviderFactory : INotificationProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<NotificationChannel, Type> _providerTypes;
    private readonly ILogger<NotificationProviderFactory> _logger;

    public NotificationProviderFactory(IServiceProvider serviceProvider, ILogger<NotificationProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        _providerTypes = new Dictionary<NotificationChannel, Type>
        {
            { NotificationChannel.Email, typeof(EmailProvider) },
            { NotificationChannel.SMS, typeof(SmsProvider) },
            { NotificationChannel.Push, typeof(PushProvider) }
        };
    }

    public INotificationProvider GetProvider(NotificationChannel channel)
    {
        if (!_providerTypes.TryGetValue(channel, out var providerType))
        {
            throw new NotSupportedException($"Notification channel {channel} is not supported");
        }

        var provider = _serviceProvider.GetService(providerType) as INotificationProvider;
        if (provider == null)
        {
            throw new InvalidOperationException($"Failed to create provider for channel {channel}");
        }

        return provider;
    }

    public IEnumerable<INotificationProvider> GetAllProviders()
    {
        foreach (var providerType in _providerTypes.Values)
        {
            var provider = _serviceProvider.GetService(providerType) as INotificationProvider;
            if (provider != null)
            {
                yield return provider;
            }
            else
            {
                _logger.LogWarning("Failed to create provider of type {ProviderType}", providerType.Name);
            }
        }
    }
}