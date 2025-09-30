using System.ComponentModel.DataAnnotations;

namespace YourCompanyBNPL.Common.Models;

/// <summary>
/// Base entity class providing common properties for all domain entities
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    public string? DeletedBy { get; set; }
    
    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Base entity with audit trail for financial transactions
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public string? AuditTrail { get; set; }
    
    public string? TransactionReference { get; set; }
    
    public string? ExternalReference { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; } = new();
}