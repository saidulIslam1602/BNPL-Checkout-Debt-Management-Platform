using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace YourCompanyBNPL.Payment.API.Configuration;

/// <summary>
/// OpenID Connect configuration for Norwegian identity providers
/// Supports Google, Microsoft, and other OIDC providers
/// </summary>
public static class OpenIdConnectConfiguration
{
    public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var oidcConfig = configuration.GetSection("OpenIdConnect").Get<OpenIdConnectSettings>();
        
        if (oidcConfig?.Enabled == true)
        {
            services.Configure<OpenIdConnectSettings>(configuration.GetSection("OpenIdConnect"));
            
            services.AddAuthentication()
                .AddOpenIdConnect("Google", options =>
                {
                    ConfigureGoogleOpenIdConnect(options, oidcConfig.Google);
                })
                .AddOpenIdConnect("Microsoft", options =>
                {
                    ConfigureMicrosoftOpenIdConnect(options, oidcConfig.Microsoft);
                })
                .AddOpenIdConnect("Generic", options =>
                {
                    ConfigureGenericOpenIdConnect(options, oidcConfig.Generic);
                });
        }

        return services;
    }

    private static void ConfigureGoogleOpenIdConnect(OpenIdConnectOptions options, GoogleOidcSettings settings)
    {
        if (settings?.Enabled != true) return;

        options.Authority = "https://accounts.google.com";
        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-google";
        options.SignedOutCallbackPath = "/signout-google";
        options.SaveTokens = true;

        // Configure scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Configure claims mapping
        options.ClaimActions.MapJsonKey("picture", "picture");
        options.ClaimActions.MapJsonKey("locale", "locale");
        options.ClaimActions.MapJsonKey("verified_email", "verified_email");

        // Configure token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = true,
            ValidAudience = settings.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Configure events
        options.Events = new OpenIdConnectEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/error?message=" + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Add custom claims or user processing here
                return Task.CompletedTask;
            }
        };
    }

    private static void ConfigureMicrosoftOpenIdConnect(OpenIdConnectOptions options, MicrosoftOidcSettings settings)
    {
        if (settings?.Enabled != true) return;

        options.Authority = $"https://login.microsoftonline.com/{settings.TenantId}/v2.0";
        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-microsoft";
        options.SignedOutCallbackPath = "/signout-microsoft";
        options.SaveTokens = true;

        // Configure scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("User.Read");

        // Configure claims mapping
        options.ClaimActions.MapJsonKey("preferred_username", "preferred_username");
        options.ClaimActions.MapJsonKey("tid", "tid");
        options.ClaimActions.MapJsonKey("oid", "oid");

        // Configure token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://login.microsoftonline.com/{settings.TenantId}/v2.0",
            ValidateAudience = true,
            ValidAudience = settings.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Configure events
        options.Events = new OpenIdConnectEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/error?message=" + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Add custom claims or user processing here
                return Task.CompletedTask;
            }
        };
    }

    private static void ConfigureGenericOpenIdConnect(OpenIdConnectOptions options, GenericOidcSettings settings)
    {
        if (settings?.Enabled != true) return;

        options.Authority = settings.Authority;
        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = settings.CallbackPath;
        options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
        options.SaveTokens = true;

        // Configure scopes
        options.Scope.Clear();
        foreach (var scope in settings.Scopes)
        {
            options.Scope.Add(scope);
        }

        // Configure claims mapping
        if (settings.ClaimMappings?.Any() == true)
        {
            foreach (var mapping in settings.ClaimMappings)
            {
                options.ClaimActions.MapJsonKey(mapping.ClaimType, mapping.JsonKey);
            }
        }

        // Configure token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = settings.ValidateIssuer,
            ValidIssuer = settings.ValidIssuer,
            ValidateAudience = settings.ValidateAudience,
            ValidAudience = settings.ValidAudience,
            ValidateLifetime = settings.ValidateLifetime,
            ClockSkew = TimeSpan.FromMinutes(settings.ClockSkewMinutes)
        };

        // Configure events
        options.Events = new OpenIdConnectEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/error?message=" + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Add custom claims or user processing here
                return Task.CompletedTask;
            }
        };
    }
}

/// <summary>
/// OpenID Connect configuration settings
/// </summary>
public class OpenIdConnectSettings
{
    public bool Enabled { get; set; } = false;
    public GoogleOidcSettings Google { get; set; } = new();
    public MicrosoftOidcSettings Microsoft { get; set; } = new();
    public GenericOidcSettings Generic { get; set; } = new();
}

public class GoogleOidcSettings
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class MicrosoftOidcSettings
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class GenericOidcSettings
{
    public bool Enabled { get; set; } = false;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-oidc";
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };
    public List<ClaimMapping> ClaimMappings { get; set; } = new();
    public bool ValidateIssuer { get; set; } = true;
    public string ValidIssuer { get; set; } = string.Empty;
    public bool ValidateAudience { get; set; } = true;
    public string ValidAudience { get; set; } = string.Empty;
    public bool ValidateLifetime { get; set; } = true;
    public int ClockSkewMinutes { get; set; } = 5;
}

public class ClaimMapping
{
    public string ClaimType { get; set; } = string.Empty;
    public string JsonKey { get; set; } = string.Empty;
}
