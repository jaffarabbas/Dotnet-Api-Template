using DBLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Attributes;

namespace ApiTemplate.Repository
{
    [AutoRegisterRepository(typeof(IPasswordPolicyRepository))]
    public class PasswordPolicyRepository : IPasswordPolicyRepository
    {
        private readonly TestContext _context;
        private readonly ILogger<PasswordPolicyRepository> _logger;

        public PasswordPolicyRepository(TestContext context, ILogger<PasswordPolicyRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TblPasswordPolicy?> GetByCompanyIdAsync(long companyId)
        {
            try
            {
                return await _context.TblPasswordPolicies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.CompanyID == companyId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password policy for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<IEnumerable<TblPasswordPolicy>> GetAllAsync()
        {
            try
            {
                return await _context.TblPasswordPolicies
                    .AsNoTracking()
                    .OrderBy(p => p.CompanyID)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all password policies");
                throw;
            }
        }

        public async Task<TblPasswordPolicy> CreateAsync(TblPasswordPolicy policy)
        {
            try
            {
                policy.CreatedDate = DateTime.UtcNow;
                policy.IsActive = true;

                await _context.TblPasswordPolicies.AddAsync(policy);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created password policy {PolicyId} for company {CompanyId}",
                    policy.PasswordPolicyID, policy.CompanyID);

                return policy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating password policy for company {CompanyId}", policy.CompanyID);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(TblPasswordPolicy policy)
        {
            try
            {
                policy.ModifiedDate = DateTime.UtcNow;

                _context.TblPasswordPolicies.Update(policy);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Updated password policy {PolicyId} for company {CompanyId}",
                    policy.PasswordPolicyID, policy.CompanyID);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password policy {PolicyId}", policy.PasswordPolicyID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long policyId)
        {
            try
            {
                var policy = await _context.TblPasswordPolicies.FindAsync(policyId);
                if (policy == null)
                    return false;

                // Soft delete
                policy.IsActive = false;
                policy.ModifiedDate = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted (soft) password policy {PolicyId}", policyId);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting password policy {PolicyId}", policyId);
                throw;
            }
        }

        public async Task<TblPasswordPolicy?> GetByIdAsync(long policyId)
        {
            try
            {
                return await _context.TblPasswordPolicies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PasswordPolicyID == policyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password policy {PolicyId}", policyId);
                throw;
            }
        }

        public async Task<bool> ExistsForCompanyAsync(long companyId)
        {
            try
            {
                return await _context.TblPasswordPolicies
                    .AnyAsync(p => p.CompanyID == companyId && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password policy existence for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<TblPasswordPolicy> GetOrCreateDefaultAsync(long companyId)
        {
            try
            {
                var existing = await GetByCompanyIdAsync(companyId);
                if (existing != null)
                    return existing;

                // Create default policy
                var defaultPolicy = new TblPasswordPolicy
                {
                    CompanyID = companyId,
                    MinimumLength = 12,
                    MaximumLength = 128,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireSpecialCharacter = true,
                    MinimumUniqueCharacters = 5,
                    ProhibitCommonPasswords = true,
                    ProhibitSequentialCharacters = true,
                    ProhibitRepeatingCharacters = true,
                    EnablePasswordExpiry = false,
                    MaxLoginAttempts = 5,
                    LockoutDurationMinutes = 30,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    Description = "Default password policy"
                };

                return await CreateAsync(defaultPolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating default password policy for company {CompanyId}", companyId);
                throw;
            }
        }
    }
}
