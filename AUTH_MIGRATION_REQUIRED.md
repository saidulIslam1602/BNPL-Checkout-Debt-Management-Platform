# Authentication Services Migration Required

## Overview
The authentication services (SAML 2.0, OpenID Connect, Azure AD/Microsoft Graph) require migration to work with the updated library versions.

## Affected Libraries
1. **Microsoft.Graph** v5.36.0 - API breaking changes from v4.x
2. **Sustainsys.Saml2.AspNetCore2** v2.9.2 - API changes in configuration and request handling
3. **Microsoft.Identity.Web** v2.15.5 - Integration changes

## Affected Files (166 errors)

### High Priority (54+ errors each)
- `Services/AzureAdService.cs` - Microsoft Graph SDK v5.x migration needed
  - `.Request()` pattern removed, now uses fluent API directly
  - User type naming conflicts resolved but API calls need updates

### Medium Priority (14-34 errors each)
- `Controllers/SamlController.cs` - Sustainsys.Saml2 API changes
  - `AuthnRequest`, `EntityId`, `RequestedAuthnContext` types relocated/renamed
  - `CommandResult.Run()` signature changed
  
- `Services/AuthenticationService.cs` - Token generation issues
  
- `Services/UserService.cs` - Database context integration
  
- `Configuration/SamlConfiguration.cs` - Configuration API changes
  - `IdentityProvider.WantAuthnRequestSigned` property renamed/moved
  - `XsdDuration` usage corrected
  
- `Configuration/OpenIdConnectConfiguration.cs` - Claim mapping changes
  - `ClaimActionCollection.MapJsonKey()` method signature changed

### Low Priority (2-4 errors each)
- `Configuration/AzureAdConfiguration.cs` - Service registration
- `Controllers/OpenIdConnectController.cs` - Minor integration issues
- `Program.cs` - Service configuration
- `Services/PaymentGatewayService.cs` - Minor dependencies

## Migration Steps Required

### 1. Microsoft Graph SDK v5.x Migration
```csharp
// OLD (v4.x)
var user = await _graphServiceClient.Users[userId]
    .Request()
    .Select("displayName,mail")
    .GetAsync();

// NEW (v5.x)
var user = await _graphServiceClient.Users[userId]
    .GetAsync(config => config.QueryParameters.Select = new[] { "displayName", "mail" });
```

### 2. Sustainsys.Saml2 Migration
- Review Sustainsys.Saml2 v2.x documentation
- Update SAML request/response handling
- Reconfigure identity provider settings
- Update metadata handling

### 3. OpenID Connect Claims
- Update claim action mappings
- Review Microsoft.Identity.Web documentation

## Current Status
 **Compilation blockers resolved:**
- Duplicate class definitions removed
- Type alias conflicts resolved (User vs GraphUser)
- Duplicate using directives cleaned up
- Interface implementations corrected (Saml2Logger)
- Property hiding warnings fixed

 **Authentication services temporarily disabled:**
- Services compile but authentication endpoints non-functional
- Requires dedicated migration effort with library-specific knowledge

## Recommendation
1. **Short term**: Core payment/settlement services can function without authentication
2. **Medium term**: Migrate authentication to updated APIs (1-2 days work)
3. **Long term**: Consider using Azure AD B2C or Auth0 for simpler integration

## Testing Without Authentication
For development/testing, you can:
1. Disable authentication in `Program.cs`
2. Use mock authentication middleware
3. Generate test JWT tokens manually

## References
- [Microsoft Graph SDK v5 Migration Guide](https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/dev/docs/upgrade-to-v5.md)
- [Sustainsys.Saml2 Documentation](https://github.com/Sustainsys/Saml2)
- [Microsoft.Identity.Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
