using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace YourCompanyBNPL.Payment.API.Configuration;

/// <summary>
/// Azure AD/Entra ID configuration for Norwegian enterprise customers
/// Provides integration with Microsoft Graph API and Azure AD authentication
/// </summary>
public static class AzureAdConfiguration
{
    public static IServiceCollection AddAzureAdAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAdConfig = configuration.GetSection("AzureAd").Get<AzureAdSettings>();
        
        if (azureAdConfig?.Enabled == true)
        {
            services.Configure<AzureAdSettings>(configuration.GetSection("AzureAd"));
            
            // Add Microsoft Identity Web
            services.AddMicrosoftIdentityWebApiAuthentication(configuration, "AzureAd")
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            // Add Microsoft Graph client
            services.AddScoped<GraphServiceClient>();
            services.AddScoped<IAzureAdService, AzureAdService>();
        }

        return services;
    }

    public static IServiceCollection AddAzureAdAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAdConfig = configuration.GetSection("AzureAd").Get<AzureAdSettings>();
        
        if (azureAdConfig?.Enabled == true)
        {
            services.AddAuthorization(options =>
            {
                // Add Azure AD specific policies
                options.AddPolicy("RequireAzureAdUser", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("iss", $"https://sts.windows.net/{azureAdConfig.TenantId}/");
                });

                options.AddPolicy("RequireAzureAdAdmin", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("iss", $"https://sts.windows.net/{azureAdConfig.TenantId}/");
                    policy.RequireRole("Admin", "Global Administrator", "User Administrator");
                });

                options.AddPolicy("RequireAzureAdGroup", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("iss", $"https://sts.windows.net/{azureAdConfig.TenantId}/");
                    policy.RequireClaim("groups", azureAdConfig.RequiredGroups.ToArray());
                });
            });
        }

        return services;
    }
}

/// <summary>
/// Azure AD configuration settings
/// </summary>
public class AzureAdSettings
{
    public bool Enabled { get; set; } = false;
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-oidc";
    public List<string> Scopes { get; set; } = new()
    {
        "https://graph.microsoft.com/User.Read",
        "https://graph.microsoft.com/Group.Read.All",
        "https://graph.microsoft.com/Directory.Read.All"
    };
    public List<string> RequiredGroups { get; set; } = new();
    public GraphSettings Graph { get; set; } = new();
    public B2CSettings B2C { get; set; } = new();
}

public class GraphSettings
{
    public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";
    public List<string> Scopes { get; set; } = new()
    {
        "User.Read",
        "Group.Read.All",
        "Directory.Read.All"
    };
}

public class B2CSettings
{
    public bool Enabled { get; set; } = false;
    public string Domain { get; set; } = string.Empty;
    public string SignUpSignInPolicyId { get; set; } = string.Empty;
    public string ResetPasswordPolicyId { get; set; } = string.Empty;
    public string EditProfilePolicyId { get; set; } = string.Empty;
}
