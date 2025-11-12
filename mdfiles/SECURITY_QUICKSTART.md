# Security Quick Start Guide

## 🚀 Immediate Actions Required

### 1. Change JWT Secret Key (CRITICAL)

**Current**: Placeholder key that will cause startup failure

**Action**:
```bash
# Generate a secure key (PowerShell)
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)

# OR (Linux/Mac)
openssl rand -base64 64
```

**Update** in `appsettings.json`:
```json
{
  "JWTSetting": {
    "securitykey": "YOUR_GENERATED_KEY_HERE"
  }
}
```

**For Production**: Use environment variables!
```bash
export JWTSetting__securitykey="your-production-key"
```

---

### 2. Configure CORS Origins

**Development** (`appsettings.Development.json`):
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  }
}
```

**Production** (`appsettings.Production.json`):
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

⚠️ **Production will NOT start without HTTPS origins configured!**

---

### 3. Update JWT Issuer and Audience

Replace placeholders in `appsettings.json`:
```json
{
  "JWTSetting": {
    "ValidIssuer": "YourCompanyName-API",
    "ValidAudience": "YourAppName-Users"
  }
}
```

---

## ✅ What's Already Secured

### Automatic Protection

- ✅ **Security Headers**: X-Frame-Options, CSP, HSTS, etc.
- ✅ **SQL Injection**: All queries validated & parameterized
- ✅ **Stack Trace Hiding**: Only shown in Development
- ✅ **Password Policy**: 12+ chars, complexity required
- ✅ **Rate Limiting**: Prevents brute force attacks
- ✅ **CORS**: Validated and restricted
- ✅ **Audit Logging**: All security events logged

### No Configuration Needed

These work out of the box:
- JWT authentication with validation
- Permission-based authorization
- Request/response logging (sensitive data masked)
- Performance monitoring
- Error handling with safe messages

---

## 🔧 Common Configuration Tasks

### Adjust Password Requirements

**⚠️ IMPORTANT**: Password policies are now in the **database**, not appsettings.json!

**Update via API** (Admin only):
```http
PUT /api/v1/PasswordPolicy/{policyId}
Content-Type: application/json

{
  "passwordPolicyID": 1,
  "minimumLength": 14,          // Change minimum length
  "requireUppercase": true,     // Require A-Z
  "requireDigit": true,         // Require 0-9
  "requireSpecialCharacter": true  // Require !@#$
}
```

**Or update database directly**:
```sql
UPDATE TblPasswordPolicy
SET MinimumLength = 14,
    MaxLoginAttempts = 3,
    LockoutDurationMinutes = 60
WHERE CompanyID = 1;
```

**See**: [DYNAMIC_CONFIGURATION.md](DYNAMIC_CONFIGURATION.md) for full details.

### Adjust Security Headers

**Allow scripts from CDN**:
```json
{
  "SecurityHeaders": {
    "CSP_ScriptSrc": "'self' https://cdn.jsdelivr.net"
  }
}
```

**Allow iframe embedding** (use carefully!):
```json
{
  "SecurityHeaders": {
    "XFrameOptions": "SAMEORIGIN"  // or "ALLOW-FROM https://trusted.com"
  }
}
```

### Adjust Rate Limits

```json
{
  "RateLimiting": {
    "PerIPPolicy": {
      "PermitLimit": 200,        // Increase from 100
      "WindowMinutes": 1
    }
  }
}
```

---

## 🛡️ Pre-Production Checklist

Run through this before deploying:

```bash
# 1. Check for vulnerabilities
dotnet list package --vulnerable

# 2. Test security headers
# Deploy to staging and visit: https://securityheaders.com

# 3. Verify secrets are NOT in appsettings.json
grep -r "securitykey" appsettings.json
# Should show: "CHANGE_THIS_TO..." or use env vars

# 4. Test CORS
# Try accessing API from unauthorized origin - should fail

# 5. Test rate limiting
# Make 101 requests quickly - should get 429

# 6. Test authentication
# Access protected endpoint without token - should get 401

# 7. Verify HTTPS redirection
curl http://yourapi.com/api/health
# Should redirect to https://
```

---

## 🚨 Common Issues & Solutions

### Issue: "JWT Configuration Errors: security key is too short"

**Solution**: Generate and set a proper 32+ character key

```bash
openssl rand -base64 64 > jwt_key.txt
```

---

### Issue: "SECURITY ERROR: CORS origins must be configured in production"

**Solution**: Add allowed origins to `appsettings.Production.json`:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://yourapp.com"]
  }
}
```

---

### Issue: App won't start - "Table name contains invalid characters"

**Solution**: This is a security feature! Only use valid table names:
- ✅ `TblUsers`, `tbl_users`, `dbo.Users`
- ❌ `Users; DROP TABLE--`, `Users'--`

---

### Issue: Password validation too strict for testing

**For Development Only**:
```json
{
  "PasswordPolicy": {
    "MinimumLength": 6,
    "RequireSpecialCharacter": false,
    "ProhibitCommonPasswords": false
  }
}
```

⚠️ **NEVER use relaxed policies in production!**

---

## 📚 Next Steps

1. **Read Full Documentation**: See `SECURITY.md` for complete details
2. **Test Endpoints**: Use Postman/Swagger to test security
3. **Review Logs**: Check `Logs/` folder for audit trails
4. **Set Up Monitoring**: Configure alerts for security events
5. **Plan Security Reviews**: Schedule regular security audits

---

## 🆘 Need Help?

- **Full Documentation**: See `SECURITY.md`
- **Security Issues**: Email security@yourcompany.com
- **General Questions**: Open an issue on GitHub

---

**Quick Links**:
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Security Headers Test](https://securityheaders.com/)
- [Content Security Policy](https://content-security-policy.com/)

---

**Last Updated**: January 15, 2025
