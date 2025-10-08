using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YourCompanyBNPL.Payment.API.Configuration;
using YourCompanyBNPL.Payment.API.Services;
using System.Security.Claims;

namespace YourCompanyBNPL.Payment.API.Controllers;

/// <summary>
/// OpenID Connect authentication controller
/// Handles OIDC authentication flows for Google, Microsoft, and other providers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OpenIdConnectController : ControllerBase
{
    private readonly ILogger<OpenIdConnectController> _logger;
    private readonly OpenIdConnectSettings _oidcSettings;
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authenticationService;

    public OpenIdConnectController(
        ILogger<OpenIdConnectController> logger,
        IOptions<OpenIdConnectSettings> oidcSettings,
        IUserService userService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _oidcSettings = oidcSettings.Value;
        _userService = userService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Initiates OpenID Connect authentication with specified provider
    /// </summary>
    /// <param name="provider">Provider name (google, microsoft, generic)</param>
    /// <param name="returnUrl">URL to redirect after successful authentication</param>
    /// <returns>Authentication challenge</returns>
    [HttpGet("login/{provider}")]
    [AllowAnonymous]
    public IActionResult Login(string provider, [FromQuery] string? returnUrl = null)
    {
        try
        {
            _logger.LogInformation("Initiating OpenID Connect login for provider: {Provider}", provider);

            if (!_oidcSettings.Enabled)
            {
                return BadRequest(new { error = "OpenID Connect authentication is not enabled" });
            }

            var scheme = GetProviderScheme(provider);
            if (string.IsNullOrEmpty(scheme))
            {
                return BadRequest(new { error = $"Provider '{provider}' is not configured" });
            }

            // Store return URL in session
            if (!string.IsNullOrEmpty(returnUrl))
            {
                HttpContext.Session.SetString("OidcReturnUrl", returnUrl);
            }

            var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), new { provider }),
                Items =
                {
                    { "scheme", scheme }
                }
            };

            return Challenge(properties, scheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating OpenID Connect login for provider: {Provider}", provider);
            return StatusCode(500, new { error = "Internal server error during OpenID Connect authentication" });
        }
    }

    /// <summary>
    /// Handles OpenID Connect callback
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <returns>Authentication result</returns>
    [HttpGet("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(string provider)
    {
        try
        {
            _logger.LogInformation("Processing OpenID Connect callback for provider: {Provider}", provider);

            if (!User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("User is not authenticated in OpenID Connect callback");
                return BadRequest(new { error = "Authentication failed" });
            }

            // Extract user information from claims
            var claims = User.Claims.ToList();
            var userInfo = ExtractUserInfoFromClaims(claims, provider);

            // Create or update user in our system
            var user = await _userService.CreateOrUpdateUserFromOpenIdConnectAsync(userInfo, provider);

            // Generate JWT token for API access
            var token = await _authenticationService.GenerateJwtTokenAsync(user);

            // Get return URL from session
            var returnUrl = HttpContext.Session.GetString("OidcReturnUrl") ?? "/dashboard";

            _logger.LogInformation("OpenID Connect authentication successful for user: {UserId}", user.Id);

            return Ok(new
            {
                success = true,
                token = token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.Name,
                    roles = user.Roles,
                    provider = provider
                },
                returnUrl = returnUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OpenID Connect callback for provider: {Provider}", provider);
            return StatusCode(500, new { error = "Internal server error during OpenID Connect authentication" });
        }
    }

    /// <summary>
    /// Initiates OpenID Connect logout
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout/{provider}")]
    [Authorize]
    public async Task<IActionResult> Logout(string provider)
    {
        try
        {
            _logger.LogInformation("Initiating OpenID Connect logout for provider: {Provider}, user: {UserId}", 
                provider, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var scheme = GetProviderScheme(provider);
            if (string.IsNullOrEmpty(scheme))
            {
                return BadRequest(new { error = $"Provider '{provider}' is not configured" });
            }

            var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(LogoutCallback), new { provider })
            };

            // Clear session
            HttpContext.Session.Clear();

            _logger.LogInformation("OpenID Connect logout initiated for provider: {Provider}", provider);

            return SignOut(properties, scheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenID Connect logout for provider: {Provider}", provider);
            return StatusCode(500, new { error = "Internal server error during logout" });
        }
    }

    /// <summary>
    /// Handles OpenID Connect logout callback
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <returns>Logout result</returns>
    [HttpGet("logout-callback/{provider}")]
    [AllowAnonymous]
    public IActionResult LogoutCallback(string provider)
    {
        try
        {
            _logger.LogInformation("OpenID Connect logout callback for provider: {Provider}", provider);

            return Ok(new { success = true, message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OpenID Connect logout callback for provider: {Provider}", provider);
            return StatusCode(500, new { error = "Internal server error during logout callback" });
        }
    }

    /// <summary>
    /// Lists available OpenID Connect providers
    /// </summary>
    /// <returns>List of configured providers</returns>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult GetProviders()
    {
        try
        {
            var providers = new List<object>();

            if (_oidcSettings.Google?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "google",
                    name = "Google",
                    description = "Sign in with Google",
                    logo = "/images/google-logo.png",
                    scheme = "Google"
                });
            }

            if (_oidcSettings.Microsoft?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "microsoft",
                    name = "Microsoft",
                    description = "Sign in with Microsoft",
                    logo = "/images/microsoft-logo.png",
                    scheme = "Microsoft"
                });
            }

            if (_oidcSettings.Generic?.Enabled == true)
            {
                providers.Add(new
                {
                    id = "generic",
                    name = "Generic OIDC",
                    description = "Generic OpenID Connect provider",
                    logo = "/images/oidc-logo.png",
                    scheme = "Generic"
                });
            }

            return Ok(new { providers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenID Connect providers");
            return StatusCode(500, new { error = "Internal server error retrieving providers" });
        }
    }

    /// <summary>
    /// Gets user information from current OpenID Connect session
    /// </summary>
    /// <returns>User information</returns>
    [HttpGet("userinfo")]
    [Authorize]
    public IActionResult GetUserInfo()
    {
        try
        {
            var claims = User.Claims.ToList();
            var userInfo = new
            {
                id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                name = User.FindFirst(ClaimTypes.Name)?.Value,
                givenName = User.FindFirst(ClaimTypes.GivenName)?.Value,
                familyName = User.FindFirst(ClaimTypes.Surname)?.Value,
                picture = User.FindFirst("picture")?.Value,
                locale = User.FindFirst("locale")?.Value,
                verifiedEmail = User.FindFirst("verified_email")?.Value,
                preferredUsername = User.FindFirst("preferred_username")?.Value,
                tenantId = User.FindFirst("tid")?.Value,
                objectId = User.FindFirst("oid")?.Value,
                allClaims = claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information");
            return StatusCode(500, new { error = "Internal server error retrieving user information" });
        }
    }

    #region Private Helper Methods

    private string? GetProviderScheme(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => _oidcSettings.Google?.Enabled == true ? "Google" : null,
            "microsoft" => _oidcSettings.Microsoft?.Enabled == true ? "Microsoft" : null,
            "generic" => _oidcSettings.Generic?.Enabled == true ? "Generic" : null,
            _ => null
        };
    }

    private OpenIdConnectUserInfo ExtractUserInfoFromClaims(List<Claim> claims, string provider)
    {
        var userInfo = new OpenIdConnectUserInfo
        {
            Provider = provider
        };

        foreach (var claim in claims)
        {
            switch (claim.Type)
            {
                case ClaimTypes.NameIdentifier:
                case "sub":
                    userInfo.SubjectId = claim.Value;
                    break;

                case ClaimTypes.Email:
                case "email":
                    userInfo.Email = claim.Value;
                    break;

                case ClaimTypes.Name:
                case "name":
                    userInfo.Name = claim.Value;
                    break;

                case ClaimTypes.GivenName:
                case "given_name":
                    userInfo.FirstName = claim.Value;
                    break;

                case ClaimTypes.Surname:
                case "family_name":
                    userInfo.LastName = claim.Value;
                    break;

                case "picture":
                    userInfo.Picture = claim.Value;
                    break;

                case "locale":
                    userInfo.Locale = claim.Value;
                    break;

                case "verified_email":
                    if (bool.TryParse(claim.Value, out var verified))
                    {
                        userInfo.VerifiedEmail = verified;
                    }
                    break;

                case "preferred_username":
                    userInfo.PreferredUsername = claim.Value;
                    break;

                case "tid":
                    userInfo.TenantId = claim.Value;
                    break;

                case "oid":
                    userInfo.ObjectId = claim.Value;
                    break;

                case ClaimTypes.Role:
                case "roles":
                    userInfo.Roles.Add(claim.Value);
                    break;
            }
        }

        return userInfo;
    }

    #endregion
}

/// <summary>
/// OpenID Connect user information extracted from claims
/// </summary>
public class OpenIdConnectUserInfo
{
    public string Provider { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public bool VerifiedEmail { get; set; } = false;
    public string PreferredUsername { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
