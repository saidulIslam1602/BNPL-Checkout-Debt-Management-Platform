using Microsoft.EntityFrameworkCore;
using YourCompanyBNPL.Payment.API.Data;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Payment.API.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// User service implementation for managing user accounts and authentication
/// </summary>
public class UserService : IUserService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(PaymentDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            throw;
        }
    }

    public async Task<User> CreateUserFromSamlAsync(SamlUserInfo userInfo)
    {
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ExternalId = userInfo.NameId,
                IdentityProvider = "SAML",
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user from SAML: {Email}", userInfo.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user from SAML: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task UpdateUserFromSamlAsync(User user, SamlUserInfo userInfo)
    {
        try
        {
            user.FirstName = userInfo.FirstName;
            user.LastName = userInfo.LastName;
            user.ExternalId = userInfo.NameId;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user from SAML: {Email}", userInfo.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user from SAML: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task<User> CreateUserFromOpenIdConnectAsync(OpenIdConnectUserInfo userInfo)
    {
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ExternalId = userInfo.Subject,
                IdentityProvider = userInfo.Provider,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user from OpenID Connect: {Email}", userInfo.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user from OpenID Connect: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task UpdateUserFromOpenIdConnectAsync(User user, OpenIdConnectUserInfo userInfo)
    {
        try
        {
            user.FirstName = userInfo.FirstName;
            user.LastName = userInfo.LastName;
            user.ExternalId = userInfo.Subject;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user from OpenID Connect: {Email}", userInfo.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user from OpenID Connect: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task<User> CreateUserFromAzureAdAsync(AzureAdUserInfo userInfo)
    {
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ExternalId = userInfo.ObjectId,
                IdentityProvider = "AzureAD",
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user from Azure AD: {Email}", userInfo.Email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user from Azure AD: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task UpdateUserFromAzureAdAsync(User user, AzureAdUserInfo userInfo)
    {
        try
        {
            user.FirstName = userInfo.FirstName;
            user.LastName = userInfo.LastName;
            user.ExternalId = userInfo.ObjectId;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user from Azure AD: {Email}", userInfo.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user from Azure AD: {Email}", userInfo.Email);
            throw;
        }
    }

    public async Task<User> GetOrCreateUserAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = email.Split('@')[0],
                LastName = string.Empty,
                IsActive = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Email}", email);
        }

        return user;
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        try
        {
            var user = await GetUserByEmailAsync(email);
            
            if (user == null || !user.IsActive)
                return null;

            // Hash the provided password and compare
            var hashedPassword = HashPassword(password);
            if (user.PasswordHash == hashedPassword)
            {
                await UpdateLastLoginAsync(user.Id);
                return user;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for: {Email}", email);
            throw;
        }
    }

    public async Task<User> CreateUserAsync(string email, string password, string firstName, string lastName)
    {
        try
        {
            var existingUser = await GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email {email} already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user account: {Email}", email);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", email);
            throw;
        }
    }

    public async Task UpdateUserProfileAsync(Guid userId, string firstName, string lastName)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user profile: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Soft delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            throw;
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
