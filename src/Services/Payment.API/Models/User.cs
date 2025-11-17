using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YourCompanyBNPL.Common.Models;

namespace YourCompanyBNPL.Payment.API.Models;

/// <summary>
/// User account model for authentication and authorization
/// </summary>
[Table("Users")]
public class User : AuditableEntity
{
    [Required]
    [MaxLength(254)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(255)]
    public string? ExternalId { get; set; }

    [MaxLength(50)]
    public string? IdentityProvider { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; }

    public bool PhoneConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }

    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation properties
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}

/// <summary>
/// User role assignment
/// </summary>
[Table("UserRoles")]
public class UserRole : AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    // Navigation properties
    public User User { get; set; } = null!;
}
