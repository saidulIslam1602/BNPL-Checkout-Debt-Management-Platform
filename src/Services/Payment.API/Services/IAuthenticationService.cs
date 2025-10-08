using YourCompanyBNPL.Payment.API.Models;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Authentication service interface for handling various authentication methods
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Generates JWT token for authenticated user
    /// </summary>
    /// <param name="user">User information</param>
    /// <returns>JWT token</returns>
    Task<string> GenerateJwtTokenAsync(User user);

    /// <summary>
    /// Validates JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User information if valid, null otherwise</returns>
    Task<User?> ValidateJwtTokenAsync(string token);

    /// <summary>
    /// Refreshes JWT token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New JWT token</returns>
    Task<string?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes JWT token
    /// </summary>
    /// <param name="token">Token to revoke</param>
    /// <returns>True if successful</returns>
    Task<bool> RevokeTokenAsync(string token);

    /// <summary>
    /// Validates SAML assertion
    /// </summary>
    /// <param name="assertion">SAML assertion</param>
    /// <returns>User information if valid</returns>
    Task<User?> ValidateSamlAssertionAsync(string assertion);

    /// <summary>
    /// Validates OpenID Connect token
    /// </summary>
    /// <param name="token">OpenID Connect token</param>
    /// <returns>User information if valid</returns>
    Task<User?> ValidateOpenIdConnectTokenAsync(string token);

    /// <summary>
    /// Validates Azure AD token
    /// </summary>
    /// <param name="token">Azure AD token</param>
    /// <returns>User information if valid</returns>
    Task<User?> ValidateAzureAdTokenAsync(string token);
}
