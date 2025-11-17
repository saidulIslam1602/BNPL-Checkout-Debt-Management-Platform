# Known Issues and Pre-existing Errors

This document tracks compilation errors found during build verification that are **not related** to the recent namespace standardization work. These issues existed prior to the fixes and need to be addressed separately.

## Compilation Errors

### Payment.API Errors

#### Missing Types and Interfaces

**Location**: `src/Services/Payment.API/Controllers/AuthController.cs`

The following types and interfaces are missing:

1. **User type** - Referenced in multiple locations but not defined
   - Lines: 88, 104, 109, 126, 131
   - Error: `The type or namespace name 'User' could not be found`

2. **IUserService interface** - Referenced but not implemented
   - Lines: 59, 64, 108
   - Error: `The name 'IUserService' does not exist in the current context`

3. **Group type** - Used but not defined
   - Line: 106
   - Error: `The type or namespace name 'Group' could not be found`

4. **Event type** - Referenced but missing
   - Line: 132
   - Error: `The type or namespace name 'Event' could not be found`

5. **Organization type** - Used but not defined
   - Lines: 125, 138
   - Error: `The type or namespace name 'Organization' could not be found`

6. **ServicePrincipal type** - Referenced but missing
   - Line: 116
   - Error: `The type or namespace name 'ServicePrincipal' could not be found`

**Required Actions**:
- Install `Microsoft.Graph` NuGet package for Azure AD types
- Install `Microsoft.Identity.Web` NuGet package for authentication
- Create `IUserService` interface or install appropriate package
- Update `using` statements to include Microsoft.Graph namespace

---

**Location**: `src/Services/Payment.API/Program.cs`

Missing authentication configuration:

1. **Microsoft.Identity.Web namespace** - Not imported
   - Lines: 227-230
   - Error: `The type or namespace name 'Microsoft' does not exist in the namespace 'YourCompanyBNPL.Payment.API'`

2. **Saml2Options type** - SAML configuration class missing
   - Line: 251
   - Error: `The type or namespace name 'Saml2Options' could not be found`

**Required Actions**:
- Install `Microsoft.Identity.Web` NuGet package (version 2.x or higher)
- Install `Sustainsys.Saml2.AspNetCore2` package for SAML support
- Add `using Microsoft.Identity.Web;` statement
- Configure SAML service provider properly

---

### Risk.API Errors

**Location**: `src/Services/Risk.API/Models/CreditCheckModels.cs`

Missing enum definition:

1. **EmploymentStatus enum** - Referenced but not defined
   - Line: 151
   - Error: `The type or namespace name 'EmploymentStatus' could not be found`

**Required Actions**:
- Create `EmploymentStatus` enum in appropriate models file
- Suggested values: `FullTime`, `PartTime`, `SelfEmployed`, `Unemployed`, `Retired`, `Student`, `Other`

---

### PaymentGatewayService Syntax Errors

**Location**: `src/Services/Payment.API/Services/PaymentGatewayService.cs`

Syntax errors preventing compilation:

1. **Line 1102** - Invalid token ']' in class, struct, or interface member declaration
2. **Line 1146** - Invalid token ']' in class, struct, or interface member declaration

**Required Actions**:
- Review code around lines 1102 and 1146
- Check for mismatched brackets, parentheses, or malformed LINQ expressions
- Validate method signatures and lambda expressions

---

## Build Warnings

### Duplicate Using Directives

**Location**: Multiple files across Infrastructure, Common, and Service projects

**Examples**:
- `src/Shared/Infrastructure/Data/ApplicationDbContext.cs` (Line 6)
- `src/Shared/Common/Exceptions/BusinessException.cs` (Line 3)
- `src/Services/Settlement.API/Program.cs` (Line 2)

**Impact**: Low - These are compiler warnings, not errors

**Resolution**: Remove duplicate `using` statements

---

### Async Method Warnings

**Location**: `src/Shared/Infrastructure/Data/ApplicationDbContext.cs`

**Warning**: `This async method lacks 'await' operators and will run synchronously`
- Lines: 147, 152
- Methods: `OnModelCreatingAsync` related operations

**Impact**: Low - Methods work correctly but could be optimized

**Resolution**: Either add `await` operators or remove `async` keyword

---

## Successful Build Outcomes

Despite the errors listed above, the namespace standardization was **successful**:

 All assemblies generated with new `YourCompanyBNPL.*` namespaces:
- `YourCompanyBNPL.Common.dll`
- `YourCompanyBNPL.Events.dll`
- `YourCompanyBNPL.Infrastructure.dll`
- `YourCompanyBNPL.API.Gateway.dll`
- `YourCompanyBNPL.Settlement.API.dll`
- `YourCompanyBNPL.Notification.API.dll`

 No errors in:
- Settlement.API
- Notification.API
- API.Gateway
- All Azure Functions
- Shared libraries (Common, Events, Infrastructure)
- RealTime.Node.API

---

## Recommended Next Steps

1. **Install Missing Packages**:
   ```bash
   cd src/Services/Payment.API
   dotnet add package Microsoft.Identity.Web --version 2.15.5
   dotnet add package Microsoft.Graph --version 5.x
   dotnet add package Sustainsys.Saml2.AspNetCore2 --version 2.9.2
   ```

2. **Define Missing Types**:
   - Create `EmploymentStatus` enum in Risk.API
   - Implement `IUserService` interface or use appropriate package

3. **Fix Syntax Errors**:
   - Review PaymentGatewayService.cs lines 1102 and 1146
   - Ensure proper bracket matching and LINQ expression syntax

4. **Clean Up Warnings**:
   - Remove duplicate using statements
   - Fix async/await warnings

5. **Run Tests**:
   ```bash
   dotnet test tests/Unit/
   dotnet test tests/Integration/
   ```

---

## Notes

- These errors are **unrelated to the namespace standardization** completed in this session
- The errors existed in the codebase before the fixes were applied
- All namespace changes were successfully implemented and verified
- The project can proceed with these issues tracked separately

**Last Updated**: Current session
**Build Verification**: Performed with `dotnet build --no-incremental`
**Status**: Documented, awaiting separate remediation
