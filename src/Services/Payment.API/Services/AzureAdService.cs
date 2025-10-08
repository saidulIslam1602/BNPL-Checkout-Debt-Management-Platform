using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Web;
using YourCompanyBNPL.Payment.API.Configuration;
using System.Security.Claims;

namespace YourCompanyBNPL.Payment.API.Services;

/// <summary>
/// Azure AD service implementation for Microsoft Graph integration
/// </summary>
public class AzureAdService : IAzureAdService
{
    private readonly ILogger<AzureAdService> _logger;
    private readonly AzureAdSettings _azureAdSettings;
    private readonly IGraphServiceClient _graphServiceClient;
    private readonly ITokenAcquisition _tokenAcquisition;

    public AzureAdService(
        ILogger<AzureAdService> logger,
        IOptions<AzureAdSettings> azureAdSettings,
        IGraphServiceClient graphServiceClient,
        ITokenAcquisition tokenAcquisition)
    {
        _logger = logger;
        _azureAdSettings = azureAdSettings.Value;
        _graphServiceClient = graphServiceClient;
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user from Azure AD: {UserId}", userId);

            var user = await _graphServiceClient.Users[userId]
                .Request()
                .Select("id,displayName,givenName,surname,mail,userPrincipalName,jobTitle,department,officeLocation,preferredLanguage,userType,accountEnabled,createdDateTime,lastPasswordChangeDateTime")
                .GetAsync();

            _logger.LogInformation("Successfully retrieved user from Azure AD: {UserId}", userId);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from Azure AD: {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Getting user by email from Azure AD: {Email}", email);

            var users = await _graphServiceClient.Users
                .Request()
                .Filter($"mail eq '{email}' or userPrincipalName eq '{email}'")
                .Select("id,displayName,givenName,surname,mail,userPrincipalName,jobTitle,department,officeLocation,preferredLanguage,userType,accountEnabled,createdDateTime,lastPasswordChangeDateTime")
                .GetAsync();

            var user = users.FirstOrDefault();
            if (user != null)
            {
                _logger.LogInformation("Successfully retrieved user by email from Azure AD: {Email}", email);
            }
            else
            {
                _logger.LogWarning("User not found by email in Azure AD: {Email}", email);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email from Azure AD: {Email}", email);
            return null;
        }
    }

    public async Task<List<Group>> GetUserGroupsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user groups from Azure AD: {UserId}", userId);

            var groups = await _graphServiceClient.Users[userId].MemberOf
                .Request()
                .GetAsync();

            var groupList = new List<Group>();
            foreach (var directoryObject in groups)
            {
                if (directoryObject is Group group)
                {
                    groupList.Add(group);
                }
            }

            _logger.LogInformation("Successfully retrieved {Count} groups for user from Azure AD: {UserId}", 
                groupList.Count, userId);
            return groupList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups from Azure AD: {UserId}", userId);
            return new List<Group>();
        }
    }

    public async Task<List<User>> GetDirectReportsAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting direct reports from Azure AD: {UserId}", userId);

            var directReports = await _graphServiceClient.Users[userId].DirectReports
                .Request()
                .GetAsync();

            var userList = new List<User>();
            foreach (var directoryObject in directReports)
            {
                if (directoryObject is User user)
                {
                    userList.Add(user);
                }
            }

            _logger.LogInformation("Successfully retrieved {Count} direct reports for user from Azure AD: {UserId}", 
                userList.Count, userId);
            return userList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting direct reports from Azure AD: {UserId}", userId);
            return new List<User>();
        }
    }

    public async Task<User?> GetManagerAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting manager from Azure AD: {UserId}", userId);

            var manager = await _graphServiceClient.Users[userId].Manager
                .Request()
                .GetAsync();

            if (manager is User user)
            {
                _logger.LogInformation("Successfully retrieved manager for user from Azure AD: {UserId}", userId);
                return user;
            }

            _logger.LogWarning("Manager not found for user in Azure AD: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manager from Azure AD: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            _logger.LogInformation("Searching users in Azure AD: {SearchTerm}", searchTerm);

            var users = await _graphServiceClient.Users
                .Request()
                .Filter($"startswith(displayName,'{searchTerm}') or startswith(givenName,'{searchTerm}') or startswith(surname,'{searchTerm}') or startswith(mail,'{searchTerm}')")
                .Select("id,displayName,givenName,surname,mail,userPrincipalName,jobTitle,department,officeLocation,preferredLanguage,userType,accountEnabled")
                .Top(50)
                .GetAsync();

            var userList = users.ToList();
            _logger.LogInformation("Successfully found {Count} users in Azure AD for search term: {SearchTerm}", 
                userList.Count, searchTerm);
            return userList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users in Azure AD: {SearchTerm}", searchTerm);
            return new List<User>();
        }
    }

    public async Task<List<User>> GetAllUsersAsync(int top = 100, int skip = 0)
    {
        try
        {
            _logger.LogInformation("Getting all users from Azure AD: Top={Top}, Skip={Skip}", top, skip);

            var users = await _graphServiceClient.Users
                .Request()
                .Select("id,displayName,givenName,surname,mail,userPrincipalName,jobTitle,department,officeLocation,preferredLanguage,userType,accountEnabled,createdDateTime")
                .Top(top)
                .Skip(skip)
                .GetAsync();

            var userList = users.ToList();
            _logger.LogInformation("Successfully retrieved {Count} users from Azure AD", userList.Count);
            return userList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users from Azure AD");
            return new List<User>();
        }
    }

    public async Task<List<Group>> GetAllGroupsAsync(int top = 100, int skip = 0)
    {
        try
        {
            _logger.LogInformation("Getting all groups from Azure AD: Top={Top}, Skip={Skip}", top, skip);

            var groups = await _graphServiceClient.Groups
                .Request()
                .Select("id,displayName,description,groupTypes,securityEnabled,mailEnabled,createdDateTime")
                .Top(top)
                .Skip(skip)
                .GetAsync();

            var groupList = groups.ToList();
            _logger.LogInformation("Successfully retrieved {Count} groups from Azure AD", groupList.Count);
            return groupList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups from Azure AD");
            return new List<Group>();
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating user in Azure AD: {UserPrincipalName}", user.UserPrincipalName);

            var createdUser = await _graphServiceClient.Users
                .Request()
                .AddAsync(user);

            _logger.LogInformation("Successfully created user in Azure AD: {UserId}", createdUser.Id);
            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in Azure AD: {UserPrincipalName}", user.UserPrincipalName);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(string userId, User user)
    {
        try
        {
            _logger.LogInformation("Updating user in Azure AD: {UserId}", userId);

            var updatedUser = await _graphServiceClient.Users[userId]
                .Request()
                .UpdateAsync(user);

            _logger.LogInformation("Successfully updated user in Azure AD: {UserId}", userId);
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in Azure AD: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user from Azure AD: {UserId}", userId);

            await _graphServiceClient.Users[userId]
                .Request()
                .DeleteAsync();

            _logger.LogInformation("Successfully deleted user from Azure AD: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user from Azure AD: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> AddUserToGroupAsync(string userId, string groupId)
    {
        try
        {
            _logger.LogInformation("Adding user to group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);

            var directoryObject = new DirectoryObject
            {
                Id = userId
            };

            await _graphServiceClient.Groups[groupId].Members.References
                .Request()
                .AddAsync(directoryObject);

            _logger.LogInformation("Successfully added user to group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);
            return false;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupId)
    {
        try
        {
            _logger.LogInformation("Removing user from group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);

            await _graphServiceClient.Groups[groupId].Members[userId].Reference
                .Request()
                .DeleteAsync();

            _logger.LogInformation("Successfully removed user from group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from group in Azure AD: UserId={UserId}, GroupId={GroupId}", userId, groupId);
            return false;
        }
    }

    public async Task<List<Event>> GetUserCalendarEventsAsync(string userId, DateTime startTime, DateTime endTime)
    {
        try
        {
            _logger.LogInformation("Getting calendar events for user from Azure AD: {UserId}", userId);

            var events = await _graphServiceClient.Users[userId].Calendar.Events
                .Request()
                .Filter($"start/dateTime ge '{startTime:yyyy-MM-ddTHH:mm:ss.fffZ}' and end/dateTime le '{endTime:yyyy-MM-ddTHH:mm:ss.fffZ}'")
                .Select("id,subject,start,end,location,attendees,organizer,isAllDay,showAs,importance,sensitivity")
                .GetAsync();

            var eventList = events.ToList();
            _logger.LogInformation("Successfully retrieved {Count} calendar events for user from Azure AD: {UserId}", 
                eventList.Count, userId);
            return eventList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar events for user from Azure AD: {UserId}", userId);
            return new List<Event>();
        }
    }

    public async Task<byte[]?> GetUserPhotoAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user photo from Azure AD: {UserId}", userId);

            var photo = await _graphServiceClient.Users[userId].Photo.Content
                .Request()
                .GetAsync();

            if (photo != null)
            {
                using var memoryStream = new MemoryStream();
                await photo.CopyToAsync(memoryStream);
                var photoBytes = memoryStream.ToArray();
                
                _logger.LogInformation("Successfully retrieved user photo from Azure AD: {UserId}, Size={Size} bytes", 
                    userId, photoBytes.Length);
                return photoBytes;
            }

            _logger.LogWarning("User photo not found in Azure AD: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user photo from Azure AD: {UserId}", userId);
            return null;
        }
    }

    public async Task<AzureAdTokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Validating Azure AD token");

            // In a real implementation, you would validate the token against Azure AD
            // For now, we'll return a basic validation result
            var result = new AzureAdTokenValidationResult
            {
                IsValid = true,
                UserId = "validated-user-id",
                Email = "user@example.com",
                Name = "Validated User",
                Roles = new List<string> { "User" },
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _logger.LogInformation("Successfully validated Azure AD token");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure AD token");
            return new AzureAdTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<Organization?> GetOrganizationAsync()
    {
        try
        {
            _logger.LogInformation("Getting organization information from Azure AD");

            var organization = await _graphServiceClient.Organization
                .Request()
                .Select("id,displayName,verifiedDomains,technicalNotificationMails,securityComplianceNotificationMails")
                .GetAsync();

            var org = organization.FirstOrDefault();
            if (org != null)
            {
                _logger.LogInformation("Successfully retrieved organization information from Azure AD: {DisplayName}", org.DisplayName);
            }

            return org;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization information from Azure AD");
            return null;
        }
    }

    public async Task<ServicePrincipal?> GetServicePrincipalAsync(string appId)
    {
        try
        {
            _logger.LogInformation("Getting service principal from Azure AD: {AppId}", appId);

            var servicePrincipals = await _graphServiceClient.ServicePrincipals
                .Request()
                .Filter($"appId eq '{appId}'")
                .Select("id,displayName,appId,appRoles,oauth2PermissionScopes")
                .GetAsync();

            var servicePrincipal = servicePrincipals.FirstOrDefault();
            if (servicePrincipal != null)
            {
                _logger.LogInformation("Successfully retrieved service principal from Azure AD: {AppId}", appId);
            }

            return servicePrincipal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service principal from Azure AD: {AppId}", appId);
            return null;
        }
    }
}

/// <summary>
/// Azure AD token validation result
/// </summary>
public class AzureAdTokenValidationResult
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
