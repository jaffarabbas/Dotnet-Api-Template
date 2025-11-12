using ApiTemplate.Repository;
using ApiTemplate.Security;
using Microsoft.Extensions.Logging;

namespace Repositories.Services
{
    /// <summary>
    /// Service for applying password policies from database
    /// </summary>
    public interface IPasswordPolicyService
    {
        Task<PasswordValidationResult> ValidatePasswordAsync(string password, long companyId);
        Task ApplyPolicySettingsAsync(long companyId);
    }

    public class PasswordPolicyService : IPasswordPolicyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PasswordPolicyService> _logger;

        public PasswordPolicyService(IUnitOfWork unitOfWork, ILogger<PasswordPolicyService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, long companyId)
        {
            try
            {
                // Get policy from database
                var policyRepo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var policy = await policyRepo.GetOrCreateDefaultAsync(companyId);

                // Apply policy settings temporarily for this validation
                var originalSettings = SaveCurrentSettings();

                ApplyPolicyToValidator(policy);

                var result = PasswordPolicy.Validate(password);

                // Restore original settings
                RestoreSettings(originalSettings);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password for company {CompanyId}", companyId);
                // Fall back to default validation
                return PasswordPolicy.Validate(password);
            }
        }

        public async Task ApplyPolicySettingsAsync(long companyId)
        {
            try
            {
                var policyRepo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var policy = await policyRepo.GetOrCreateDefaultAsync(companyId);

                ApplyPolicyToValidator(policy);

                _logger.LogInformation("Applied password policy from database for company {CompanyId}", companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying password policy for company {CompanyId}", companyId);
            }
        }

        private void ApplyPolicyToValidator(DBLayer.Models.TblPasswordPolicy policy)
        {
            PasswordPolicy.Settings.MinimumLength = policy.MinimumLength;
            PasswordPolicy.Settings.MaximumLength = policy.MaximumLength;
            PasswordPolicy.Settings.RequireUppercase = policy.RequireUppercase;
            PasswordPolicy.Settings.RequireLowercase = policy.RequireLowercase;
            PasswordPolicy.Settings.RequireDigit = policy.RequireDigit;
            PasswordPolicy.Settings.RequireSpecialCharacter = policy.RequireSpecialCharacter;
            PasswordPolicy.Settings.MinimumUniqueCharacters = policy.MinimumUniqueCharacters;
            PasswordPolicy.Settings.ProhibitCommonPasswords = policy.ProhibitCommonPasswords;
            PasswordPolicy.Settings.ProhibitSequentialCharacters = policy.ProhibitSequentialCharacters;
            PasswordPolicy.Settings.ProhibitRepeatingCharacters = policy.ProhibitRepeatingCharacters;
        }

        private PasswordPolicySettings SaveCurrentSettings()
        {
            return new PasswordPolicySettings
            {
                MinimumLength = PasswordPolicy.Settings.MinimumLength,
                MaximumLength = PasswordPolicy.Settings.MaximumLength,
                RequireUppercase = PasswordPolicy.Settings.RequireUppercase,
                RequireLowercase = PasswordPolicy.Settings.RequireLowercase,
                RequireDigit = PasswordPolicy.Settings.RequireDigit,
                RequireSpecialCharacter = PasswordPolicy.Settings.RequireSpecialCharacter,
                MinimumUniqueCharacters = PasswordPolicy.Settings.MinimumUniqueCharacters,
                ProhibitCommonPasswords = PasswordPolicy.Settings.ProhibitCommonPasswords,
                ProhibitSequentialCharacters = PasswordPolicy.Settings.ProhibitSequentialCharacters,
                ProhibitRepeatingCharacters = PasswordPolicy.Settings.ProhibitRepeatingCharacters
            };
        }

        private void RestoreSettings(PasswordPolicySettings settings)
        {
            PasswordPolicy.Settings.MinimumLength = settings.MinimumLength;
            PasswordPolicy.Settings.MaximumLength = settings.MaximumLength;
            PasswordPolicy.Settings.RequireUppercase = settings.RequireUppercase;
            PasswordPolicy.Settings.RequireLowercase = settings.RequireLowercase;
            PasswordPolicy.Settings.RequireDigit = settings.RequireDigit;
            PasswordPolicy.Settings.RequireSpecialCharacter = settings.RequireSpecialCharacter;
            PasswordPolicy.Settings.MinimumUniqueCharacters = settings.MinimumUniqueCharacters;
            PasswordPolicy.Settings.ProhibitCommonPasswords = settings.ProhibitCommonPasswords;
            PasswordPolicy.Settings.ProhibitSequentialCharacters = settings.ProhibitSequentialCharacters;
            PasswordPolicy.Settings.ProhibitRepeatingCharacters = settings.ProhibitRepeatingCharacters;
        }

        private class PasswordPolicySettings
        {
            public int MinimumLength { get; set; }
            public int MaximumLength { get; set; }
            public bool RequireUppercase { get; set; }
            public bool RequireLowercase { get; set; }
            public bool RequireDigit { get; set; }
            public bool RequireSpecialCharacter { get; set; }
            public int MinimumUniqueCharacters { get; set; }
            public bool ProhibitCommonPasswords { get; set; }
            public bool ProhibitSequentialCharacters { get; set; }
            public bool ProhibitRepeatingCharacters { get; set; }
        }
    }
}
