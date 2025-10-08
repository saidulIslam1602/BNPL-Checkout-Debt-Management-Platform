using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using YourCompanyBNPL.Payment.API.Configuration;
using YourCompanyBNPL.Payment.API.Services;
using System.Security.Claims;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// Azure AD controller for Microsoft Graph integration
/// Provides endpoints for Azure AD user and group management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AzureAdController : ControllerBase
{
    private readonly ILogger<AzureAdController> _logger;
    private readonly AzureAdSettings _azureAdSettings;
    private readonly IAzureAdService _azureAdService;

    public AzureAdController(
        ILogger<AzureAdController> logger,
        IOptions<AzureAdSettings> azureAdSettings,
        IAzureAdService azureAdService)
    {
        _logger = logger;
        _azureAdSettings = azureAdSettings.Value;
        _azureAdService = azureAdService;
    }

    /// <summary>
    /// Gets current user information from Azure AD
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Getting current user information from Azure AD: {UserId}", userId);

            var user = await _azureAdService.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found in Azure AD" });
            }

            var userInfo = new
            {
                id = user.Id,
                displayName = user.DisplayName,
                givenName = user.GivenName,
                surname = user.Surname,
                mail = user.Mail,
                userPrincipalName = user.UserPrincipalName,
                jobTitle = user.JobTitle,
                department = user.Department,
                officeLocation = user.OfficeLocation,
                preferredLanguage = user.PreferredLanguage,
                userType = user.UserType,
                accountEnabled = user.AccountEnabled,
                createdDateTime = user.CreatedDateTime,
                lastPasswordChangeDateTime = user.LastPasswordChangeDateTime
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving user information" });
        }
    }

    /// <summary>
    /// Gets user information by ID from Azure AD
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>User information</returns>
    [HttpGet("users/{userId}")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user information from Azure AD: {UserId}", userId);

            var user = await _azureAdService.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found in Azure AD" });
            }

            var userInfo = new
            {
                id = user.Id,
                displayName = user.DisplayName,
                givenName = user.GivenName,
                surname = user.Surname,
                mail = user.Mail,
                userPrincipalName = user.UserPrincipalName,
                jobTitle = user.JobTitle,
                department = user.Department,
                officeLocation = user.OfficeLocation,
                preferredLanguage = user.PreferredLanguage,
                userType = user.UserType,
                accountEnabled = user.AccountEnabled,
                createdDateTime = user.CreatedDateTime,
                lastPasswordChangeDateTime = user.LastPasswordChangeDateTime
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user information from Azure AD: {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error retrieving user information" });
        }
    }

    /// <summary>
    /// Gets user information by email from Azure AD
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>User information</returns>
    [HttpGet("users/by-email/{email}")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            _logger.LogInformation("Getting user by email from Azure AD: {Email}", email);

            var user = await _azureAdService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { error = "User not found in Azure AD" });
            }

            var userInfo = new
            {
                id = user.Id,
                displayName = user.DisplayName,
                givenName = user.GivenName,
                surname = user.Surname,
                mail = user.Mail,
                userPrincipalName = user.UserPrincipalName,
                jobTitle = user.JobTitle,
                department = user.Department,
                officeLocation = user.OfficeLocation,
                preferredLanguage = user.PreferredLanguage,
                userType = user.UserType,
                accountEnabled = user.AccountEnabled,
                createdDateTime = user.CreatedDateTime,
                lastPasswordChangeDateTime = user.LastPasswordChangeDateTime
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email from Azure AD: {Email}", email);
            return StatusCode(500, new { error = "Internal server error retrieving user information" });
        }
    }

    /// <summary>
    /// Gets current user's groups from Azure AD
    /// </summary>
    /// <returns>List of groups</returns>
    [HttpGet("me/groups")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUserGroups()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Getting current user groups from Azure AD: {UserId}", userId);

            var groups = await _azureAdService.GetUserGroupsAsync(userId);
            var groupInfo = groups.Select(g => new
            {
                id = g.Id,
                displayName = g.DisplayName,
                description = g.Description,
                groupTypes = g.GroupTypes,
                securityEnabled = g.SecurityEnabled,
                mailEnabled = g.MailEnabled,
                createdDateTime = g.CreatedDateTime
            }).ToList();

            return Ok(new { groups = groupInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user groups from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving user groups" });
        }
    }

    /// <summary>
    /// Gets user's groups from Azure AD
    /// </summary>
    /// <param name="userId">Azure AD user ID</param>
    /// <returns>List of groups</returns>
    [HttpGet("users/{userId}/groups")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetUserGroups(string userId)
    {
        try
        {
            _logger.LogInformation("Getting user groups from Azure AD: {UserId}", userId);

            var groups = await _azureAdService.GetUserGroupsAsync(userId);
            var groupInfo = groups.Select(g => new
            {
                id = g.Id,
                displayName = g.DisplayName,
                description = g.Description,
                groupTypes = g.GroupTypes,
                securityEnabled = g.SecurityEnabled,
                mailEnabled = g.MailEnabled,
                createdDateTime = g.CreatedDateTime
            }).ToList();

            return Ok(new { groups = groupInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups from Azure AD: {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error retrieving user groups" });
        }
    }

    /// <summary>
    /// Gets current user's manager from Azure AD
    /// </summary>
    /// <returns>Manager information</returns>
    [HttpGet("me/manager")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUserManager()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Getting current user manager from Azure AD: {UserId}", userId);

            var manager = await _azureAdService.GetManagerAsync(userId);
            if (manager == null)
            {
                return NotFound(new { error = "Manager not found in Azure AD" });
            }

            var managerInfo = new
            {
                id = manager.Id,
                displayName = manager.DisplayName,
                givenName = manager.GivenName,
                surname = manager.Surname,
                mail = manager.Mail,
                userPrincipalName = manager.UserPrincipalName,
                jobTitle = manager.JobTitle,
                department = manager.Department,
                officeLocation = manager.OfficeLocation
            };

            return Ok(managerInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user manager from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving manager information" });
        }
    }

    /// <summary>
    /// Gets current user's direct reports from Azure AD
    /// </summary>
    /// <returns>List of direct reports</returns>
    [HttpGet("me/direct-reports")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUserDirectReports()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Getting current user direct reports from Azure AD: {UserId}", userId);

            var directReports = await _azureAdService.GetDirectReportsAsync(userId);
            var directReportsInfo = directReports.Select(dr => new
            {
                id = dr.Id,
                displayName = dr.DisplayName,
                givenName = dr.GivenName,
                surname = dr.Surname,
                mail = dr.Mail,
                userPrincipalName = dr.UserPrincipalName,
                jobTitle = dr.JobTitle,
                department = dr.Department,
                officeLocation = dr.OfficeLocation
            }).ToList();

            return Ok(new { directReports = directReportsInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user direct reports from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving direct reports" });
        }
    }

    /// <summary>
    /// Searches for users in Azure AD
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>List of matching users</returns>
    [HttpGet("users/search")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest(new { error = "Search term is required" });
            }

            _logger.LogInformation("Searching users in Azure AD: {SearchTerm}", searchTerm);

            var users = await _azureAdService.SearchUsersAsync(searchTerm);
            var usersInfo = users.Select(u => new
            {
                id = u.Id,
                displayName = u.DisplayName,
                givenName = u.GivenName,
                surname = u.Surname,
                mail = u.Mail,
                userPrincipalName = u.UserPrincipalName,
                jobTitle = u.JobTitle,
                department = u.Department,
                officeLocation = u.OfficeLocation,
                accountEnabled = u.AccountEnabled
            }).ToList();

            return Ok(new { users = usersInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users in Azure AD: {SearchTerm}", searchTerm);
            return StatusCode(500, new { error = "Internal server error searching users" });
        }
    }

    /// <summary>
    /// Gets all users from Azure AD
    /// </summary>
    /// <param name="top">Number of users to return</param>
    /// <param name="skip">Number of users to skip</param>
    /// <returns>List of users</returns>
    [HttpGet("users")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int top = 100, [FromQuery] int skip = 0)
    {
        try
        {
            _logger.LogInformation("Getting all users from Azure AD: Top={Top}, Skip={Skip}", top, skip);

            var users = await _azureAdService.GetAllUsersAsync(top, skip);
            var usersInfo = users.Select(u => new
            {
                id = u.Id,
                displayName = u.DisplayName,
                givenName = u.GivenName,
                surname = u.Surname,
                mail = u.Mail,
                userPrincipalName = u.UserPrincipalName,
                jobTitle = u.JobTitle,
                department = u.Department,
                officeLocation = u.OfficeLocation,
                accountEnabled = u.AccountEnabled,
                createdDateTime = u.CreatedDateTime
            }).ToList();

            return Ok(new { users = usersInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving users" });
        }
    }

    /// <summary>
    /// Gets all groups from Azure AD
    /// </summary>
    /// <param name="top">Number of groups to return</param>
    /// <param name="skip">Number of groups to skip</param>
    /// <returns>List of groups</returns>
    [HttpGet("groups")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetAllGroups([FromQuery] int top = 100, [FromQuery] int skip = 0)
    {
        try
        {
            _logger.LogInformation("Getting all groups from Azure AD: Top={Top}, Skip={Skip}", top, skip);

            var groups = await _azureAdService.GetAllGroupsAsync(top, skip);
            var groupsInfo = groups.Select(g => new
            {
                id = g.Id,
                displayName = g.DisplayName,
                description = g.Description,
                groupTypes = g.GroupTypes,
                securityEnabled = g.SecurityEnabled,
                mailEnabled = g.MailEnabled,
                createdDateTime = g.CreatedDateTime
            }).ToList();

            return Ok(new { groups = groupsInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all groups from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving groups" });
        }
    }

    /// <summary>
    /// Gets current user's calendar events from Azure AD
    /// </summary>
    /// <param name="startTime">Start time for events</param>
    /// <param name="endTime">End time for events</param>
    /// <returns>List of calendar events</returns>
    [HttpGet("me/calendar")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUserCalendar([FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            var start = startTime ?? DateTime.UtcNow.Date;
            var end = endTime ?? DateTime.UtcNow.Date.AddDays(7);

            _logger.LogInformation("Getting current user calendar from Azure AD: {UserId}, Start={Start}, End={End}", 
                userId, start, end);

            var events = await _azureAdService.GetUserCalendarEventsAsync(userId, start, end);
            var eventsInfo = events.Select(e => new
            {
                id = e.Id,
                subject = e.Subject,
                start = e.Start,
                end = e.End,
                location = e.Location,
                attendees = e.Attendees,
                organizer = e.Organizer,
                isAllDay = e.IsAllDay,
                showAs = e.ShowAs,
                importance = e.Importance,
                sensitivity = e.Sensitivity
            }).ToList();

            return Ok(new { events = eventsInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user calendar from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving calendar events" });
        }
    }

    /// <summary>
    /// Gets current user's profile photo from Azure AD
    /// </summary>
    /// <returns>Profile photo</returns>
    [HttpGet("me/photo")]
    [Authorize(Policy = "RequireAzureAdUser")]
    public async Task<IActionResult> GetCurrentUserPhoto()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            _logger.LogInformation("Getting current user photo from Azure AD: {UserId}", userId);

            var photo = await _azureAdService.GetUserPhotoAsync(userId);
            if (photo == null)
            {
                return NotFound(new { error = "User photo not found in Azure AD" });
            }

            return File(photo, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user photo from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving user photo" });
        }
    }

    /// <summary>
    /// Gets organization information from Azure AD
    /// </summary>
    /// <returns>Organization information</returns>
    [HttpGet("organization")]
    [Authorize(Policy = "RequireAzureAdAdmin")]
    public async Task<IActionResult> GetOrganization()
    {
        try
        {
            _logger.LogInformation("Getting organization information from Azure AD");

            var organization = await _azureAdService.GetOrganizationAsync();
            if (organization == null)
            {
                return NotFound(new { error = "Organization not found in Azure AD" });
            }

            var orgInfo = new
            {
                id = organization.Id,
                displayName = organization.DisplayName,
                verifiedDomains = organization.VerifiedDomains,
                technicalNotificationMails = organization.TechnicalNotificationMails,
                securityComplianceNotificationMails = organization.SecurityComplianceNotificationMails
            };

            return Ok(orgInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization information from Azure AD");
            return StatusCode(500, new { error = "Internal server error retrieving organization information" });
        }
    }
}
