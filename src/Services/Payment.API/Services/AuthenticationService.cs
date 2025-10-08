using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Payment.API.Data;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Authentication service implementation supporting multiple authentication methods
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly PaymentDbContext _context;
    private readonly IUserService _userService;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpiryMinutes;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IConfiguration configuration,
        PaymentDbContext context,
        IUserService userService)
    {
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _userService = userService;
        
        _jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer not configured");
        _jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience not configured");
        _jwtExpiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        try
        {
            _logger.LogInformation("Generating JWT token for user: {UserId}", user.Id);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new("user_type", user.UserType.ToString()),
                new("created_at", user.CreatedAt.ToString("O")),
                new("jti", Guid.NewGuid().ToString())
            };

            // Add roles
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add Norwegian specific claims
            if (!string.IsNullOrEmpty(user.PersonalNumber))
            {
                claims.Add(new Claim("personal_number", user.PersonalNumber));
            }

            if (!string.IsNullOrEmpty(user.Organization))
            {
                claims.Add(new Claim("organization", user.Organization));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Store token in database for tracking
            await StoreTokenAsync(user.Id, tokenString, token.Id);

            _logger.LogInformation("JWT token generated successfully for user: {UserId}", user.Id);
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<User?> ValidateJwtTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Validating JWT token");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid user ID in JWT token");
                return null;
            }

            // Check if token is revoked
            var tokenId = jwtToken.Id;
            if (await IsTokenRevokedAsync(tokenId))
            {
                _logger.LogWarning("JWT token is revoked: {TokenId}", tokenId);
                return null;
            }

            // Get user from database
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for JWT token: {UserId}", userId);
                return null;
            }

            _logger.LogDebug("JWT token validated successfully for user: {UserId}", userId);
            return user;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("JWT token has invalid signature");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("Refreshing token");

            // Validate refresh token
            var tokenData = await ValidateRefreshTokenAsync(refreshToken);
            if (tokenData == null)
            {
                _logger.LogWarning("Invalid refresh token");
                return null;
            }

            // Get user
            var user = await _userService.GetUserByIdAsync(tokenData.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token: {UserId}", tokenData.UserId);
                return null;
            }

            // Generate new JWT token
            var newToken = await GenerateJwtTokenAsync(user);

            // Revoke old refresh token
            await RevokeRefreshTokenAsync(refreshToken);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Revoking token");

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            await RevokeTokenByIdAsync(jwtToken.Id);
            
            _logger.LogInformation("Token revoked successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    public async Task<User?> ValidateSamlAssertionAsync(string assertion)
    {
        try
        {
            _logger.LogInformation("Validating SAML assertion");

            // In a real implementation, this would validate the SAML assertion
            // For now, we'll extract user information from the assertion
            var userInfo = ExtractUserInfoFromSamlAssertion(assertion);
            if (userInfo == null)
            {
                _logger.LogWarning("Failed to extract user info from SAML assertion");
                return null;
            }

            // Find or create user
            var user = await _userService.GetUserByEmailAsync(userInfo.Email);
            if (user == null)
            {
                user = await _userService.CreateUserFromSamlAsync(userInfo);
            }
            else
            {
                await _userService.UpdateUserFromSamlAsync(user, userInfo);
            }

            _logger.LogInformation("SAML assertion validated successfully for user: {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SAML assertion");
            return null;
        }
    }

    public async Task<User?> ValidateOpenIdConnectTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Validating OpenID Connect token");

            // In a real implementation, this would validate the OpenID Connect token
            // against the identity provider's public keys
            var userInfo = await ValidateOpenIdConnectTokenWithProviderAsync(token);
            if (userInfo == null)
            {
                _logger.LogWarning("Failed to validate OpenID Connect token");
                return null;
            }

            // Find or create user
            var user = await _userService.GetUserByEmailAsync(userInfo.Email);
            if (user == null)
            {
                user = await _userService.CreateUserFromOpenIdConnectAsync(userInfo);
            }
            else
            {
                await _userService.UpdateUserFromOpenIdConnectAsync(user, userInfo);
            }

            _logger.LogInformation("OpenID Connect token validated successfully for user: {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OpenID Connect token");
            return null;
        }
    }

    public async Task<User?> ValidateAzureAdTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Validating Azure AD token");

            // In a real implementation, this would validate the Azure AD token
            // against Microsoft's public keys
            var userInfo = await ValidateAzureAdTokenWithMicrosoftAsync(token);
            if (userInfo == null)
            {
                _logger.LogWarning("Failed to validate Azure AD token");
                return null;
            }

            // Find or create user
            var user = await _userService.GetUserByEmailAsync(userInfo.Email);
            if (user == null)
            {
                user = await _userService.CreateUserFromAzureAdAsync(userInfo);
            }
            else
            {
                await _userService.UpdateUserFromAzureAdAsync(user, userInfo);
            }

            _logger.LogInformation("Azure AD token validated successfully for user: {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure AD token");
            return null;
        }
    }

    #region Private Helper Methods

    private async Task StoreTokenAsync(Guid userId, string token, string tokenId)
    {
        try
        {
            // In a real implementation, you would store this in a database
            // For now, we'll just log it
            _logger.LogDebug("Storing token for user: {UserId}, tokenId: {TokenId}", userId, tokenId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing token for user: {UserId}", userId);
        }
    }

    private async Task<bool> IsTokenRevokedAsync(string tokenId)
    {
        try
        {
            // In a real implementation, you would check a database or cache
            // For now, we'll return false (not revoked)
            await Task.CompletedTask;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is revoked: {TokenId}", tokenId);
            return true; // Assume revoked on error for security
        }
    }

    private async Task RevokeTokenByIdAsync(string tokenId)
    {
        try
        {
            // In a real implementation, you would mark the token as revoked in a database
            _logger.LogDebug("Revoking token: {TokenId}", tokenId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token: {TokenId}", tokenId);
        }
    }

    private async Task<RefreshTokenData?> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            // In a real implementation, you would validate the refresh token
            // For now, we'll return null
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return null;
        }
    }

    private async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            // In a real implementation, you would revoke the refresh token
            _logger.LogDebug("Revoking refresh token");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
        }
    }

    private SamlUserInfo? ExtractUserInfoFromSamlAssertion(string assertion)
    {
        try
        {
            // In a real implementation, you would parse the SAML assertion XML
            // For now, we'll return null
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user info from SAML assertion");
            return null;
        }
    }

    private async Task<OpenIdConnectUserInfo?> ValidateOpenIdConnectTokenWithProviderAsync(string token)
    {
        try
        {
            // In a real implementation, you would validate with the identity provider
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OpenID Connect token with provider");
            return null;
        }
    }

    private async Task<AzureAdUserInfo?> ValidateAzureAdTokenWithMicrosoftAsync(string token)
    {
        try
        {
            // In a real implementation, you would validate with Microsoft Graph
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure AD token with Microsoft");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Refresh token data
/// </summary>
public class RefreshTokenData
{
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}

/// <summary>
/// SAML user information
/// </summary>
public class SamlUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// OpenID Connect user information
/// </summary>
public class OpenIdConnectUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Azure AD user information
/// </summary>
public class AzureAdUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
