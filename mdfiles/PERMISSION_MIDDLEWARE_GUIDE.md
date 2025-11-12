# Global Permission Middleware - Implementation Guide

## Overview

This guide explains the global permission checking middleware implementation that validates user permissions for every request based on **Resource ID from headers**.

## Architecture

### Key Components

1. **IPermissionService** - Interface for permission checking logic
2. **PermissionService** - Implementation that queries the database for user permissions
3. **PermissionMiddleware** - Middleware that intercepts all requests and validates permissions
4. **SkipPermissionCheckAttribute** - Attribute to bypass permission checking for specific endpoints

## Database Schema

The permission system uses the following tables:

```
TblUser (Userid, Username, Email, etc.)
  ↓
TblUserRole (UserId, RoleId, UserRoleIsActive)
  ↓
TblRole (RoleId, RoleTitle, RoleIsActive)
  ↓
TblRolePermission (RoleId, PermissionId, RolePermissionIsActive)
  ↓
TblPermission (PermissionId, ResourceId, ActionTypeId, PermissionIsActive)
  ↓
TblResource (ResourceId, ResourceName, ResourceIsActive)
  ↓
TblActionType (ActionTypeId, ActionTypeTitle, ActionTypeIsActive)
  - Examples: "Read", "Create", "Update", "Delete"
```

## How It Works

### 1. Request Flow

```
Request → AuthMiddleware (validates JWT)
       → PermissionMiddleware (checks resource permission)
       → Controller Action
```

### 2. Permission Checking Process

1. **Extract Resource ID from Header**: The middleware reads the `X-Resource-Id` header
2. **Get User ID from Claims**: Extracts userId from JWT token claims
3. **Map HTTP Method to Action Type**:
   - GET → Read
   - POST → Create
   - PUT/PATCH → Update
   - DELETE → Delete
4. **Check Permission**: Queries database to verify user has the required permission
5. **Allow or Deny**: Either continues to controller or returns 403 Forbidden

### 3. Permission Query Logic

The `PermissionService` checks if a user has permission by:

1. Finding all active roles for the user
2. Finding all active role permissions for those roles
3. Checking if any permission matches:
   - The requested Resource ID
   - The requested Action Type
   - All entities are active (user role, role permission, permission, resource, action type)

## Files Created/Modified

### New Files

1. **[Repositories/Services/IPermissionService.cs](../Repositories/Services/IPermissionService.cs)**
   - Interface defining permission checking methods

2. **[Repositories/Services/PermissionService.cs](../Repositories/Services/PermissionService.cs)**
   - Implementation of permission checking logic

3. **[ApiTemplate/Middleware/PermissionMiddleware.cs](../ApiTemplate/Middleware/PermissionMiddleware.cs)**
   - Global middleware for permission checking

4. **[ApiTemplate/Middleware/PermissionMiddlewareExtensions.cs](../ApiTemplate/Middleware/PermissionMiddlewareExtensions.cs)**
   - Extension method for registering middleware

### Modified Files

1. **[Repositories/RepositoryDI.cs](../Repositories/RepositoryDI.cs)**
   - Added `PermissionService` registration

2. **[Shared/Pipline/ApplicationPipelineExtensions.cs](../Shared/Pipline/ApplicationPipelineExtensions.cs)**
   - Added `configureBeforeEndpoints` parameter for middleware injection

3. **[ApiTemplate/Program.cs](../ApiTemplate/Program.cs)**
   - Configured permission middleware in the pipeline

## Usage

### 1. Client-Side: Sending Requests

All API requests must include the `X-Resource-Id` header:

```http
GET /api/users/123 HTTP/1.1
Authorization: Bearer <your-jwt-token>
X-Resource-Id: 5
```

### 2. Skipping Permission Check

Use the `[SkipPermissionCheck]` attribute on controllers or actions that should bypass permission checking:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [SkipPermissionCheck]  // Login doesn't require permission check
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Login logic
    }
}
```

### 3. Pre-configured Skip Paths

The following paths automatically skip permission checking:

- `/swagger`
- `/favicon.ico`
- `/health`
- `/api/auth/login`
- `/api/auth/register`
- `/api/auth/refresh-token`
- `/api/auth/forgot-password`
- `/api/auth/reset-password`

## Configuration

### Adding/Removing Skip Paths

Edit the `SkipPermissionPaths` array in [PermissionMiddleware.cs](../ApiTemplate/Middleware/PermissionMiddleware.cs):

```csharp
private static readonly string[] SkipPermissionPaths = new[]
{
    "/swagger",
    "/favicon.ico",
    "/health",
    "/api/auth/login",
    // Add more paths here...
};
```

### Changing HTTP Method Mappings

Edit the `HttpMethodToActionType` dictionary in [PermissionService.cs](../Repositories/Services/PermissionService.cs):

```csharp
private static readonly Dictionary<string, string> HttpMethodToActionType = new()
{
    { "GET", "Read" },
    { "POST", "Create" },
    { "PUT", "Update" },
    { "PATCH", "Update" },
    { "DELETE", "Delete" }
};
```

### Changing Header Name

To use a different header name, modify the constant in [PermissionMiddleware.cs](../ApiTemplate/Middleware/PermissionMiddleware.cs):

```csharp
private const string ResourceIdHeader = "X-Resource-Id";  // Change this
```

## Error Responses

### 401 Unauthorized
```json
{
  "StatusCode": 401,
  "Message": "User not authenticated",
  "Timestamp": "2025-10-26T12:00:00Z"
}
```

**Cause**: User is not authenticated or JWT token is invalid/missing

### 403 Forbidden - Missing Header
```json
{
  "StatusCode": 403,
  "Message": "Missing or invalid 'X-Resource-Id' header. Please provide a valid resource ID.",
  "Timestamp": "2025-10-26T12:00:00Z"
}
```

**Cause**: Request doesn't include the `X-Resource-Id` header or the value is not a valid integer

### 403 Forbidden - No Permission
```json
{
  "StatusCode": 403,
  "Message": "You do not have permission to perform 'POST' action on this resource.",
  "Timestamp": "2025-10-26T12:00:00Z"
}
```

**Cause**: User doesn't have the required permission for the resource and action type

## Database Setup

### 1. Create Resources

```sql
INSERT INTO TblResource (ResourceName, ResourceCreatedAt, ResourceIsActive)
VALUES
    ('Users', GETDATE(), 1),
    ('Products', GETDATE(), 1),
    ('Orders', GETDATE(), 1);
```

### 2. Create Action Types

```sql
INSERT INTO TblActionType (ActionTypeTitle, ActionTypeCreatedAt, ActionTypeIsActive)
VALUES
    ('Read', GETDATE(), 1),
    ('Create', GETDATE(), 1),
    ('Update', GETDATE(), 1),
    ('Delete', GETDATE(), 1);
```

### 3. Create Permissions

```sql
-- Assuming ResourceId=1 (Users), ActionTypeId=1 (Read)
INSERT INTO TblPermission (ResourceId, ActionTypeId, PermissionCreatedAt, PermissionIsActive)
VALUES (1, 1, GETDATE(), 1);
```

### 4. Create Role and Assign Permissions

```sql
-- Create Admin role
INSERT INTO TblRole (RoleTitle, RoleCreatedAt, RoleIsActive)
VALUES ('Admin', GETDATE(), 1);

-- Assign permission to role (assuming RoleId=1, PermissionId=1)
INSERT INTO TblRolePermission (RoleId, PermissionId, RolePermissionCreatedAt, RolePermissionIsActive)
VALUES (1, 1, GETDATE(), 1);
```

### 5. Assign Role to User

```sql
-- Assign Admin role to user (assuming UserId=123, RoleId=1)
INSERT INTO TblUserRole (UserId, RoleId, UserRoleCreatedAt, UserRoleIsActive)
VALUES (123, 1, GETDATE(), 1);
```

## Testing

### Test Scenario 1: Successful Permission Check

**Setup:**
- User ID: 123
- User has "Admin" role
- Admin role has "Read" permission for "Users" resource (ResourceId=1)

**Request:**
```http
GET /api/users HTTP/1.1
Authorization: Bearer <valid-token-for-user-123>
X-Resource-Id: 1
```

**Expected:** 200 OK (request proceeds to controller)

### Test Scenario 2: Permission Denied

**Setup:**
- User ID: 456
- User has "Guest" role
- Guest role does NOT have any permissions for "Users" resource

**Request:**
```http
GET /api/users HTTP/1.1
Authorization: Bearer <valid-token-for-user-456>
X-Resource-Id: 1
```

**Expected:** 403 Forbidden

### Test Scenario 3: Missing Header

**Request:**
```http
GET /api/users HTTP/1.1
Authorization: Bearer <valid-token>
# Missing X-Resource-Id header
```

**Expected:** 403 Forbidden with message about missing header

## Logging

The middleware logs all permission checks with structured logging:

### Permission Granted
```
[Information] Permission granted: UserId=123, ResourceId=1, Method=GET, Path=/api/users
```

### Permission Denied
```
[Warning] Permission denied: UserId=456, ResourceId=1, Method=POST, Path=/api/users
```

### Missing Header
```
[Warning] Permission check failed: Missing or invalid 'X-Resource-Id' header for path: /api/users
```

## Performance Considerations

### Database Queries

Each permission check makes a single database query that:
- Joins: UserRoles → Roles → RolePermissions → Permissions → Resources → ActionTypes
- Uses indexed columns (UserId, RoleId, ResourceId, ActionTypeId)
- Filters on active flags to reduce result set

### Optimization Tips

1. **Add Database Indexes**:
   ```sql
   CREATE INDEX IX_TblUserRole_UserId_Active ON TblUserRole(UserId, UserRoleIsActive);
   CREATE INDEX IX_TblRolePermission_RoleId_Active ON TblRolePermission(RoleId, RolePermissionIsActive);
   CREATE INDEX IX_TblPermission_ResourceId_ActionTypeId ON TblPermission(ResourceId, ActionTypeId);
   ```

2. **Add Caching** (future enhancement):
   - Cache user permissions in memory
   - Invalidate cache when roles/permissions change
   - Use distributed cache (Redis) for scalability

3. **Batch Permission Checks** (future enhancement):
   - Load all user permissions on login
   - Store in JWT claims or session
   - Reduce database queries

## Troubleshooting

### Issue: All requests return 403

**Possible Causes:**
1. Database has no permission data
2. User has no active roles
3. Roles have no active permissions
4. Resource/Action type is marked as inactive

**Solution:**
- Check database for complete permission chain
- Verify all `IsActive` flags are set to `true`
- Check logs for specific error messages

### Issue: Permission check not running

**Possible Causes:**
1. Path matches a skip pattern
2. Endpoint has `[SkipPermissionCheck]` attribute
3. Middleware order is incorrect

**Solution:**
- Verify path doesn't match any skip patterns
- Remove `[SkipPermissionCheck]` if not needed
- Ensure middleware is registered in correct order (after Auth, before controllers)

## Best Practices

1. **Always include X-Resource-Id header** in client requests
2. **Map resources to meaningful entities** (Users, Products, Orders, etc.)
3. **Use descriptive role names** (Admin, Manager, Employee, Guest)
4. **Regularly audit permissions** in production
5. **Test permission scenarios** in your test suite
6. **Log permission denials** for security monitoring
7. **Keep permission data normalized** and consistent

## Migration from Attribute-Based to Middleware-Based

If you're migrating from `[RequirePermission]` attributes:

### Before (Attribute-based)
```csharp
[HttpGet("{id}")]
[RequirePermission(ResourceName = "Users", Action = "Read")]
public async Task<IActionResult> GetUser(int id)
{
    // ...
}
```

### After (Middleware-based)
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    // Permission check happens automatically in middleware
    // Client sends: X-Resource-Id: 1 (where 1 = Users resource)
}
```

### Benefits of Middleware Approach

✅ **Centralized**: All permission logic in one place
✅ **Consistent**: Every request goes through the same validation
✅ **Header-based**: Resource ID comes from request header
✅ **Easier to maintain**: No need to decorate every action
✅ **Better separation**: Controllers focus on business logic
✅ **Dynamic**: Can change permissions without code changes

## Support

For questions or issues:
1. Check the logs for detailed error messages
2. Verify database permission setup
3. Test with a simple scenario (single user, role, permission)
4. Review this documentation

---

**Last Updated**: October 26, 2025
**Version**: 1.0
**Author**: AI Assistant
