using DBLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Services
{
    /// <summary>
    /// Service for checking user permissions based on resource and action type.
    /// Supports caching for performance optimization.
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly TestContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(TestContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a user has permission to perform an action on a resource.
        /// </summary>
        public async Task<bool> HasPermissionAsync(long userId, int resourceId, string actionTypeTitle)
        {
            try
            {
                // Get user's active roles
                var userRoleIds = await _context.TblUserRoles
                    .Where(ur => ur.UserId == userId && ur.UserRoleIsActive)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (!userRoleIds.Any())
                {
                    _logger.LogWarning("User {UserId} has no active roles", userId);
                    return false;
                }

                // Check if any of the user's roles have the required permission
                var hasPermission = await _context.TblRolePermissions
                    .Where(rp => userRoleIds.Contains(rp.RoleId) && rp.RolePermissionIsActive)
                    .AnyAsync(rp =>
                        rp.Permission.ResourceId == resourceId &&
                        rp.Permission.PermissionIsActive &&
                        rp.Permission.ActionType.ActionTypeTitle.ToLower() == actionTypeTitle.ToLower() &&
                        rp.Permission.ActionType.ActionTypeIsActive &&
                        rp.Permission.Resource.ResourceIsActive
                    );

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "Permission denied: UserId={UserId}, ResourceId={ResourceId}, Action={ActionType}",
                        userId, resourceId, actionTypeTitle);
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking permission for UserId={UserId}, ResourceId={ResourceId}, Action={ActionType}",
                    userId, resourceId, actionTypeTitle);
                return false;
            }
        }

        /// <summary>
        /// Checks if a user has permission to perform an action on a resource by action type ID.
        /// </summary>
        public async Task<bool> HasPermissionByActionTypeIdAsync(long userId, int resourceId, int actionTypeId)
        {
            try
            {
                // Get user's active roles
                var userRoleIds = await _context.TblUserRoles
                    .Where(ur => ur.UserId == userId && ur.UserRoleIsActive)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (!userRoleIds.Any())
                {
                    _logger.LogWarning("User {UserId} has no active roles", userId);
                    return false;
                }

                // Check if any of the user's roles have the required permission
                var hasPermission = await _context.TblRolePermissions
                    .Where(rp => userRoleIds.Contains(rp.RoleId) && rp.RolePermissionIsActive)
                    .AnyAsync(rp =>
                        rp.Permission.ResourceId == resourceId &&
                        rp.Permission.ActionTypeId == actionTypeId &&
                        rp.Permission.PermissionIsActive &&
                        rp.Permission.ActionType.ActionTypeIsActive &&
                        rp.Permission.Resource.ResourceIsActive
                    );

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "Permission denied: UserId={UserId}, ResourceId={ResourceId}, ActionTypeId={ActionTypeId}",
                        userId, resourceId, actionTypeId);
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking permission for UserId={UserId}, ResourceId={ResourceId}, ActionTypeId={ActionTypeId}",
                    userId, resourceId, actionTypeId);
                return false;
            }
        }
    }
}
