using Microsoft.Graph;
using Microsoft.Graph.Models;
using YourCompanyBNPL.Payment.API.Models;

// Alias to avoid naming conflicts
using GraphUser = Microsoft.Graph.Models.User;
using AppUser = YourCompanyBNPL.Payment.API.Models.User;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Azure AD service interface for Microsoft Graph integration
/// </summary>
public interface IAzureAdService
{
    /// <summary>
    /// Gets user information from Azure AD
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>User information</returns>
    Task<GraphUser?> GetUserAsync(string userId);

    /// <summary>
    /// Gets user information by email
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>User information</returns>
    Task<GraphUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Gets user's group memberships
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>List of groups</returns>
    Task<List<Group>> GetUserGroupsAsync(string userId);

    /// <summary>
    /// Gets user's direct reports
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>List of direct reports</returns>
    Task<List<GraphUser>> GetDirectReportsAsync(string userId);

    /// <summary>
    /// Gets user's manager
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>Manager information</returns>
    Task<GraphUser?> GetManagerAsync(string userId);

    /// <summary>
    /// Searches for users
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>List of matching users</returns>
    Task<List<GraphUser>> SearchUsersAsync(string searchTerm);

    /// <summary>
    /// Gets all users in the organization
    /// </summary>
    /// <param name="top">Number of users to return</param>
    /// <param name="skip">Number of users to skip</param>
    /// <returns>List of users</returns>
    Task<List<GraphUser>> GetAllUsersAsync(int top = 100, int skip = 0);

    /// <summary>
    /// Gets all groups in the organization
    /// </summary>
    /// <param name="top">Number of groups to return</param>
    /// <param name="skip">Number of groups to skip</param>
    /// <returns>List of groups</returns>
    Task<List<Group>> GetAllGroupsAsync(int top = 100, int skip = 0);

    /// <summary>
    /// Creates a new user in Azure AD
    /// </summary>
    /// <param name="user">User information</param>
    /// <returns>Created user</returns>
    Task<GraphUser> CreateUserAsync(GraphUser user);

    /// <summary>
    /// Updates user information in Azure AD
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <param name="user">Updated user information</param>
    /// <returns>Updated user</returns>
    Task<GraphUser> UpdateUserAsync(string userId, GraphUser user);

    /// <summary>
    /// Deletes a user from Azure AD
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteUserAsync(string userId);

    /// <summary>
    /// Adds user to a group
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <param name="groupId">Azure AD group ID</param>
    /// <returns>True if successful</returns>
    Task<bool> AddUserToGroupAsync(string userId, string groupId);

    /// <summary>
    /// Removes user from a group
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <param name="groupId">Azure AD group ID</param>
    /// <returns>True if successful</returns>
    Task<bool> RemoveUserFromGroupAsync(string userId, string groupId);

    /// <summary>
    /// Gets user's calendar events
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <param name="startTime">Start time for events</param>
    /// <param name="endTime">End time for events</param>
    /// <returns>List of calendar events</returns>
    Task<List<Event>> GetUserCalendarEventsAsync(string userId, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Gets user's profile photo
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>Profile photo as byte array</returns>
    Task<byte[]?> GetUserPhotoAsync(string userId);

    /// <summary>
    /// Validates Azure AD token
    /// </summary>
    /// <param name="token">Azure AD token</param>
    /// <returns>Token validation result</returns>
    Task<AzureAdTokenValidationResult> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets organization information
    /// </summary>
    /// <returns>Organization information</returns>
    Task<Organization?> GetOrganizationAsync();

    /// <summary>
    /// Gets service principal information
    /// </summary>
    /// <param name="appId">Application ID</param>
    /// <returns>Service principal information</returns>
    Task<ServicePrincipal?> GetServicePrincipalAsync(string appId);
}
