# Dynamic Configuration System

## Overview

This system provides database-driven configuration for password policies and application feature flags on a per-company basis. It allows different organizations to have different security policies and feature sets without code changes.

---

## Table of Contents

1. [Password Policy Management](#password-policy-management)
2. [Application Flags](#application-flags)
3. [Database Schema](#database-schema)
4. [API Endpoints](#api-endpoints)
5. [Usage Examples](#usage-examples)
6. [Integration with UnitOfWork](#integration-with-unitofwork)

---

## 1. Password Policy Management

### Features

- **Per-Company Policies**: Each company can have unique password requirements
- **Dynamic Validation**: Password policies are loaded from the database
- **Automatic Defaults**: If no policy exists, a secure default is created
- **Configurable Settings**:
  - Minimum/Maximum length
  - Character requirements (uppercase, lowercase, digits, special)
  - Common password blocking
  - Sequential/Repeating character blocking
  - Password expiration
  - Login attempt limits
  - Account lockout duration

### Database Model

```csharp
TblPasswordPolicy
├── PasswordPolicyID (PK)
├── CompanyID (FK to TblCompany)
├── MinimumLength (default: 12)
├── MaximumLength (default: 128)
├── RequireUppercase (default: true)
├── RequireLowercase (default: true)
├── RequireDigit (default: true)
├── RequireSpecialCharacter (default: true)
├── MinimumUniqueCharacters (default: 5)
├── ProhibitCommonPasswords (default: true)
├── ProhibitSequentialCharacters (default: true)
├── ProhibitRepeatingCharacters (default: true)
├── PasswordExpirationDays (nullable)
├── PasswordHistoryCount (nullable)
├── EnablePasswordExpiry (default: false)
├── MaxLoginAttempts (default: 5)
├── LockoutDurationMinutes (default: 30)
├── IsActive
├── CreatedDate
├── ModifiedDate
├── CreatedBy
├── ModifiedBy
└── Description
```

---

## 2. Application Flags

### Features

- **Feature Toggles**: Enable/disable features per company
- **A/B Testing**: Time-based flag activation
- **Dynamic Configuration**: Change app behavior without deployments
- **Typed Values**: Support for Boolean, Integer, String, CSV, JSON
- **Categorization**: Group flags by Security, Feature, UI, Integration
- **User Visibility**: Control which flags are shown to end-users
- **Read-Only Protection**: Prevent modification of critical flags

### Database Model

```csharp
TblApplicationFlag
├── FlagID (PK)
├── CompanyID (FK to TblCompany)
├── FlagName (unique per company)
├── FlagValue
├── DataType (String, Boolean, Integer, Decimal, JSON, CSV)
├── Description
├── PossibleValues (comma-separated or JSON)
├── DefaultValue
├── ShowToUser (for UI configuration)
├── Category (Security, Feature, UI, Integration)
├── IsActive
├── IsReadOnly (prevents deletion)
├── DisplayOrder
├── EffectiveFrom (schedule activation)
├── EffectiveTo (schedule deactivation)
├── ModuleNamespace (for grouping)
├── CreatedDate
├── ModifiedDate
├── CreatedBy
└── ModifiedBy
```

---

## 3. Database Schema

### Installation

Run the migration script:

```bash
sqlcmd -S localhost -d YourDatabase -i DBLayer/Migrations/001_Create_PasswordPolicy_ApplicationFlag_Tables.sql
```

Or use your preferred SQL client to execute:
`DBLayer/Migrations/001_Create_PasswordPolicy_ApplicationFlag_Tables.sql`

### Tables Created

1. **TblPasswordPolicy** - Password policy configurations
2. **TblApplicationFlag** - Feature flags and settings

### Indexes Created

- `IX_TblPasswordPolicy_CompanyID` - Unique index for active policies
- `IX_TblPasswordPolicy_IsActive` - Performance index
- `IX_TblApplicationFlag_CompanyID_IsActive` - Composite index
- `IX_TblApplicationFlag_Category` - Category filtering
- `IX_TblApplicationFlag_ShowToUser` - User-visible flags

### Views Created

- `vw_ActivePasswordPolicies` - Active policies with company info
- `vw_ActiveApplicationFlags` - Active flags with company info

---

## 4. API Endpoints

### Password Policy Endpoints

#### Get Policy for Company
```http
GET /api/v1/PasswordPolicy/company/{companyId}
```

**Response:**
```json
{
  "passwordPolicyID": 1,
  "companyID": 1,
  "minimumLength": 12,
  "maximumLength": 128,
  "requireUppercase": true,
  "requireLowercase": true,
  "requireDigit": true,
  "requireSpecialCharacter": true,
  "minimumUniqueCharacters": 5,
  "prohibitCommonPasswords": true,
  "prohibitSequentialCharacters": true,
  "prohibitRepeatingCharacters": true,
  "maxLoginAttempts": 5,
  "lockoutDurationMinutes": 30,
  "isActive": true
}
```

#### Get or Create Default Policy
```http
GET /api/v1/PasswordPolicy/company/{companyId}/ensure
```

Creates a default policy if none exists.

#### Create Policy (Admin Only)
```http
POST /api/v1/PasswordPolicy
Content-Type: application/json

{
  "companyID": 1,
  "minimumLength": 12,
  "requireUppercase": true,
  "requireLowercase": true,
  "requireDigit": true,
  "requireSpecialCharacter": true
}
```

#### Update Policy (Admin Only)
```http
PUT /api/v1/PasswordPolicy/{policyId}
Content-Type: application/json

{
  "passwordPolicyID": 1,
  "minimumLength": 14,
  "requireUppercase": true
}
```

#### Delete Policy (Admin Only)
```http
DELETE /api/v1/PasswordPolicy/{policyId}
```

---

### Application Flag Endpoints

#### Get Single Flag Value
```http
GET /api/v1/ApplicationFlag/company/{companyId}/flag/{flagName}
```

**Response:**
```json
{
  "flagName": "EnableTwoFactorAuth",
  "flagValue": "true"
}
```

#### Get Multiple Flags (Comma-Separated)
```http
GET /api/v1/ApplicationFlag/company/1/flags?flagNames=EnableTwoFactorAuth,SessionTimeoutMinutes,ThemeColor
```

**Response:**
```json
{
  "EnableTwoFactorAuth": "true",
  "SessionTimeoutMinutes": "30",
  "ThemeColor": "#007bff"
}
```

#### Get All Flags for Company
```http
GET /api/v1/ApplicationFlag/company/{companyId}/all
```

#### Get Active Flags
```http
GET /api/v1/ApplicationFlag/company/{companyId}/active
```

#### Get Flags by Category
```http
GET /api/v1/ApplicationFlag/company/{companyId}/category/Security
```

Categories: `Security`, `Feature`, `UI`, `Integration`

#### Get User-Visible Flags
```http
GET /api/v1/ApplicationFlag/company/{companyId}/user-visible
```

#### Create Flag (Admin Only)
```http
POST /api/v1/ApplicationFlag
Content-Type: application/json

{
  "companyID": 1,
  "flagName": "NewFeature",
  "flagValue": "true",
  "dataType": "Boolean",
  "description": "Enable new feature",
  "category": "Feature",
  "showToUser": true
}
```

#### Update Flag (Admin Only)
```http
PUT /api/v1/ApplicationFlag/{flagId}
Content-Type: application/json

{
  "flagID": 1,
  "flagValue": "false",
  "description": "Updated description",
  "showToUser": true,
  "isActive": true
}
```

#### Bulk Update Flags (Admin Only)
```http
PUT /api/v1/ApplicationFlag/company/{companyId}/bulk
Content-Type: application/json

{
  "EnableTwoFactorAuth": "true",
  "SessionTimeoutMinutes": "60",
  "ThemeColor": "#28a745"
}
```

---

## 5. Usage Examples

### Using Password Policy in Code

```csharp
// In your controller or service
private readonly IUnitOfWork _unitOfWork;
private readonly IPasswordPolicyService _policyService;

// Validate password using database policy
var result = await _policyService.ValidatePasswordAsync(password, companyId);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        // Display error to user
        Console.WriteLine(error);
    }
}

// Apply policy settings globally
await _policyService.ApplyPolicySettingsAsync(companyId);
```

### Using Application Flags in Code

#### Basic Flag Retrieval

```csharp
// In your controller or service
private readonly IUnitOfWork _unitOfWork;

public async Task<bool> IsFeatureEnabled(long companyId, string featureName)
{
    var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
    return await flagRepo.GetFlagValueAsync<bool>(companyId, featureName, false);
}
```

#### Get Multiple Flags

```csharp
var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
var flags = await flagRepo.GetFlagValuesAsync(companyId, "Feature1,Feature2,Setting1");

if (flags.TryGetValue("Feature1", out var feature1Value))
{
    // Use feature1Value
}
```

#### Typed Flag Values

```csharp
var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();

// Get as boolean
bool twoFactorEnabled = await flagRepo.GetFlagValueAsync<bool>(
    companyId, "EnableTwoFactorAuth", defaultValue: false);

// Get as integer
int timeout = await flagRepo.GetFlagValueAsync<int>(
    companyId, "SessionTimeoutMinutes", defaultValue: 30);

// Get as string
string theme = await flagRepo.GetFlagValueAsync<string>(
    companyId, "ThemeColor", defaultValue: "#007bff");
```

#### Feature Toggle Pattern

```csharp
public async Task<IActionResult> GetData(long companyId)
{
    var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
    var useNewAlgorithm = await flagRepo.GetFlagValueAsync<bool>(
        companyId, "UseNewDataAlgorithm", false);

    if (useNewAlgorithm)
    {
        return await GetDataWithNewAlgorithm();
    }
    else
    {
        return await GetDataWithOldAlgorithm();
    }
}
```

---

## 6. Integration with UnitOfWork

### UnitOfWork Pattern

Both repositories are fully integrated with your existing UnitOfWork architecture:

```csharp
public class MyService
{
    private readonly IUnitOfWork _unitOfWork;

    public MyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ProcessWithFlags(long companyId)
    {
        // Get repositories through UnitOfWork
        var policyRepo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
        var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();

        // Use within transaction
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var policy = await policyRepo.GetByCompanyIdAsync(companyId);
            var flags = await flagRepo.GetActiveByCompanyAsync(companyId);

            // Do work...

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
```

### Repository Registration

Repositories are automatically registered in DI:

```csharp
// In RepositoryDI.cs
services.AddScoped<IPasswordPolicyRepository, PasswordPolicyRepository>();
services.AddScoped<IApplicationFlagRepository, ApplicationFlagRepository>();
```

Access them through UnitOfWork:

```csharp
var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
```

---

## Sample Data

The migration script creates sample data for Company ID 1:

### Password Policy Sample
- Minimum Length: 12
- Requires: Uppercase, Lowercase, Digit, Special Character
- Blocks: Common passwords, Sequential, Repeating characters
- Max Login Attempts: 5
- Lockout Duration: 30 minutes

### Application Flag Samples

| Flag Name | Value | Type | Category | Description |
|-----------|-------|------|----------|-------------|
| EnableTwoFactorAuth | false | Boolean | Security | Enable 2FA |
| SessionTimeoutMinutes | 30 | Integer | Security | Session timeout |
| AllowedFileExtensions | .pdf,.docx,.xlsx | CSV | Security | Allowed uploads |
| MaxFileUploadSizeMB | 10 | Integer | Security | Max upload size |
| EnableMaintenanceMode | false | Boolean | Feature | Maintenance mode |
| ThemeColor | #007bff | String | UI | Primary color |
| EnableNotifications | true | Boolean | Feature | Push notifications |
| APIRateLimitPerMinute | 100 | Integer | Security | Rate limit |

---

## Best Practices

### Password Policies

1. **Always use database policies** instead of hardcoded rules
2. **Create policies during company onboarding**
3. **Test policies** before applying to production
4. **Document policy changes** in audit logs
5. **Review policies quarterly** for security updates

### Application Flags

1. **Use meaningful flag names** (PascalCase recommended)
2. **Always provide default values**
3. **Document possible values** for enums
4. **Use categories** to organize flags
5. **Mark critical flags as read-only**
6. **Use effective dates** for scheduled rollouts
7. **Clean up unused flags** periodically

### Performance

1. **Flags are cached** by default (consider adding caching layer)
2. **Batch flag retrieval** when possible
3. **Use indexed queries** (already implemented)
4. **Monitor database performance** for flag-heavy operations

---

## Security Considerations

### Password Policies

- Policies are validated on Create/Update
- Only Admins can modify policies
- Changes are audit-logged
- Soft deletes preserve history

### Application Flags

- Read-only flags cannot be modified or deleted
- Effective dates prevent premature activation
- Category-based access control
- Audit trail for all changes

---

## Troubleshooting

### Policy Not Applied

**Issue**: Password validation doesn't respect database policy

**Solutions**:
1. Ensure `IPasswordPolicyService` is injected
2. Call `ApplyPolicySettingsAsync()` during startup
3. Check that policy exists for company
4. Verify `IsActive = true`

### Flag Not Found

**Issue**: `GetFlagAsync()` returns null

**Solutions**:
1. Check flag is `IsActive = true`
2. Verify `EffectiveFrom` is not in future
3. Verify `EffectiveTo` is not in past
4. Check `CompanyID` matches
5. Verify flag name spelling

---

## Migration Checklist

- [ ] Run SQL migration script
- [ ] Verify tables created
- [ ] Insert company-specific policies
- [ ] Insert initial application flags
- [ ] Test API endpoints
- [ ] Update documentation
- [ ] Train admin users

---

**Version**: 1.0.0
**Last Updated**: January 15, 2025
**Maintained By**: Development Team
