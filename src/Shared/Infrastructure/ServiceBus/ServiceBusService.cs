using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RivertyBNPL.Shared.Infrastructure.ServiceBus;

/// <summary>
/// Azure Service Bus service for event-driven architecture
/// Handles real-time messaging between microservices in the Norwegian BNPL platform
/// </summary>
public interface IServiceBusService
{
    Task PublishEventAsync<T>(string topicName, T eventData, DateTime? scheduledEnqueueTime = null) where T : class;
    Task PublishEventAsync(string topicName, string message, DateTime? scheduledEnqueueTime = null);
    Task PublishBatchEventsAsync<T>(string topicName, IEnumerable<T> events) where T : class;
    Task<ServiceBusReceiver> CreateReceiverAsync(string topicName, string subscriptionName);
    Task<ServiceBusSender> CreateSenderAsync(string topicName);
    Task CloseAsync();
}

public class ServiceBusService : IServiceBusService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusService> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders;
    private readonly Dictionary<string, ServiceBusReceiver> _receivers;
    private readonly SemaphoreSlim _semaphore;

    public ServiceBusService(IConfiguration configuration, ILogger<ServiceBusService> logger)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus") 
                              ?? throw new InvalidOperationException("ServiceBus connection string not found");

        _client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets, // Better for Azure environments
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30)
            }
        });

        _logger = logger;
        _senders = new Dictionary<string, ServiceBusSender>();
        _receivers = new Dictionary<string, ServiceBusReceiver>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task PublishEventAsync<T>(string topicName, T eventData, DateTime? scheduledEnqueueTime = null) where T : class
    {
        try
        {
            var sender = await GetOrCreateSenderAsync(topicName);
            
            var messageBody = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var message = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = GetCorrelationId(eventData),
                Subject = typeof(T).Name,
                TimeToLive = TimeSpan.FromDays(7), // Norwegian regulation: keep audit trail for 7 days minimum
                ApplicationProperties =
                {
                    ["EventType"] = typeof(T).Name,
                    ["Source"] = "RivertyBNPL",
                    ["Version"] = "1.0",
                    ["Country"] = "NO",
                    ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                }
            };

            // Add Norwegian-specific properties
            if (eventData is INorwegianEvent norwegianEvent)
            {
                message.ApplicationProperties["CustomerId"] = norwegianEvent.CustomerId;
                message.ApplicationProperties["SSN"] = MaskSSN(norwegianEvent.SocialSecurityNumber);
                message.ApplicationProperties["Amount"] = norwegianEvent.Amount.ToString("F2");
                message.ApplicationProperties["Currency"] = "NOK";
            }

            if (scheduledEnqueueTime.HasValue)
            {
                message.ScheduledEnqueueTime = scheduledEnqueueTime.Value;
            }

            await sender.SendMessageAsync(message);

            _logger.LogInformation("Event published to topic {TopicName}. MessageId: {MessageId}, EventType: {EventType}", 
                topicName, message.MessageId, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to topic {TopicName}. EventType: {EventType}", 
                topicName, typeof(T).Name);
            throw;
        }
    }

    public async Task PublishEventAsync(string topicName, string message, DateTime? scheduledEnqueueTime = null)
    {
        try
        {
            var sender = await GetOrCreateSenderAsync(topicName);
            
            var serviceBusMessage = new ServiceBusMessage(message)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                TimeToLive = TimeSpan.FromDays(7),
                ApplicationProperties =
                {
                    ["Source"] = "RivertyBNPL",
                    ["Country"] = "NO",
                    ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                }
            };

            if (scheduledEnqueueTime.HasValue)
            {
                serviceBusMessage.ScheduledEnqueueTime = scheduledEnqueueTime.Value;
            }

            await sender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation("Message published to topic {TopicName}. MessageId: {MessageId}", 
                topicName, serviceBusMessage.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to topic {TopicName}", topicName);
            throw;
        }
    }

    public async Task PublishBatchEventsAsync<T>(string topicName, IEnumerable<T> events) where T : class
    {
        try
        {
            var sender = await GetOrCreateSenderAsync(topicName);
            var eventList = events.ToList();
            
            if (!eventList.Any())
            {
                _logger.LogWarning("No events to publish to topic {TopicName}", topicName);
                return;
            }

            // Service Bus batch size limit is 1MB, so we need to batch carefully
            const int maxBatchSize = 100;
            var batches = eventList.Chunk(maxBatchSize);

            foreach (var batch in batches)
            {
                using var messageBatch = await sender.CreateMessageBatchAsync();
                
                foreach (var eventData in batch)
                {
                    var messageBody = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    var message = new ServiceBusMessage(messageBody)
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString(),
                        CorrelationId = GetCorrelationId(eventData),
                        Subject = typeof(T).Name,
                        TimeToLive = TimeSpan.FromDays(7),
                        ApplicationProperties =
                        {
                            ["EventType"] = typeof(T).Name,
                            ["Source"] = "RivertyBNPL",
                            ["Version"] = "1.0",
                            ["Country"] = "NO",
                            ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                        }
                    };

                    if (!messageBatch.TryAddMessage(message))
                    {
                        // If we can't add the message, send the current batch and start a new one
                        if (messageBatch.Count > 0)
                        {
                            await sender.SendMessagesAsync(messageBatch);
                        }
                        
                        // Create a new batch and add the message
                        using var newBatch = await sender.CreateMessageBatchAsync();
                        if (!newBatch.TryAddMessage(message))
                        {
                            _logger.LogError("Message too large for Service Bus batch. EventType: {EventType}", typeof(T).Name);
                            continue;
                        }
                        await sender.SendMessagesAsync(newBatch);
                    }
                }

                if (messageBatch.Count > 0)
                {
                    await sender.SendMessagesAsync(messageBatch);
                }
            }

            _logger.LogInformation("Batch of {EventCount} events published to topic {TopicName}. EventType: {EventType}", 
                eventList.Count, topicName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch events to topic {TopicName}. EventType: {EventType}", 
                topicName, typeof(T).Name);
            throw;
        }
    }

    public async Task<ServiceBusReceiver> CreateReceiverAsync(string topicName, string subscriptionName)
    {
        try
        {
            var receiverKey = $"{topicName}/{subscriptionName}";
            
            await _semaphore.WaitAsync();
            try
            {
                if (_receivers.TryGetValue(receiverKey, out var existingReceiver))
                {
                    return existingReceiver;
                }

                var receiver = _client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.PeekLock, // Ensure message processing reliability
                    PrefetchCount = 10, // Optimize throughput
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5) // Handle long-running processes
                });

                _receivers[receiverKey] = receiver;
                
                _logger.LogInformation("Created Service Bus receiver for topic {TopicName}, subscription {SubscriptionName}", 
                    topicName, subscriptionName);
                
                return receiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Service Bus receiver for topic {TopicName}, subscription {SubscriptionName}", 
                topicName, subscriptionName);
            throw;
        }
    }

    public async Task<ServiceBusSender> CreateSenderAsync(string topicName)
    {
        return await GetOrCreateSenderAsync(topicName);
    }

    public async Task CloseAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                // Close all senders
                foreach (var sender in _senders.Values)
                {
                    await sender.CloseAsync();
                }
                _senders.Clear();

                // Close all receivers
                foreach (var receiver in _receivers.Values)
                {
                    await receiver.CloseAsync();
                }
                _receivers.Clear();

                // Close the client
                await _client.DisposeAsync();

                _logger.LogInformation("Service Bus service closed successfully");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing Service Bus service");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Private Methods

    private async Task<ServiceBusSender> GetOrCreateSenderAsync(string topicName)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_senders.TryGetValue(topicName, out var existingSender))
            {
                return existingSender;
            }

            var sender = _client.CreateSender(topicName, new ServiceBusSenderOptions
            {
                // No specific options needed for sender
            });

            _senders[topicName] = sender;
            
            _logger.LogDebug("Created Service Bus sender for topic {TopicName}", topicName);
            
            return sender;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string GetCorrelationId<T>(T eventData) where T : class
    {
        // Try to get correlation ID from the event data
        var correlationIdProperty = typeof(T).GetProperty("CorrelationId");
        if (correlationIdProperty != null && correlationIdProperty.PropertyType == typeof(string))
        {
            var correlationId = correlationIdProperty.GetValue(eventData) as string;
            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }
        }

        // Try to get ID property
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(eventData);
            if (id != null)
            {
                return id.ToString() ?? Guid.NewGuid().ToString();
            }
        }

        return Guid.NewGuid().ToString();
    }

    private static string MaskSSN(string? ssn)
    {
        if (string.IsNullOrEmpty(ssn) || ssn.Length < 4)
            return "****";
        
        return ssn[..2] + "****" + ssn[^2..];
    }

    #endregion
}

/// <summary>
/// Interface for Norwegian-specific events that contain sensitive data
/// </summary>
public interface INorwegianEvent
{
    string CustomerId { get; }
    string SocialSecurityNumber { get; }
    decimal Amount { get; }
}

/// <summary>
/// Base class for all domain events in the Norwegian BNPL system
/// </summary>
public abstract class NorwegianBNPLEvent : INorwegianEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
    public string Source { get; set; } = "RivertyBNPL";
    public string Version { get; set; } = "1.0";
    
    // Norwegian-specific properties
    public abstract string CustomerId { get; }
    public abstract string SocialSecurityNumber { get; }
    public abstract decimal Amount { get; }
    public string Currency { get; set; } = "NOK";
    public string Country { get; set; } = "NO";
}

/// <summary>
/// Event published when an installment is successfully paid
/// </summary>
public class InstallmentPaidEvent : NorwegianBNPLEvent
{
    public string InstallmentId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string GatewayTransactionId { get; set; } = string.Empty;
    
    public override string CustomerId { get; set; } = string.Empty;
    public override string SocialSecurityNumber { get; set; } = string.Empty;
    public override decimal Amount { get; set; }
}

/// <summary>
/// Event published when an installment needs to be retried
/// </summary>
public class InstallmentRetryEvent
{
    public string InstallmentId { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public int AttemptCount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an installment is escalated to collections
/// </summary>
public class CollectionsEscalationEvent : NorwegianBNPLEvent
{
    public string CaseId { get; set; } = string.Empty;
    public string InstallmentId { get; set; } = string.Empty;
    public int DaysOverdue { get; set; }
    public decimal LateFees { get; set; }
    public string EscalationReason { get; set; } = string.Empty;
    
    public override string CustomerId { get; set; } = string.Empty;
    public override string SocialSecurityNumber { get; set; } = string.Empty;
    public override decimal Amount { get; set; }
}

/// <summary>
/// Event published when a settlement is processed
/// </summary>
public class SettlementProcessedEvent
{
    public string SettlementId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NOK";
    public DateTime ProcessedAt { get; set; }
    public string BankTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a risk assessment is completed
/// </summary>
public class RiskAssessmentCompletedEvent : NorwegianBNPLEvent
{
    public string AssessmentId { get; set; } = string.Empty;
    public int CreditScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public bool IsApproved { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public override string CustomerId { get; set; } = string.Empty;
    public override string SocialSecurityNumber { get; set; } = string.Empty;
    public override decimal Amount => RequestedAmount;
}

/// <summary>
/// Event published when fraud is detected
/// </summary>
public class FraudDetectedEvent : NorwegianBNPLEvent
{
    public string FraudDetectionId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal FraudScore { get; set; }
    public List<string> FraudIndicators { get; set; } = new();
    public string Action { get; set; } = string.Empty; // BLOCK, REVIEW, ALLOW
    public Dictionary<string, object> Details { get; set; } = new();
    
    public override string CustomerId { get; set; } = string.Empty;
    public override string SocialSecurityNumber { get; set; } = string.Empty;
    public override decimal Amount { get; set; }
}