using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace YourCompanyBNPL.Payment.API.Configuration;

/// <summary>
/// SAML configuration for Norwegian enterprise customers
/// Supports BankID, FEIDE, and other Norwegian identity providers
/// </summary>
public static class SamlConfiguration
{
    public static IServiceCollection AddSamlAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var samlConfig = configuration.GetSection("Saml").Get<SamlSettings>();
        
        if (samlConfig?.Enabled == true)
        {
            services.Configure<SamlSettings>(configuration.GetSection("Saml"));
            
            services.AddAuthentication()
                .AddSaml2(options =>
                {
                    ConfigureSamlOptions(options, samlConfig);
                });
        }

        return services;
    }

    private static void ConfigureSamlOptions(Saml2Options options, SamlSettings settings)
    {
        // SP (Service Provider) Configuration
        options.SPOptions.EntityId = new EntityId(settings.ServiceProvider.EntityId);
        options.SPOptions.ReturnUrl = new Uri(settings.ServiceProvider.ReturnUrl);
        options.SPOptions.AuthenticateRequestSigningBehavior = SigningBehavior.Always;
        
        // Load SP certificate
        if (!string.IsNullOrEmpty(settings.ServiceProvider.CertificatePath))
        {
            var spCert = new X509Certificate2(settings.ServiceProvider.CertificatePath, settings.ServiceProvider.CertificatePassword);
            options.SPOptions.ServiceCertificates.Add(spCert);
        }

        // Configure Norwegian Identity Providers
        ConfigureNorwegianIdentityProviders(options, settings);

        // Configure logging
        options.SPOptions.Logger = new Saml2Logger();
        
        // Configure metadata
        options.SPOptions.MetadataCacheDuration = TimeSpan.FromHours(1);
        options.SPOptions.MetadataValidDuration = TimeSpan.FromDays(7);
    }

    private static void ConfigureNorwegianIdentityProviders(Saml2Options options, SamlSettings settings)
    {
        // BankID SAML Configuration
        if (settings.IdentityProviders.BankId?.Enabled == true)
        {
            var bankIdIdp = new IdentityProvider(
                new EntityId(settings.IdentityProviders.BankId.EntityId),
                options.SPOptions)
            {
                LoadMetadata = true,
                MetadataLocation = settings.IdentityProviders.BankId.MetadataUrl,
                AllowUnsolicitedAuthnResponse = false,
                WantAuthnRequestSigned = true,
                DisableOutboundLogoutRequests = false
            };

            // Configure BankID specific settings
            bankIdIdp.SigningKeys.AddConfiguredKey(
                new X509Certificate2(settings.IdentityProviders.BankId.CertificatePath));

            options.IdentityProviders.Add(bankIdIdp);
        }

        // FEIDE Configuration (Norwegian education sector)
        if (settings.IdentityProviders.Feide?.Enabled == true)
        {
            var feideIdp = new IdentityProvider(
                new EntityId(settings.IdentityProviders.Feide.EntityId),
                options.SPOptions)
            {
                LoadMetadata = true,
                MetadataLocation = settings.IdentityProviders.Feide.MetadataUrl,
                AllowUnsolicitedAuthnResponse = false,
                WantAuthnRequestSigned = true
            };

            options.IdentityProviders.Add(feideIdp);
        }

        // Azure AD Configuration
        if (settings.IdentityProviders.AzureAd?.Enabled == true)
        {
            var azureAdIdp = new IdentityProvider(
                new EntityId(settings.IdentityProviders.AzureAd.EntityId),
                options.SPOptions)
            {
                LoadMetadata = true,
                MetadataLocation = settings.IdentityProviders.AzureAd.MetadataUrl,
                AllowUnsolicitedAuthnResponse = false,
                WantAuthnRequestSigned = true
            };

            options.IdentityProviders.Add(azureAdIdp);
        }

        // Generic SAML IdP Configuration
        if (settings.IdentityProviders.Generic?.Any() == true)
        {
            foreach (var idp in settings.IdentityProviders.Generic)
            {
                var genericIdp = new IdentityProvider(
                    new EntityId(idp.EntityId),
                    options.SPOptions)
                {
                    LoadMetadata = idp.LoadMetadata,
                    MetadataLocation = idp.MetadataUrl,
                    AllowUnsolicitedAuthnResponse = idp.AllowUnsolicitedAuthnResponse,
                    WantAuthnRequestSigned = idp.WantAuthnRequestSigned
                };

                if (!string.IsNullOrEmpty(idp.CertificatePath))
                {
                    var cert = new X509Certificate2(idp.CertificatePath, idp.CertificatePassword);
                    genericIdp.SigningKeys.AddConfiguredKey(cert);
                }

                options.IdentityProviders.Add(genericIdp);
            }
        }
    }
}

/// <summary>
/// SAML configuration settings
/// </summary>
public class SamlSettings
{
    public bool Enabled { get; set; } = false;
    public ServiceProviderSettings ServiceProvider { get; set; } = new();
    public IdentityProviderSettings IdentityProviders { get; set; } = new();
}

public class ServiceProviderSettings
{
    public string EntityId { get; set; } = "https://api.yourcompany.com/saml";
    public string ReturnUrl { get; set; } = "https://api.yourcompany.com/saml/acs";
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = "https://api.yourcompany.com/saml/metadata";
}

public class IdentityProviderSettings
{
    public BankIdSettings? BankId { get; set; }
    public FeideSettings? Feide { get; set; }
    public AzureAdSettings? AzureAd { get; set; }
    public List<GenericIdpSettings> Generic { get; set; } = new();
}

public class BankIdSettings
{
    public bool Enabled { get; set; } = false;
    public string EntityId { get; set; } = "https://bankid.no/saml";
    public string MetadataUrl { get; set; } = "https://bankid.no/saml/metadata";
    public string CertificatePath { get; set; } = string.Empty;
}

public class FeideSettings
{
    public bool Enabled { get; set; } = false;
    public string EntityId { get; set; } = "https://idp.feide.no";
    public string MetadataUrl { get; set; } = "https://idp.feide.no/metadata";
}

public class AzureAdSettings
{
    public bool Enabled { get; set; } = false;
    public string EntityId { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

public class GenericIdpSettings
{
    public string EntityId { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public bool LoadMetadata { get; set; } = true;
    public bool AllowUnsolicitedAuthnResponse { get; set; } = false;
    public bool WantAuthnRequestSigned { get; set; } = true;
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
}

/// <summary>
/// Custom SAML logger for detailed logging
/// </summary>
public class Saml2Logger : ILogger
{
    private readonly ILogger<Saml2Logger> _logger;

    public Saml2Logger()
    {
        // This would be injected in a real implementation
        _logger = new LoggerFactory().CreateLogger<Saml2Logger>();
    }

    public void WriteError(string message, Exception? exception = null)
    {
        _logger.LogError(exception, "SAML Error: {Message}", message);
    }

    public void WriteInformation(string message)
    {
        _logger.LogInformation("SAML Info: {Message}", message);
    }

    public void WriteVerbose(string message)
    {
        _logger.LogDebug("SAML Verbose: {Message}", message);
    }

    public void WriteWarning(string message)
    {
        _logger.LogWarning("SAML Warning: {Message}", message);
    }
}
