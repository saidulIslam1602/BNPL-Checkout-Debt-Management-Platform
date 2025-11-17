using YourCompanyBNPL.Payment.API.Models;
using YourCompanyBNPL.Payment.API.DTOs;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// User service interface for managing user accounts and authentication
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>User object if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns>User object if found, null otherwise</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Creates a new user from SAML authentication
    /// </summary>
    /// <param name="userInfo">User information from SAML provider</param>
    /// <returns>Created user object</returns>
    Task<User> CreateUserFromSamlAsync(SamlUserInfo userInfo);

    /// <summary>
    /// Updates an existing user with SAML authentication data
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="userInfo">User information from SAML provider</param>
    /// <returns>Task</returns>
    Task UpdateUserFromSamlAsync(User user, SamlUserInfo userInfo);

    /// <summary>
    /// Creates a new user from OpenID Connect authentication
    /// </summary>
    /// <param name="userInfo">User information from OpenID Connect provider</param>
    /// <returns>Created user object</returns>
    Task<User> CreateUserFromOpenIdConnectAsync(OpenIdConnectUserInfo userInfo);

    /// <summary>
    /// Updates an existing user with OpenID Connect authentication data
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="userInfo">User information from OpenID Connect provider</param>
    /// <returns>Task</returns>
    Task UpdateUserFromOpenIdConnectAsync(User user, OpenIdConnectUserInfo userInfo);

    /// <summary>
    /// Creates a new user from Azure AD authentication
    /// </summary>
    /// <param name="userInfo">User information from Azure AD</param>
    /// <returns>Created user object</returns>
    Task<User> CreateUserFromAzureAdAsync(AzureAdUserInfo userInfo);

    /// <summary>
    /// Updates an existing user with Azure AD authentication data
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="userInfo">User information from Azure AD</param>
    /// <returns>Task</returns>
    Task UpdateUserFromAzureAdAsync(User user, AzureAdUserInfo userInfo);

    /// <summary>
    /// Gets or creates a user from email address
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns>User object</returns>
    Task<User> GetOrCreateUserAsync(string email);

    /// <summary>
    /// Updates user's last login timestamp
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>Task</returns>
    Task UpdateLastLoginAsync(Guid userId);

    /// <summary>
    /// Validates user credentials
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="password">The user's password</param>
    /// <returns>User object if credentials are valid, null otherwise</returns>
    Task<User?> ValidateCredentialsAsync(string email, string password);

    /// <summary>
    /// Creates a new user account
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="password">The user's password</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <returns>Created user object</returns>
    Task<User> CreateUserAsync(string email, string password, string firstName, string lastName);

    /// <summary>
    /// Updates user profile information
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <returns>Task</returns>
    Task UpdateUserProfileAsync(Guid userId, string firstName, string lastName);

    /// <summary>
    /// Deletes a user account
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteUserAsync(Guid userId);
}
