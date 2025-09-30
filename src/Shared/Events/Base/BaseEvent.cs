using MediatR;
using System.Text.Json.Serialization;

namespace YourCompanyBNPL.Events.Base;

/// <summary>
/// Base class for all domain events in the system
/// </summary>
public abstract class BaseEvent : INotification
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; } = Guid.NewGuid();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    [JsonPropertyName("eventType")]
    public string EventType { get; } = string.Empty;
    
    [JsonPropertyName("aggregateId")]
    public Guid AggregateId { get; set; }
    
    [JsonPropertyName("aggregateType")]
    public string AggregateType { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
    
    [JsonPropertyName("causationId")]
    public string? CausationId { get; set; }
    
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; } = new();

    protected BaseEvent()
    {
        EventType = GetType().Name;
    }
}

/// <summary>
/// Base class for integration events that cross service boundaries
/// </summary>
public abstract class IntegrationEvent : BaseEvent
{
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceVersion")]
    public string ServiceVersion { get; set; } = "1.0.0";
    
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; } = 0;
    
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Base class for domain events that stay within service boundaries
/// </summary>
public abstract class DomainEvent : BaseEvent
{
    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; } = false;
    
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}