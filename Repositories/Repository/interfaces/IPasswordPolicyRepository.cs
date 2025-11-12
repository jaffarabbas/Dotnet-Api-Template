using DBLayer.Models;

namespace ApiTemplate.Repository
{
    /// <summary>
    /// Repository for managing password policy configurations per company
    /// </summary>
    public interface IPasswordPolicyRepository
    {
        /// <summary>
        /// Gets the active password policy for a company
        /// </summary>
        Task<TblPasswordPolicy?> GetByCompanyIdAsync(long companyId);

        /// <summary>
        /// Gets all password policies (for admin purposes)
        /// </summary>
        Task<IEnumerable<TblPasswordPolicy>> GetAllAsync();

        /// <summary>
        /// Creates a new password policy for a company
        /// </summary>
        Task<TblPasswordPolicy> CreateAsync(TblPasswordPolicy policy);

        /// <summary>
        /// Updates an existing password policy
        /// </summary>
        Task<bool> UpdateAsync(TblPasswordPolicy policy);

        /// <summary>
        /// Deletes a password policy
        /// </summary>
        Task<bool> DeleteAsync(long policyId);

        /// <summary>
        /// Gets password policy by ID
        /// </summary>
        Task<TblPasswordPolicy?> GetByIdAsync(long policyId);

        /// <summary>
        /// Checks if a company has a password policy
        /// </summary>
        Task<bool> ExistsForCompanyAsync(long companyId);

        /// <summary>
        /// Gets or creates default password policy for a company
        /// </summary>
        Task<TblPasswordPolicy> GetOrCreateDefaultAsync(long companyId);
    }
}
