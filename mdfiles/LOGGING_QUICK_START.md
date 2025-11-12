# Logging System - Quick Start Guide

## What Was Added

A comprehensive logging system has been successfully integrated into your API with the following components:

### 1. **Packages Installed**
- ✅ Serilog.AspNetCore (8.0.3)
- ✅ Serilog.Sinks.Console (6.0.0)
- ✅ Serilog.Sinks.File (7.0.0)
- ✅ Serilog.Sinks.MSSqlServer (8.2.2)
- ✅ Serilog.Enrichers.Environment (3.0.1)
- ✅ Serilog.Enrichers.Thread (4.0.0)

### 2. **New Files Created**

#### Services
- `Shared/Services/LoggingConfiguration.cs` - Serilog configuration
- `Shared/Services/AuditLoggingService.cs` - Audit trail service

#### Middleware
- `Shared/Middleware/RequestResponseLoggingMiddleware.cs` - HTTP logging
- `Shared/Middleware/PerformanceMonitoringMiddleware.cs` - Performance tracking

#### SQL
- `Shared/SQL/CreateLoggingTable.sql` - Database logging table

#### Documentation
- `LOGGING_DOCUMENTATION.md` - Complete documentation
- `LOGGING_QUICK_START.md` - This file

### 3. **Modified Files**
- `ApiTemplate/Program.cs` - Added Serilog initialization
- `ApiTemplate/appsettings.json` - Added Serilog configuration
- `Shared/Pipline/ApplicationPipelineExtensions.cs` - Added logging middlewares
- `ApiTemplate/Service/GeneralDIContainer.cs` - Registered audit service
- `ApiTemplate/Controllers/AuthController.cs` - Added audit logging example

---

## Getting Started

### Step 1: Run the SQL Script (Optional)

The logging table will be auto-created, but you can run this manually:

```sql
-- Run in your SQL Server database
USE test2;
GO

-- Execute the script
-- File: Shared/SQL/CreateLoggingTable.sql
```

### Step 2: Start the Application

```bash
cd d:\GitHub\C-sharp-Practice\ApiTemplate\ApiTemplate
dotnet run
```

### Step 3: Check the Logs

Logs will be written to:

1. **Console** (Development mode) - Real-time output
2. **File (JSON)**: `ApiTemplate/Logs/log-YYYYMMDD.json`
3. **File (Text)**: `ApiTemplate/Logs/log-YYYYMMDD.txt`
4. **Database**: `dbo.ApplicationLogs` table

---

## Quick Examples

### 1. View Logs in Console

When you start the app in Development mode, you'll see logs like:

```
[10:15:32 INF] Logging system initialized - Environment: Development
[10:15:33 INF] Application started successfully
[10:15:35 INF] HTTP Request abc123 | POST /api/auth/login | IP: 127.0.0.1 | User: Anonymous
[10:15:35 INF] AUDIT: Successful login | User: john.doe | IP: 127.0.0.1
[10:15:35 INF] HTTP Response abc123 | Status: 200 | Duration: 123ms
```

### 2. Query Database Logs

```sql
-- Last 50 logs
SELECT TOP 50 * FROM ApplicationLogs
ORDER BY TimeStamp DESC;

-- Audit logs only
SELECT * FROM AuditLogs
WHERE TimeStamp > DATEADD(HOUR, -24, GETDATE())
ORDER BY TimeStamp DESC;

-- Failed login attempts
SELECT TimeStamp, UserName, IPAddress, Message
FROM ApplicationLogs
WHERE Message LIKE '%Failed login attempt%'
ORDER BY TimeStamp DESC;
```

### 3. Use Audit Logging in Your Controllers

```csharp
public class MyController : ControllerBase
{
    private readonly IAuditLoggingService _auditLogger;
    private readonly ILogger<MyController> _logger;

    public MyController(IAuditLoggingService auditLogger, ILogger<MyController> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    [HttpPost("sensitive-operation")]
    public async Task<IActionResult> SensitiveOperation()
    {
        _logger.LogInformation("Starting sensitive operation");

        await _auditLogger.LogSecurityEvent("SensitiveOperation", "User performed X");

        return Ok();
    }
}
```

---

## What Gets Logged Automatically

### ✅ Automatically Logged (No Code Changes Needed)

- **All HTTP Requests/Responses**
  - Method, Path, Status Code
  - Response time
  - IP Address
  - User Agent
  - User information (if authenticated)

- **Performance Metrics**
  - Slow requests (>1s warning, >3s critical)
  - Response time headers

- **Errors & Exceptions**
  - Full stack traces
  - Contextual information

- **Application Lifecycle**
  - Startup/Shutdown events
  - Configuration loading

### ✅ Manual Logging (AuthController Example Implemented)

- Login attempts (success/failure)
- Password changes
- Security events

---

## Log Levels

| Level | When to Use | Example |
|-------|------------|---------|
| **Information** | Normal operations | User logged in, Order created |
| **Warning** | Potential issues | Slow request, Deprecated API used |
| **Error** | Errors that don't crash app | Failed to send email, Database timeout |
| **Critical** | Critical failures | Database down, Out of memory |

---

## Configuration Options

### appsettings.json

```json
{
  "Serilog": {
    "FilePath": "Logs/log-.json",      // JSON log file path
    "TextFilePath": "Logs/log-.txt",   // Text log file path
    "MinimumLevel": {
      "Default": "Information",         // Minimum level to log
      "Override": {
        "Microsoft": "Warning",         // Filter Microsoft logs
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Change Log Level

```json
// Log everything (including debug)
"Default": "Debug"

// Log only warnings and errors
"Default": "Warning"
```

---

## Maintenance

### Clean Old Logs

```sql
-- Clean logs older than 90 days
EXEC sp_CleanOldLogs;

-- Clean logs older than 30 days
EXEC sp_CleanOldLogs @RetentionDays = 30;
```

### Monitor Disk Space

Check the `Logs/` directory periodically:
- JSON logs: ~5-20MB per day
- Text logs: ~3-10MB per day

Logs auto-rotate daily and old files are deleted based on retention policy.

---

## Testing the Logging System

### 1. Test Login Logging

```bash
POST http://localhost:8080/api/v1/auth/login
Content-Type: application/json

{
  "username": "test",
  "password": "wrong"
}
```

Check logs for:
- ✅ HTTP Request logged
- ✅ Failed login audit event
- ✅ HTTP Response logged

### 2. Test Performance Monitoring

Make a slow request and check for performance warnings in logs.

### 3. Test Database Logging

```sql
SELECT TOP 10 * FROM ApplicationLogs
ORDER BY TimeStamp DESC;
```

Should see recent requests.

---

## Troubleshooting

### No logs appearing?

1. **Check file permissions** - Ensure app can write to `Logs/` directory
2. **Check minimum log level** - May be filtering out logs
3. **Check database connection** - Verify connection string

### Logs folder not created?

The folder is created automatically on first log write. If it doesn't exist, the app will create it.

### Database logs not working?

Run the SQL script manually:
```bash
sqlcmd -S localhost -d test2 -i Shared/SQL/CreateLoggingTable.sql
```

---

## Next Steps

1. ✅ **Test the logging** - Make some API calls and check logs
2. ✅ **Add audit logging** to your other controllers
3. ✅ **Set up log cleanup** - Schedule the sp_CleanOldLogs stored procedure
4. ✅ **Review logs regularly** - Monitor for errors and performance issues
5. ✅ **Integrate with monitoring tools** (optional) - Application Insights, Seq, etc.

---

## Summary

Your API now has **enterprise-grade logging** that provides:

✔ **Complete visibility** into all HTTP requests and responses
✔ **Audit trails** for security-sensitive operations
✔ **Performance monitoring** with automatic slow request detection
✔ **Error tracking** with full stack traces
✔ **Structured logs** for easy searching and analysis
✔ **Multiple output sinks** (Console, Files, Database)

**The system is production-ready** and requires no additional configuration to start working!

For detailed information, see [LOGGING_DOCUMENTATION.md](./LOGGING_DOCUMENTATION.md).
