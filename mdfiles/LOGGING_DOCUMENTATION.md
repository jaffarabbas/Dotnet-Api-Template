# Logging System Documentation

## Overview

This API template includes a comprehensive, scalable logging system built with **Serilog** that provides structured logging, audit trails, performance monitoring, and error tracking across the entire application lifecycle.

## Features

### 1. **Structured Logging with Serilog**
- JSON and text-based log files with automatic rotation
- Console output for development
- SQL Server sink for production logging
- Enriched with contextual information (machine name, environment, thread ID, etc.)

### 2. **HTTP Request/Response Logging**
- Automatic logging of all HTTP requests and responses
- Request body logging (with sensitive data masking)
- Response time tracking
- IP address and user agent capture
- Request ID correlation

### 3. **Audit Logging**
- Dedicated service for security-sensitive operations
- Login attempt tracking (success and failures)
- Password change auditing
- User creation/deletion tracking
- Role and permission changes
- Data access logging

### 4. **Performance Monitoring**
- Automatic detection of slow requests (>1s warning, >3s critical)
- Response time headers for client-side monitoring
- Performance metrics in structured logs

### 5. **Error Tracking**
- Comprehensive exception logging
- Stack trace capture
- Contextual error information

---

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "FilePath": "Logs/log-.json",
    "TextFilePath": "Logs/log-.txt",
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Log Sinks

#### 1. Console Sink (Development)
- **Format**: Human-readable text with timestamps
- **Output Template**: `[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}`

#### 2. File Sink (JSON)
- **Location**: `Logs/log-YYYYMMDD.json`
- **Format**: Compact JSON for structured logging
- **Rotation**: Daily
- **Retention**: 30 days
- **Max Size**: 10MB per file

#### 3. File Sink (Text)
- **Location**: `Logs/log-YYYYMMDD.txt`
- **Format**: Plain text for easy reading
- **Rotation**: Daily
- **Retention**: 7 days

#### 4. SQL Server Sink
- **Table**: `dbo.ApplicationLogs`
- **Auto-creation**: Yes
- **Batch Size**: 50 records
- **Batch Period**: 5 seconds
- **Custom Columns**:
  - `UserName` - Authenticated user
  - `IPAddress` - Client IP
  - `RequestPath` - API endpoint
  - `ActionName` - Controller action
  - `Application` - "ApiTemplate"

---

## Usage Examples

### 1. Basic Logging in Controllers

```csharp
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;

    public MyController(ILogger<MyController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetData()
    {
        _logger.LogInformation("Fetching data for user {UserId}", userId);

        try
        {
            // Your logic
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for user {UserId}", userId);
            throw;
        }
    }
}
```

### 2. Audit Logging

```csharp
public class AuthController : ControllerBase
{
    private readonly IAuditLoggingService _auditLogger;

    public AuthController(IAuditLoggingService auditLogger)
    {
        _auditLogger = auditLogger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (loginSuccess)
        {
            await _auditLogger.LogLoginAttempt(dto.Username, true, ipAddress);
            return Ok(token);
        }
        else
        {
            await _auditLogger.LogLoginAttempt(dto.Username, false, ipAddress, "Invalid credentials");
            return Unauthorized();
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        await _auditLogger.LogPasswordChange(userId, success: true);
        return Ok();
    }
}
```

### 3. Performance Monitoring

Performance monitoring is automatic via middleware. All requests are tracked:

```
PERFORMANCE: Slow request | POST /api/invoice/create | Duration: 1523ms | Status: 200
PERFORMANCE: Very slow request detected | GET /api/reports/annual | Duration: 3125ms | Status: 200
```

### 4. Structured Logging

```csharp
_logger.LogInformation(
    "Order created | OrderId: {OrderId} | CustomerId: {CustomerId} | Amount: {Amount}",
    orderId, customerId, amount
);

// Results in searchable structured data:
// {
//   "OrderId": 12345,
//   "CustomerId": 67890,
//   "Amount": 99.99,
//   "Message": "Order created | OrderId: 12345 | CustomerId: 67890 | Amount: 99.99"
// }
```

---

## Audit Events

### Available Audit Methods

```csharp
public interface IAuditLoggingService
{
    Task LogLoginAttempt(string username, bool success, string ipAddress, string reason = "");
    Task LogPasswordChange(long userId, bool success, string reason = "");
    Task LogUserCreation(long userId, string username, string createdBy);
    Task LogUserDeletion(long userId, string deletedBy);
    Task LogRoleAssignment(long userId, int roleId, string assignedBy);
    Task LogPermissionChange(string resourceName, string actionType, string changedBy);
    Task LogDataAccess(string tableName, string operation, long? recordId, string accessedBy);
    Task LogSecurityEvent(string eventType, string description, Dictionary<string, object>? additionalData);
    Task LogFailedAuthorization(long userId, string resourceName, string actionType);
}
```

### Usage in Your Code

```csharp
// Login tracking
await _auditLogger.LogLoginAttempt("john.doe", true, "192.168.1.100");

// User management
await _auditLogger.LogUserCreation(userId, "new.user", "admin");
await _auditLogger.LogRoleAssignment(userId, roleId, "admin");

// Security events
await _auditLogger.LogSecurityEvent("SuspiciousActivity", "Multiple failed login attempts",
    new Dictionary<string, object> {
        { "AttemptCount", 5 },
        { "TimeWindow", "5 minutes" }
    });

// Data access
await _auditLogger.LogDataAccess("tblSalary", "Read", recordId: 123, "john.doe");
```

---

## Sensitive Data Masking

The logging system automatically masks sensitive data in request bodies:

**Masked Fields:**
- `password`
- `currentPassword`
- `newPassword`
- `resetToken`

**Example:**
```json
// Original:
{ "username": "john", "password": "MySecret123" }

// Logged as:
{ "username": "john", "password": "***MASKED***" }
```

**Masked Headers:**
- `Authorization`
- `Cookie`
- `X-API-Key`

---

## Database Schema

### ApplicationLogs Table

```sql
CREATE TABLE [dbo].[ApplicationLogs] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Message] NVARCHAR(MAX),
    [MessageTemplate] NVARCHAR(MAX),
    [Level] NVARCHAR(128),
    [TimeStamp] DATETIME NOT NULL,
    [Exception] NVARCHAR(MAX),
    [Properties] NVARCHAR(MAX),
    [LogEvent] NVARCHAR(MAX),
    [UserName] NVARCHAR(100),
    [IPAddress] NVARCHAR(50),
    [RequestPath] NVARCHAR(500),
    [ActionName] NVARCHAR(200),
    [Application] NVARCHAR(100)
);
```

### Useful Queries

#### View Audit Logs
```sql
SELECT * FROM dbo.ApplicationLogs
WHERE Message LIKE 'AUDIT:%'
ORDER BY TimeStamp DESC;
```

#### Failed Login Attempts
```sql
SELECT TimeStamp, UserName, IPAddress, Message
FROM dbo.ApplicationLogs
WHERE Message LIKE '%Failed login attempt%'
ORDER BY TimeStamp DESC;
```

#### Slow Requests
```sql
SELECT TimeStamp, RequestPath, Message
FROM dbo.ApplicationLogs
WHERE Message LIKE '%PERFORMANCE: Slow request%'
ORDER BY TimeStamp DESC;
```

#### Errors Only
```sql
SELECT * FROM dbo.ApplicationLogs
WHERE Level = 'Error'
ORDER BY TimeStamp DESC;
```

### AuditLogs View

A dedicated view for audit trails:

```sql
SELECT * FROM dbo.AuditLogs
WHERE TimeStamp > DATEADD(DAY, -7, GETDATE());
```

---

## Log Levels

### When to Use Each Level

- **Trace**: Very detailed diagnostic information (rarely used in production)
- **Debug**: Detailed information for debugging (disabled in production)
- **Information**: General informational messages (normal operations)
- **Warning**: Potentially harmful situations (e.g., slow requests, deprecated API usage)
- **Error**: Error events that might still allow the application to continue
- **Critical**: Critical failures that require immediate attention

### Examples

```csharp
_logger.LogDebug("Entering method {MethodName} with parameters {Params}", methodName, params);
_logger.LogInformation("User {UserId} created successfully", userId);
_logger.LogWarning("Request took {Duration}ms, exceeding threshold", duration);
_logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);
_logger.LogCritical("Database connection pool exhausted!");
```

---

## Maintenance

### Log Cleanup

Use the provided stored procedure to clean old logs:

```sql
-- Clean logs older than 90 days (default)
EXEC sp_CleanOldLogs;

-- Clean logs older than 30 days
EXEC sp_CleanOldLogs @RetentionDays = 30;
```

### Log File Rotation

- **JSON logs**: 30 days retention, daily rotation
- **Text logs**: 7 days retention, daily rotation
- **Max file size**: 10MB (automatically creates new file)

### Disk Space Monitoring

Monitor the `Logs/` directory:
- Typical JSON log: 5-20MB per day
- Typical text log: 3-10MB per day

---

## Performance Considerations

### Request/Response Logging
- Request bodies are **only logged** for POST/PUT/PATCH
- **Excluded paths**: `/swagger`, `/health`, `/favicon.ico`, SignalR
- Sensitive endpoints (login, register) have body logging **disabled**

### Batching
- SQL Server logs are batched (50 records / 5 seconds)
- Reduces database I/O
- Minimal performance impact

### Async Operations
- All audit logging is async
- Non-blocking
- Fire-and-forget pattern

---

## Troubleshooting

### Logs Not Appearing

1. **Check file permissions** on `Logs/` directory
2. **Verify configuration** in `appsettings.json`
3. **Check minimum log level** (may be filtering out logs)
4. **Database connection** for SQL Server sink

### SQL Server Logs Not Working

```csharp
// Verify connection string
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=...;..."
}
```

Run the SQL script to create the table manually:
```bash
sqlcmd -S localhost -d YourDatabase -i Shared/SQL/CreateLoggingTable.sql
```

### High Disk Usage

Adjust retention in `LoggingConfiguration.cs`:

```csharp
loggerConfig.WriteTo.File(
    // ...
    retainedFileCountLimit: 7,  // Reduce from 30 to 7
    // ...
);
```

---

## Best Practices

### ✅ DO

- Use structured logging with named parameters
- Include correlation IDs for tracing
- Log business-critical operations
- Mask sensitive data
- Use appropriate log levels
- Include contextual information

### ❌ DON'T

- Log passwords or sensitive data in plain text
- Log excessive information in tight loops
- Use string interpolation in log messages
- Log PII (Personally Identifiable Information) without masking
- Ignore exceptions without logging

### Example

```csharp
// ✅ Good
_logger.LogInformation("Order {OrderId} processed for customer {CustomerId}", orderId, customerId);

// ❌ Bad
_logger.LogInformation($"Order {orderId} processed for customer {customerId}");
```

---

## Integration with Monitoring Tools

This logging system can be integrated with:

- **Application Insights**: Add Serilog.Sinks.ApplicationInsights
- **Elasticsearch**: Add Serilog.Sinks.Elasticsearch
- **Seq**: Add Serilog.Sinks.Seq
- **Splunk**: Add Serilog.Sinks.Splunk

Example for Seq:

```csharp
loggerConfig.WriteTo.Seq("http://localhost:5341");
```

---

## Summary

Your API now has enterprise-grade logging that provides:

✔ **Full application lifecycle visibility**
✔ **Security audit trails**
✔ **Performance monitoring**
✔ **Error tracking and diagnostics**
✔ **Searchable structured logs**
✔ **Compliance-ready audit logs**

**Next Steps:**
1. Run the SQL script to create logging tables
2. Test the logging by making API calls
3. Review logs in `Logs/` directory and database
4. Set up log retention policies
5. Configure alerts for critical errors
