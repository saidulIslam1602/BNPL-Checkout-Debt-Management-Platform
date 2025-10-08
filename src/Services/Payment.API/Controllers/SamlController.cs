using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sustainsys.Saml2;
using Sustainsys.Saml2.WebSso;
using YourCompanyBNPL.Payment.API.Configuration;
using YourCompanyBNPL.Payment.API.Services;
using System.Security.Claims;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// SAML authentication controller for Norwegian enterprise customers
/// Handles SAML authentication flows for BankID, FEIDE, and other identity providers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SamlController : ControllerBase
{
    private readonly ILogger<SamlController> _logger;
    private readonly SamlSettings _samlSettings;
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authenticationService;

    public SamlController(
        ILogger<SamlController> logger,
        IOptions<SamlSettings> samlSettings,
        IUserService userService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _samlSettings = samlSettings.Value;
        _userService = userService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Initiates SAML authentication with specified identity provider
    /// </summary>
    /// <param name="idp">Identity provider identifier (bankid, feide, azuread)</param>
    /// <param name="returnUrl">URL to redirect after successful authentication</param>
    /// <returns>SAML authentication request</returns>
    [HttpGet("login/{idp}")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string idp, [FromQuery] string? returnUrl = null)
    {
        try
        {
            _logger.LogInformation("Initiating SAML login for IdP: {IdP}", idp);

            if (!_samlSettings.Enabled)
            {
                return BadRequest(new { error = "SAML authentication is not enabled" });
            }

            var idpEntityId = GetIdentityProviderEntityId(idp);
            if (string.IsNullOrEmpty(idpEntityId))
            {
                return BadRequest(new { error = $"Identity provider '{idp}' is not configured" });
            }

            // Store return URL in session
            if (!string.IsNullOrEmpty(returnUrl))
            {
                HttpContext.Session.SetString("SamlReturnUrl", returnUrl);
            }

            // Create SAML authentication request
            var request = new AuthnRequest(
                new EntityId(idpEntityId),
                new Uri(_samlSettings.ServiceProvider.ReturnUrl),
                new Uri(_samlSettings.ServiceProvider.EntityId));

            // Add Norwegian-specific attributes
            request.RequestedAuthnContext = new RequestedAuthnContext
            {
                Comparison = AuthnContextComparisonType.Minimum,
                AuthnContextClassRef = new[]
                {
                    "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport",
                    "urn:oasis:names:tc:SAML:2.0:ac:classes:TimeSyncToken"
                }
            };

            // Generate relay state for tracking
            var relayState = Guid.NewGuid().ToString();
            HttpContext.Session.SetString($"SamlRelayState_{relayState}", idp);

            var commandResult = CommandFactory.GetCommand(CommandFactory.AcsCommandName)
                .Run(HttpContext, new EntityId(idpEntityId), new Uri(_samlSettings.ServiceProvider.ReturnUrl));

            return new Saml2RedirectResult(commandResult.Location, commandResult.RelayState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating SAML login for IdP: {IdP}", idp);
            return StatusCode(500, new { error = "Internal server error during SAML authentication" });
        }
    }

    /// <summary>
    /// Handles SAML assertion consumer service (ACS) response
    /// </summary>
    /// <returns>Authentication result</returns>
    [HttpPost("acs")]
    [AllowAnonymous]
    public async Task<IActionResult> Acs()
    {
        try
        {
            _logger.LogInformation("Processing SAML ACS response");

            var commandResult = CommandFactory.GetCommand(CommandFactory.AcsCommandName)
                .Run(HttpContext, null, null);

            if (commandResult.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Extract user information from SAML claims
                var claims = HttpContext.User.Claims.ToList();
                var userInfo = ExtractUserInfoFromClaims(claims);

                // Create or update user in our system
                var user = await _userService.CreateOrUpdateUserFromSamlAsync(userInfo);

                // Generate JWT token for API access
                var token = await _authenticationService.GenerateJwtTokenAsync(user);

                // Get return URL from session
                var returnUrl = HttpContext.Session.GetString("SamlReturnUrl") ?? "/dashboard";

                _logger.LogInformation("SAML authentication successful for user: {UserId}", user.Id);

                return Ok(new
                {
                    success = true,
                    token = token,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        name = user.Name,
                        roles = user.Roles
                    },
                    returnUrl = returnUrl
                });
            }

            _logger.LogWarning("SAML authentication failed");
            return BadRequest(new { error = "SAML authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SAML ACS response");
            return StatusCode(500, new { error = "Internal server error during SAML authentication" });
        }
    }

    /// <summary>
    /// Initiates SAML logout
    /// </summary>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            _logger.LogInformation("Initiating SAML logout for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var commandResult = CommandFactory.GetCommand(CommandFactory.LogoutCommandName)
                .Run(HttpContext, null, null);

            // Clear session
            HttpContext.Session.Clear();

            _logger.LogInformation("SAML logout completed");

            return Ok(new { success = true, message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SAML logout");
            return StatusCode(500, new { error = "Internal server error during logout" });
        }
    }

    /// <summary>
    /// Returns SAML metadata for this service provider
    /// </summary>
    /// <returns>SAML metadata XML</returns>
    [HttpGet("metadata")]
    [AllowAnonymous]
    public IActionResult Metadata()
    {
        try
        {
            var metadata = CommandFactory.GetCommand(CommandFactory.MetadataCommandName)
                .Run(HttpContext, null, null);

            return Content(metadata.Content, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAML metadata");
            return StatusCode(500, new { error = "Internal server error generating metadata" });
        }
    }

    /// <summary>
    /// Lists available identity providers
    /// </summary>
    /// <returns>List of configured identity providers</returns>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult GetProviders()
    {
        try
        {
            var providers = new List<object>();

            if (_samlSettings.IdentityProviders.BankId?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "bankid",
                    name = "BankID",
                    description = "Norwegian BankID authentication",
                    logo = "/images/bankid-logo.png"
                });
            }

            if (_samlSettings.IdentityProviders.Feide?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "feide",
                    name = "FEIDE",
                    description = "Norwegian education sector authentication",
                    logo = "/images/feide-logo.png"
                });
            }

            if (_samlSettings.IdentityProviders.AzureAd?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "azuread",
                    name = "Azure AD",
                    description = "Microsoft Azure Active Directory",
                    logo = "/images/azure-logo.png"
                });
            }

            return Ok(new { providers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SAML providers");
            return StatusCode(500, new { error = "Internal server error retrieving providers" });
        }
    }

    #region Private Helper Methods

    private string? GetIdentityProviderEntityId(string idp)
    {
        return idp.ToLowerInvariant() switch
        {
            "bankid" => _samlSettings.IdentityProviders.BankId?.EntityId,
            "feide" => _samlSettings.IdentityProviders.Feide?.EntityId,
            "azuread" => _samlSettings.IdentityProviders.AzureAd?.EntityId,
            _ => _samlSettings.IdentityProviders.Generic?.FirstOrDefault(x => 
                x.EntityId.Contains(idp, StringComparison.OrdinalIgnoreCase))?.EntityId
        };
    }

    private SamlUserInfo ExtractUserInfoFromClaims(List<Claim> claims)
    {
        var userInfo = new SamlUserInfo();

        foreach (var claim in claims)
        {
            switch (claim.Type)
            {
                case ClaimTypes.NameIdentifier:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":
                    userInfo.SubjectId = claim.Value;
                    break;

                case ClaimTypes.Email:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
                    userInfo.Email = claim.Value;
                    break;

                case ClaimTypes.Name:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                    userInfo.Name = claim.Value;
                    break;

                case ClaimTypes.GivenName:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":
                    userInfo.FirstName = claim.Value;
                    break;

                case ClaimTypes.Surname:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname":
                    userInfo.LastName = claim.Value;
                    break;

                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth":
                    if (DateTime.TryParse(claim.Value, out var birthDate))
                    {
                        userInfo.DateOfBirth = birthDate;
                    }
                    break;

                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode":
                    userInfo.PostalCode = claim.Value;
                    break;

                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country":
                    userInfo.Country = claim.Value;
                    break;

                case ClaimTypes.Role:
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role":
                    userInfo.Roles.Add(claim.Value);
                    break;

                // Norwegian specific claims
                case "http://schemas.bankid.no/claims/personalnumber":
                case "http://schemas.feide.no/claims/user/personalNumber":
                    userInfo.PersonalNumber = claim.Value;
                    break;

                case "http://schemas.feide.no/claims/user/org":
                    userInfo.Organization = claim.Value;
                    break;

                case "http://schemas.feide.no/claims/user/orgunit":
                    userInfo.OrganizationalUnit = claim.Value;
                    break;
            }
        }

        return userInfo;
    }

    #endregion
}

/// <summary>
/// SAML user information extracted from claims
/// </summary>
public class SamlUserInfo
{
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string OrganizationalUnit { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Custom SAML redirect result
/// </summary>
public class Saml2RedirectResult : IActionResult
{
    private readonly string _location;
    private readonly string? _relayState;

    public Saml2RedirectResult(string location, string? relayState = null)
    {
        _location = location;
        _relayState = relayState;
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.Redirect(_location);
        return Task.CompletedTask;
    }
}
