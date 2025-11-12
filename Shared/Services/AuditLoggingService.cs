using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Shared.Services
{
    /// <summary>
    /// Service for logging audit trails of sensitive operations
    /// </summary>
    public interface IAuditLoggingService
    {
        Task LogLoginAttempt(string username, bool success, string ipAddress, string reason = "");
        Task LogPasswordChange(long userId, bool success, string reason = "");
        Task LogUserCreation(long userId, string username, string createdBy);
        Task LogUserDeletion(long userId, string deletedBy);
        Task LogRoleAssignment(long userId, int roleId, string assignedBy);
        Task LogPermissionChange(string resourceName, string actionType, string changedBy);
        Task LogDataAccess(string tableName, string operation, long? recordId, string accessedBy);
        Task LogSecurityEvent(string eventType, string description, Dictionary<string, object>? additionalData = null);
        Task LogFailedAuthorization(long userId, string resourceName, string actionType);
    }

    public class AuditLoggingService : IAuditLoggingService
    {
        private readonly ILogger<AuditLoggingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLoggingService(ILogger<AuditLoggingService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task LogLoginAttempt(string username, bool success, string ipAddress, string reason = "")
        {
            var logData = new
            {
                EventType = "LoginAttempt",
                Username = username,
                Success = success,
                IPAddress = ipAddress,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
            };

            if (success)
            {
                _logger.LogInformation("AUDIT: Successful login | User: {Username} | IP: {IPAddress} | Data: {Data}",
                    username, ipAddress, JsonSerializer.Serialize(logData));
            }
            else
            {
                _logger.LogWarning("AUDIT: Failed login attempt | User: {Username} | IP: {IPAddress} | Reason: {Reason} | Data: {Data}",
                    username, ipAddress, reason, JsonSerializer.Serialize(logData));
            }

            return Task.CompletedTask;
        }

        public Task LogPasswordChange(long userId, bool success, string reason = "")
        {
            var currentUser = GetCurrentUser();
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "PasswordChange",
                UserId = userId,
                Success = success,
                ChangedBy = currentUser,
                IPAddress = ipAddress,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            if (success)
            {
                _logger.LogInformation("AUDIT: Password changed | UserId: {UserId} | ChangedBy: {ChangedBy} | IP: {IPAddress} | Data: {Data}",
                    userId, currentUser, ipAddress, JsonSerializer.Serialize(logData));
            }
            else
            {
                _logger.LogWarning("AUDIT: Password change failed | UserId: {UserId} | Reason: {Reason} | Data: {Data}",
                    userId, reason, JsonSerializer.Serialize(logData));
            }

            return Task.CompletedTask;
        }

        public Task LogUserCreation(long userId, string username, string createdBy)
        {
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "UserCreation",
                UserId = userId,
                Username = username,
                CreatedBy = createdBy,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("AUDIT: User created | UserId: {UserId} | Username: {Username} | CreatedBy: {CreatedBy} | Data: {Data}",
                userId, username, createdBy, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        public Task LogUserDeletion(long userId, string deletedBy)
        {
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "UserDeletion",
                UserId = userId,
                DeletedBy = deletedBy,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogWarning("AUDIT: User deleted | UserId: {UserId} | DeletedBy: {DeletedBy} | Data: {Data}",
                userId, deletedBy, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        public Task LogRoleAssignment(long userId, int roleId, string assignedBy)
        {
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "RoleAssignment",
                UserId = userId,
                RoleId = roleId,
                AssignedBy = assignedBy,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("AUDIT: Role assigned | UserId: {UserId} | RoleId: {RoleId} | AssignedBy: {AssignedBy} | Data: {Data}",
                userId, roleId, assignedBy, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        public Task LogPermissionChange(string resourceName, string actionType, string changedBy)
        {
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "PermissionChange",
                ResourceName = resourceName,
                ActionType = actionType,
                ChangedBy = changedBy,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogWarning("AUDIT: Permission changed | Resource: {ResourceName} | Action: {ActionType} | ChangedBy: {ChangedBy} | Data: {Data}",
                resourceName, actionType, changedBy, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        public Task LogDataAccess(string tableName, string operation, long? recordId, string accessedBy)
        {
            var logData = new
            {
                EventType = "DataAccess",
                TableName = tableName,
                Operation = operation,
                RecordId = recordId,
                AccessedBy = accessedBy,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("AUDIT: Data access | Table: {TableName} | Operation: {Operation} | RecordId: {RecordId} | AccessedBy: {AccessedBy}",
                tableName, operation, recordId, accessedBy);

            return Task.CompletedTask;
        }

        public Task LogSecurityEvent(string eventType, string description, Dictionary<string, object>? additionalData = null)
        {
            var ipAddress = GetClientIpAddress();
            var currentUser = GetCurrentUser();

            var logData = new
            {
                EventType = eventType,
                Description = description,
                User = currentUser,
                IPAddress = ipAddress,
                AdditionalData = additionalData,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogWarning("AUDIT: Security event | Type: {EventType} | User: {User} | Description: {Description} | Data: {Data}",
                eventType, currentUser, description, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        public Task LogFailedAuthorization(long userId, string resourceName, string actionType)
        {
            var ipAddress = GetClientIpAddress();

            var logData = new
            {
                EventType = "FailedAuthorization",
                UserId = userId,
                ResourceName = resourceName,
                ActionType = actionType,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogWarning("AUDIT: Authorization failed | UserId: {UserId} | Resource: {ResourceName} | Action: {ActionType} | IP: {IPAddress} | Data: {Data}",
                userId, resourceName, actionType, ipAddress, JsonSerializer.Serialize(logData));

            return Task.CompletedTask;
        }

        private string GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? httpContext.User.FindFirst("userId")?.Value
                    ?? "AuthenticatedUser";
            }
            return "Anonymous";
        }

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "Unknown";

            return httpContext.Connection.RemoteIpAddress?.ToString()
                ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? "Unknown";
        }
    }
}
