# Quick Start: Dynamic Configuration System

## 🚀 Get Started in 5 Minutes

### 1. Run Database Migration

```sql
-- Execute this SQL script
-- Location: DBLayer/Migrations/001_Create_PasswordPolicy_ApplicationFlag_Tables.sql

-- Option A: SQL Server Management Studio
-- Open file and execute

-- Option B: sqlcmd
sqlcmd -S localhost -d YourDatabase -i "DBLayer/Migrations/001_Create_PasswordPolicy_ApplicationFlag_Tables.sql"

-- Option C: EF Core Migration (if preferred)
-- Add-Migration AddPasswordPolicyAndFlags
-- Update-Database
```

### 2. Verify Tables Created

```sql
SELECT * FROM TblPasswordPolicy;
SELECT * FROM TblApplicationFlag;
```

### 3. Test API Endpoints

Start your API and use Swagger or Postman:

```http
# Get password policy for Company 1
GET /api/v1/PasswordPolicy/company/1

# Get application flags
GET /api/v1/ApplicationFlag/company/1/active
```

---

## 📘 Common Use Cases

### Use Case 1: Get Multiple Flags at Once

```http
GET /api/v1/ApplicationFlag/company/1/flags?flagNames=EnableTwoFactorAuth,SessionTimeoutMinutes,ThemeColor
```

**Response:**
```json
{
  "EnableTwoFactorAuth": "false",
  "SessionTimeoutMinutes": "30",
  "ThemeColor": "#007bff"
}
```

### Use Case 2: Get Password Policy

```http
GET /api/v1/PasswordPolicy/company/1
```

**Response:**
```json
{
  "passwordPolicyID": 1,
  "companyID": 1,
  "minimumLength": 12,
  "requireUppercase": true,
  "requireLowercase": true,
  "requireDigit": true,
  "requireSpecialCharacter": true
}
```

### Use Case 3: Use Flags in Your Code

```csharp
// Inject IUnitOfWork in your service/controller
private readonly IUnitOfWork _unitOfWork;

public async Task<IActionResult> MyAction(long companyId)
{
    // Get flag repository
    var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();

    // Get a boolean flag
    bool twoFactorEnabled = await flagRepo.GetFlagValueAsync<bool>(
        companyId, "EnableTwoFactorAuth", defaultValue: false);

    if (twoFactorEnabled)
    {
        // Show 2FA setup
    }

    // Get an integer flag
    int timeout = await flagRepo.GetFlagValueAsync<int>(
        companyId, "SessionTimeoutMinutes", defaultValue: 30);

    // Use timeout value...
}
```

### Use Case 4: Validate Password with Database Policy

```csharp
// Inject IPasswordPolicyService
private readonly Repositories.Services.IPasswordPolicyService _policyService;

public async Task<bool> ValidatePassword(string password, long companyId)
{
    var result = await _policyService.ValidatePasswordAsync(password, companyId);

    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            // Display error: "Password must be at least 12 characters long"
            Console.WriteLine(error);
        }
        return false;
    }

    return true;
}
```

---

## 🔧 Quick Configuration

### Add a New Flag (Admin)

```http
POST /api/v1/ApplicationFlag
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "companyID": 1,
  "flagName": "EnableNewFeature",
  "flagValue": "true",
  "dataType": "Boolean",
  "description": "Enable the new feature for this company",
  "category": "Feature",
  "showToUser": true,
  "defaultValue": "false"
}
```

### Update Password Policy (Admin)

```http
PUT /api/v1/PasswordPolicy/1
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "passwordPolicyID": 1,
  "minimumLength": 14,
  "maximumLength": 128,
  "requireUppercase": true,
  "requireLowercase": true,
  "requireDigit": true,
  "requireSpecialCharacter": true,
  "minimumUniqueCharacters": 6
}
```

### Bulk Update Flags (Admin)

```http
PUT /api/v1/ApplicationFlag/company/1/bulk
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "EnableTwoFactorAuth": "true",
  "SessionTimeoutMinutes": "60",
  "ThemeColor": "#28a745",
  "EnableMaintenanceMode": "false"
}
```

---

## 📊 Sample Data

The migration script automatically creates these sample flags for Company ID 1:

| Flag Name | Value | Type | Description |
|-----------|-------|------|-------------|
| EnableTwoFactorAuth | false | Boolean | Enable 2FA |
| SessionTimeoutMinutes | 30 | Integer | Session timeout |
| AllowedFileExtensions | .pdf,.docx,.xlsx | CSV | Allowed uploads |
| MaxFileUploadSizeMB | 10 | Integer | Max upload size |
| EnableMaintenanceMode | false | Boolean | Maintenance mode |
| ThemeColor | #007bff | String | Primary color |
| EnableNotifications | true | Boolean | Push notifications |
| APIRateLimitPerMinute | 100 | Integer | Rate limit |

---

## 🎯 Integration Points

### With UnitOfWork Pattern

```csharp
public class MyService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task ProcessTransaction(long companyId)
    {
        // Get repositories
        var policyRepo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
        var flagRepo = _unitOfWork.GetRepository<IApplicationFlagRepository>();

        // Work with transaction
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var policy = await policyRepo.GetByCompanyIdAsync(companyId);
            var flags = await flagRepo.GetActiveByCompanyAsync(companyId);

            // Process...

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

### Get Repository

```csharp
// Pattern 1: Get through UnitOfWork
var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();

// Pattern 2: Direct injection (also works)
private readonly IPasswordPolicyRepository _policyRepo;
```

---

## ⚡ Quick Tips

1. **Always use `GetOrCreateDefaultAsync()`** if you're not sure a policy exists
2. **Use comma-separated flag names** for batch retrieval
3. **Set `ShowToUser = true`** for flags you want to expose in UI
4. **Use `Category`** to organize flags (Security, Feature, UI, Integration)
5. **Set `IsReadOnly = true`** for critical system flags
6. **Use `EffectiveFrom/EffectiveTo`** for scheduled feature rollouts

---

## 🐛 Troubleshooting

### Flag Returns Null

**Check:**
- Is `IsActive = true`?
- Is `EffectiveFrom` not in the future?
- Is `EffectiveTo` not in the past?
- Is `CompanyID` correct?

### Password Policy Not Found

```csharp
// Use this pattern to ensure policy exists
var policy = await policyRepo.GetOrCreateDefaultAsync(companyId);
```

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet build
```

---

## 📚 Full Documentation

See [DYNAMIC_CONFIGURATION.md](DYNAMIC_CONFIGURATION.md) for complete documentation.

---

**Quick Reference Created**: January 15, 2025
**Build Status**: ✅ All Tests Passing
