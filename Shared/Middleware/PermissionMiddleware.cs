using ApiTemplate.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Shared.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ApiTemplate.Middleware
{
    /// <summary>
    /// Middleware for global permission checking based on resource ID from headers.
    /// Validates user permissions for every request.
    /// </summary>
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        // Header names
        private const string ResourceIdHeader = "X-Resource-Id";
        private const string ActionTypeIdHeader = "X-Action-Type-Id";
        private const string ActionTypeHeader = "X-Action-Type";

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
        {
            // Skip for swagger and static files FIRST (before checking endpoint metadata)
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }
            // Check if endpoint has SkipJwtValidation or SkipPermissionCheck attribute
            // Use IEndpointFeature to obtain the Endpoint in a way that works without the GetEndpoint() extension/generic overload.
            var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
            if (endpoint == null)
            {
                // Endpoint could be null if routing hasn't populated metadata yet.
                // We've already done path-based public endpoint checks above. Log and continue to header-based permission checks.
                _logger.LogDebug("Endpoint metadata is null for path: {Path}. Falling back to path-based checks.", path);
            }
            else
            {
                // Check for skip attributes using GetMetadata for better reliability (works for both method and controller level)
                var skipJwtAttr = endpoint.Metadata.GetMetadata<SkipJwtValidationAttribute>();
                var skipPermissionAttr = endpoint.Metadata.GetMetadata<SkipPermissionCheckAttribute>();

                if (skipJwtAttr != null || skipPermissionAttr != null)
                {
                    _logger.LogDebug(
                        "Skipping permission check for path: {Path} (SkipJwt: {SkipJwt}, SkipPermission: {SkipPermission})",
                        path, skipJwtAttr != null, skipPermissionAttr != null);
                    await _next(context);
                    return;
                }
            }

            // Get resource ID from header
            if (!context.Request.Headers.TryGetValue(ResourceIdHeader, out var resourceIdValue) ||
                !int.TryParse(resourceIdValue.FirstOrDefault(), out var resourceId))
            {
                _logger.LogWarning(
                    "Permission check failed: Missing or invalid '{Header}' header for path: {Path}",
                    ResourceIdHeader, context.Request.Path);

                await RespondWithForbidden(context,
                    $"Missing or invalid '{ResourceIdHeader}' header. Please provide a valid resource ID.");
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Permission check failed: User not authenticated or invalid user ID");
                await RespondWithUnauthorized(context, "User not authenticated");
                return;
            }

            // Check permission based on header priority: ActionTypeId > ActionType > fallback to action name
            bool hasPermission = false;
            string actionInfo = string.Empty;

            // Priority 1: Check for X-Action-Type-Id header
            if (context.Request.Headers.TryGetValue(ActionTypeIdHeader, out var actionTypeIdValue) &&
                int.TryParse(actionTypeIdValue.FirstOrDefault(), out var actionTypeId))
            {
                hasPermission = await permissionService.HasPermissionByActionTypeIdAsync(userId, resourceId, actionTypeId);
                actionInfo = $"ActionTypeId={actionTypeId}";
            }
            // Priority 2: Check for X-Action-Type header (action name)
            else if (context.Request.Headers.TryGetValue(ActionTypeHeader, out var actionTypeValue) &&
                     !string.IsNullOrEmpty(actionTypeValue.FirstOrDefault()))
            {
                var actionTypeName = actionTypeValue.FirstOrDefault()!;
                hasPermission = await permissionService.HasPermissionAsync(userId, resourceId, actionTypeName);
                actionInfo = $"ActionType={actionTypeName}";
            }
            // Priority 3: No action type header provided - reject the request
            else
            {
                _logger.LogWarning(
                    "Permission check failed: Missing '{ActionTypeIdHeader}' or '{ActionTypeHeader}' header for path: {Path}",
                    ActionTypeIdHeader, ActionTypeHeader, context.Request.Path);

                await RespondWithForbidden(context,
                    $"Missing '{ActionTypeIdHeader}' or '{ActionTypeHeader}' header. Please provide the action type for permission checking.");
                return;
            }

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "Permission denied: UserId={UserId}, ResourceId={ResourceId}, {ActionInfo}, Path={Path}",
                    userId, resourceId, actionInfo, context.Request.Path);

                await RespondWithForbidden(context,
                    $"You do not have permission to perform this action on this resource.");
                return;
            }

            // User has permission, continue to next middleware
            _logger.LogInformation(
                "Permission granted: UserId={UserId}, ResourceId={ResourceId}, {ActionInfo}, Path={Path}",
                userId, resourceId, actionInfo, context.Request.Path);

            await _next(context);
        }

        private async Task RespondWithUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = 401,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private async Task RespondWithForbidden(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = 403,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}
