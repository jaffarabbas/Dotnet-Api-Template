using DBLayer.Models;

namespace ApiTemplate.Repository
{
    /// <summary>
    /// Repository for managing application feature flags and configuration
    /// </summary>
    public interface IApplicationFlagRepository
    {
        /// <summary>
        /// Gets a specific flag value by company and flag name
        /// </summary>
        Task<TblApplicationFlag?> GetFlagAsync(long companyId, string flagName);

        /// <summary>
        /// Gets multiple flags by company and comma-separated flag names
        /// Returns as dictionary for easy key-value access
        /// </summary>
        Task<Dictionary<string, string>> GetFlagValuesAsync(long companyId, string commaSeparatedFlagNames);

        /// <summary>
        /// Gets all flags for a company
        /// </summary>
        Task<IEnumerable<TblApplicationFlag>> GetAllByCompanyAsync(long companyId);

        /// <summary>
        /// Gets all active flags for a company
        /// </summary>
        Task<IEnumerable<TblApplicationFlag>> GetActiveByCompanyAsync(long companyId);

        /// <summary>
        /// Gets flags by category
        /// </summary>
        Task<IEnumerable<TblApplicationFlag>> GetByCategoryAsync(long companyId, string category);

        /// <summary>
        /// Gets flags that should be shown to users
        /// </summary>
        Task<IEnumerable<TblApplicationFlag>> GetUserVisibleFlagsAsync(long companyId);

        /// <summary>
        /// Creates a new flag
        /// </summary>
        Task<TblApplicationFlag> CreateAsync(TblApplicationFlag flag);

        /// <summary>
        /// Updates an existing flag
        /// </summary>
        Task<bool> UpdateAsync(TblApplicationFlag flag);

        /// <summary>
        /// Deletes a flag
        /// </summary>
        Task<bool> DeleteAsync(long flagId);

        /// <summary>
        /// Gets flag by ID
        /// </summary>
        Task<TblApplicationFlag?> GetByIdAsync(long flagId);

        /// <summary>
        /// Checks if a flag exists
        /// </summary>
        Task<bool> ExistsAsync(long companyId, string flagName);

        /// <summary>
        /// Gets a flag value as a specific type (with default fallback)
        /// </summary>
        Task<T> GetFlagValueAsync<T>(long companyId, string flagName, T defaultValue);

        /// <summary>
        /// Bulk updates flag values
        /// </summary>
        Task<bool> BulkUpdateAsync(long companyId, Dictionary<string, string> flagValues);
    }
}
