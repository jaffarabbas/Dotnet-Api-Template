# Security Features Documentation

## Overview

This ASP.NET Core API Template implements enterprise-grade security features following OWASP best practices and industry standards. This document outlines all security measures implemented and how to configure them.

---

## Table of Contents

1. [Security Headers](#security-headers)
2. [SQL Injection Prevention](#sql-injection-prevention)
3. [Authentication & Authorization](#authentication--authorization)
4. [Password Security](#password-security)
5. [CORS Protection](#cors-protection)
6. [Rate Limiting](#rate-limiting)
7. [Error Handling](#error-handling)
8. [Audit Logging](#audit-logging)
9. [Configuration Security](#configuration-security)
10. [Deployment Security Checklist](#deployment-security-checklist)

---

## 1. Security Headers

### Implemented Headers

The application automatically adds the following security headers to all HTTP responses:

| Header | Purpose | Configuration |
|--------|---------|---------------|
| **Strict-Transport-Security (HSTS)** | Forces HTTPS connections | `SecurityHeaders:EnableHSTS` |
| **X-Frame-Options** | Prevents clickjacking attacks | `SecurityHeaders:XFrameOptions` |
| **X-Content-Type-Options** | Prevents MIME-type sniffing | `SecurityHeaders:EnableXContentTypeOptions` |
| **Content-Security-Policy (CSP)** | Prevents XSS and code injection | `SecurityHeaders:EnableCSP` |
| **Referrer-Policy** | Controls referrer information | `SecurityHeaders:ReferrerPolicy` |
| **Permissions-Policy** | Restricts browser features | `SecurityHeaders:EnablePermissionsPolicy` |
| **Cross-Origin-*-Policy** | Controls cross-origin behavior | `SecurityHeaders:EnableCOOP/COEP/CORP` |

### Configuration

Configure security headers in `appsettings.json`:

```json
{
  "SecurityHeaders": {
    "EnableHSTS": true,
    "HSTSMaxAge": 31536000,
    "XFrameOptions": "DENY",
    "EnableCSP": true,
    "CSP_DefaultSrc": "'self'",
    "CSP_ScriptSrc": "'self'",
    "CSP_StyleSrc": "'self' 'unsafe-inline'"
  }
}
```

### Content Security Policy (CSP)

The default CSP is restrictive. Adjust based on your needs:

- **`'self'`**: Allow from same origin only
- **`'unsafe-inline'`**: Allow inline scripts/styles (use sparingly)
- **`data:`**: Allow data: URIs for images
- **`https:`**: Allow any HTTPS source

**Example**: Allow scripts from Google Analytics:
```json
"CSP_ScriptSrc": "'self' https://www.google-analytics.com"
```

---

## 2. SQL Injection Prevention

### Protection Mechanisms

1. **Entity Framework Core**: Automatically uses parameterized queries
2. **Dapper**: All queries use parameterized binding
3. **SQL Identifier Validation**: Dynamic table/column names are validated

### SqlIdentifierValidator

Prevents SQL injection in dynamic queries:

```csharp
// Validates table/column names
SqlIdentifierValidator.ValidateIdentifier(tableName, nameof(tableName));

// Safely quotes identifiers
var safeTable = SqlIdentifierValidator.QuoteIdentifier(tableName);

// Validates ORDER BY clauses
SqlIdentifierValidator.ValidateOrderByClause(orderBy, nameof(orderBy));
```

### Validation Rules

- Only alphanumeric characters, underscores, and periods
- Maximum 128 characters (SQL Server limit)
- No SQL keywords (SELECT, DROP, etc.)
- No suspicious patterns (comments, semicolons, etc.)

### Example Usage

```csharp
// SECURE: Using GenericRepository with validation
var users = await _repository.GetAllAsync("TblUsers"); // ✅ Validated

// INSECURE: Raw SQL without validation
var sql = $"SELECT * FROM {userInput}"; // ❌ Never do this!
```

---

## 3. Authentication & Authorization

### JWT Configuration

**Location**: `appsettings.json` → `JWTSetting`

```json
{
  "JWTSetting": {
    "ValidIssuer": "YourAppName-API",
    "ValidAudience": "YourAppName-Users",
    "securitykey": "CHANGE_THIS_TO_A_STRONG_RANDOM_KEY"
  }
}
```

### Security Requirements

✅ **Implemented**:
- Issuer validation enabled
- Audience validation enabled
- Signature validation required
- Token expiration enforced
- HTTPS required in production
- Minimum 32-character secret key

### Generating a Secure JWT Key

**PowerShell**:
```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

**Linux/Mac**:
```bash
openssl rand -base64 64
```

### Environment Variables (Recommended for Production)

Store secrets in environment variables instead of config files:

```bash
# Linux/Mac
export JWTSetting__securitykey="your-secret-key-here"

# Windows
setx JWTSetting__securitykey "your-secret-key-here"
```

### Permission-Based Authorization

The application uses a hierarchical permission system:

```
User → UserRole → Role → RolePermission → Permission → (Resource + ActionType)
```

**Headers Required**:
- `X-Resource-Id`: Resource being accessed
- `X-Action-Type-Id`: Action type ID
- `X-Action-Type`: Action name (e.g., "Read", "Write")

**Skip Permission Check**:
```csharp
[SkipPermissionCheck]
public IActionResult PublicEndpoint() { ... }
```

---

## 4. Password Security

### Password Policy (Database-Driven)

**⚠️ IMPORTANT**: Password policies are now stored in the **database** (TblPasswordPolicy table) on a per-company basis, **NOT** in appsettings.json.

**Configuration**: Database table `TblPasswordPolicy`

Each company can have unique password requirements:

```sql
SELECT * FROM TblPasswordPolicy WHERE CompanyID = 1;
```

**To configure password policies**:
1. Use the REST API: `POST /api/v1/PasswordPolicy`
2. Or directly insert into database
3. Policies are automatically applied per company

**Default Policy** (auto-created if not exists):
- Minimum Length: 12 characters
- Maximum Length: 128 characters
- Requires: Uppercase, Lowercase, Digit, Special Character
- Minimum Unique Characters: 5
- Prohibits: Common passwords, Sequential characters, Repeating characters
- Max Login Attempts: 5
- Lockout Duration: 30 minutes

### Password Requirements

Default policy requires:
- ✅ Minimum 12 characters
- ✅ At least 1 uppercase letter (A-Z)
- ✅ At least 1 lowercase letter (a-z)
- ✅ At least 1 digit (0-9)
- ✅ At least 1 special character (!@#$%^&*...)
- ✅ At least 5 unique characters
- ❌ Not a common password (e.g., "password123")
- ❌ No sequential characters (e.g., "abc", "123")
- ❌ No repeating characters (e.g., "aaa")

### Password Hashing

Passwords are hashed using **PBKDF2** with:
- 100,000 iterations
- SHA-256 algorithm
- Random 16-byte salt per password

### Validation in Code

```csharp
// RECOMMENDED: Use database-driven validation with company ID
private readonly IPasswordPolicyService _policyService;

public async Task<bool> ValidatePassword(string password, long companyId)
{
    var result = await _policyService.ValidatePasswordAsync(password, companyId);
    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            Console.WriteLine(error); // Display validation errors
        }
        return false;
    }
    return true;
}

// Automatic validation with FluentValidation
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Password)
            .MustBeStrongPassword(); // ✅ Enforces password policy
    }
}

// Fallback: Manual validation (uses static defaults)
var result = PasswordPolicy.Validate(password);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```

### Managing Password Policies

```http
# Get policy for a company
GET /api/v1/PasswordPolicy/company/{companyId}

# Create new policy (Admin only)
POST /api/v1/PasswordPolicy
{
  "companyID": 1,
  "minimumLength": 14,
  "requireUppercase": true,
  "requireLowercase": true,
  "requireDigit": true,
  "requireSpecialCharacter": true,
  "maxLoginAttempts": 3,
  "lockoutDurationMinutes": 60
}

# Update existing policy (Admin only)
PUT /api/v1/PasswordPolicy/{policyId}
```

**See also**: [DYNAMIC_CONFIGURATION.md](DYNAMIC_CONFIGURATION.md) for complete password policy management documentation.

---

## 5. CORS Protection

### Configuration

**Location**: `appsettings.json` → `Cors:AllowedOrigins`

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com"
    ]
  }
}
```

### Security Validations

✅ **Development**:
- Allows `http://localhost` and `http://127.0.0.1`
- Allows any origin if none configured

✅ **Production**:
- **Requires** explicit origin configuration
- **Rejects** HTTP origins (only HTTPS allowed)
- **Rejects** localhost origins
- **Fails startup** if no valid origins configured

### Adding Origins

1. **Exact origin matching**:
   ```json
   "AllowedOrigins": ["https://app.example.com"]
   ```

2. **Multiple subdomains**:
   ```json
   "AllowedOrigins": [
     "https://app.example.com",
     "https://admin.example.com"
   ]
   ```

3. **Development + Production**:
   ```json
   // appsettings.Development.json
   "AllowedOrigins": ["http://localhost:3000"]

   // appsettings.Production.json
   "AllowedOrigins": ["https://app.example.com"]
   ```

---

## 6. Rate Limiting

### Implemented Strategies

| Policy | Limit | Window | Use Case |
|--------|-------|--------|----------|
| **Fixed Window** | 10 requests | 1 minute | Login endpoints |
| **Sliding Window** | 100 requests | 1 minute | General API usage |
| **Token Bucket** | 100 tokens | Replenish 20/10s | Burst traffic |
| **Concurrency** | 10 concurrent | N/A | Resource protection |
| **Per-IP** | 100 requests | 1 minute | IP-based throttling |
| **Per-User** | 50 requests | 1 minute | Authenticated users |

### Applying Rate Limits

```csharp
// On specific endpoint
[EnableRateLimiting("PerIPPolicy")]
public IActionResult Login() { ... }

// On entire controller
[EnableRateLimiting("SlidingPolicy")]
public class UsersController { ... }

// Disable rate limiting
[DisableRateLimiting]
public IActionResult HealthCheck() { ... }
```

### Configuration

```json
{
  "RateLimiting": {
    "PerIPPolicy": {
      "PermitLimit": 100,
      "WindowMinutes": 1
    }
  }
}
```

---

## 7. Error Handling

### Environment-Aware Responses

#### Development
```json
{
  "error": "Object reference not set to an instance of an object",
  "stackTrace": "at MyApp.Controllers.UserController...",
  "innerException": "...",
  "type": "NullReferenceException",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

#### Production
```json
{
  "error": "An unexpected error occurred. Please try again later.",
  "statusCode": 500,
  "timestamp": "2025-01-15T10:30:00Z",
  "traceId": "0HMVFE42N5555"
}
```

### Custom Exception Types

```csharp
throw new NotFoundException("User not found");        // 404
throw new BadRequestException("Invalid input");       // 400
throw new UnAuthorizedAccessException("No access");   // 401
```

### Server-Side Logging

All exceptions are logged with full details:
- Exception type and message
- Stack trace
- User information (if authenticated)
- Request details (IP, path, method)
- Trace ID for correlation

---

## 8. Audit Logging

### What is Logged

- ✅ Login attempts (success/failure)
- ✅ Password changes
- ✅ User creation/deletion
- ✅ Role assignments
- ✅ Permission changes
- ✅ Failed authorization attempts
- ✅ Security events

### Usage

```csharp
// Inject the service
private readonly IAuditLoggingService _auditLog;

// Log security events
await _auditLog.LogLoginAttemptAsync(userId, success: true);
await _auditLog.LogPasswordChangeAsync(userId);
await _auditLog.LogFailedAuthorizationAsync(userId, resource);
```

### Log Storage

Logs are stored in:
- **Serilog files**: `Logs/log-*.json` and `Logs/log-*.txt`
- **Database**: Audit events are persisted for compliance

---

## 9. Configuration Security

### Secret Management

**❌ DO NOT**:
- Store secrets in `appsettings.json` in production
- Commit secrets to source control
- Share secrets via email or chat

**✅ DO**:
- Use environment variables in production
- Use Azure Key Vault / AWS Secrets Manager
- Use .NET User Secrets for local development
- Rotate secrets regularly

### User Secrets (Development)

```bash
# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "JWTSetting:securitykey" "your-dev-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

# List secrets
dotnet user-secrets list
```

### Environment Variables (Production)

**Azure App Service**:
```bash
az webapp config appsettings set --name myapp \
  --resource-group mygroup \
  --settings JWTSetting__securitykey="production-key"
```

**Docker**:
```yaml
environment:
  - JWTSetting__securitykey=production-key
  - ConnectionStrings__DefaultConnection=server=...
```

---

## 10. Deployment Security Checklist

### Pre-Deployment

- [ ] Generate strong JWT secret key (min 32 chars)
- [ ] Store secrets in environment variables or vault
- [ ] Configure production CORS origins (HTTPS only)
- [ ] Review and adjust CSP headers
- [ ] Enable HSTS with appropriate max-age
- [ ] Set production connection strings
- [ ] Remove development CORS origins
- [ ] Review rate limiting policies
- [ ] Test authentication flows
- [ ] Enable HTTPS redirection
- [ ] **Run database migration for password policies and flags**
- [ ] **Configure company-specific password policies in database**
- [ ] **Set up application flags for each company**

### Post-Deployment

- [ ] Verify HTTPS is working
- [ ] Check security headers using [securityheaders.com](https://securityheaders.com)
- [ ] Test CORS from allowed origins
- [ ] Verify rate limiting is active
- [ ] Check error responses don't leak information
- [ ] Monitor audit logs for suspicious activity
- [ ] Set up security alerts
- [ ] Document incident response procedures

### Security Scanning

```bash
# Dependency vulnerability scanning
dotnet list package --vulnerable

# OWASP Dependency Check
dependency-check --project "MyAPI" --scan .

# SonarQube analysis
dotnet sonarscanner begin /k:"myapi"
dotnet build
dotnet sonarscanner end
```

---

## Additional Resources

### Security Standards

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)

### Testing Tools

- [OWASP ZAP](https://www.zaproxy.org/) - Web application security scanner
- [Burp Suite](https://portswigger.net/burp) - Security testing toolkit
- [Mozilla Observatory](https://observatory.mozilla.org/) - Security header testing
- [SecurityHeaders.com](https://securityheaders.com/) - Header analysis

### Further Reading

- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Content Security Policy Guide](https://content-security-policy.com/)

---

## Support & Reporting Security Issues

If you discover a security vulnerability, please email security@yourcompany.com instead of opening a public issue.

**Include**:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

We take security seriously and will respond to reports within 48 hours.

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-15 | Initial security features implementation |

---

**Last Updated**: January 15, 2025
**Maintained By**: Security Team
