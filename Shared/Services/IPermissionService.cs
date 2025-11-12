using System.Threading.Tasks;

namespace Shared.Services
{
    /// <summary>
    /// Service for checking user permissions based on resource and action type.
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Checks if a user has permission to perform an action on a resource.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="resourceId">The resource ID</param>
        /// <param name="actionTypeTitle">The action type (e.g., "Read", "Write", "Delete", "Update")</param>
        /// <returns>True if user has permission, false otherwise</returns>
        Task<bool> HasPermissionAsync(long userId, int resourceId, string actionTypeTitle);

        /// <summary>
        /// Checks if a user has permission to perform an action on a resource by action type ID.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="resourceId">The resource ID</param>
        /// <param name="actionTypeId">The action type ID</param>
        /// <returns>True if user has permission, false otherwise</returns>
        Task<bool> HasPermissionByActionTypeIdAsync(long userId, int resourceId, int actionTypeId);
    }
}
