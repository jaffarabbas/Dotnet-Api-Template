using ApiTemplate.Repository;
using ApiTemplate.Shared.Helper.Constants;
using Asp.Versioning;
using DBLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace ApiTemplate.Controllers
{
    [ApiController]
    [ApiVersion(ApiVersioningConstants.CurrentVersion)]
    [Route(ApiVersioningConstants.versionRoute)]
    public class PasswordPolicyController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PasswordPolicyController> _logger;

        public PasswordPolicyController(IUnitOfWork unitOfWork, ILogger<PasswordPolicyController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets the password policy for a specific company
        /// </summary>
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(long companyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var policy = await repo.GetByCompanyIdAsync(companyId);

                if (policy == null)
                    return NotFound(new { message = "Password policy not found for this company" });

                var dto = MapToDto(policy);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password policy for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving the password policy" });
            }
        }

        /// <summary>
        /// Gets or creates the password policy for a company
        /// </summary>
        [HttpGet("company/{companyId}/ensure")]
        public async Task<IActionResult> GetOrCreateDefault(long companyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var policy = await repo.GetOrCreateDefaultAsync(companyId);

                var dto = MapToDto(policy);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating password policy for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while processing the password policy" });
            }
        }

        /// <summary>
        /// Gets all password policies (admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var policies = await repo.GetAllAsync();

                var dtos = policies.Select(MapToDto);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all password policies");
                return StatusCode(500, new { message = "An error occurred while retrieving password policies" });
            }
        }

        /// <summary>
        /// Creates a new password policy
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePasswordPolicyDto dto)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();

                // Check if policy already exists for company
                if (await repo.ExistsForCompanyAsync(dto.CompanyID))
                {
                    return BadRequest(new { message = "Password policy already exists for this company" });
                }

                var policy = MapToEntity(dto);
                var created = await repo.CreateAsync(policy);
                await _unitOfWork.SaveAsync();

                var resultDto = MapToDto(created);
                return CreatedAtAction(nameof(GetByCompany), new { companyId = created.CompanyID }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating password policy");
                return StatusCode(500, new { message = "An error occurred while creating the password policy" });
            }
        }

        /// <summary>
        /// Updates an existing password policy
        /// </summary>
        [HttpPut("{policyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(long policyId, [FromBody] UpdatePasswordPolicyDto dto)
        {
            try
            {
                if (policyId != dto.PasswordPolicyID)
                {
                    return BadRequest(new { message = "Policy ID mismatch" });
                }

                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var existing = await repo.GetByIdAsync(policyId);

                if (existing == null)
                    return NotFound(new { message = "Password policy not found" });

                // Update fields
                existing.MinimumLength = dto.MinimumLength;
                existing.MaximumLength = dto.MaximumLength;
                existing.RequireUppercase = dto.RequireUppercase;
                existing.RequireLowercase = dto.RequireLowercase;
                existing.RequireDigit = dto.RequireDigit;
                existing.RequireSpecialCharacter = dto.RequireSpecialCharacter;
                existing.MinimumUniqueCharacters = dto.MinimumUniqueCharacters;
                existing.ProhibitCommonPasswords = dto.ProhibitCommonPasswords;
                existing.ProhibitSequentialCharacters = dto.ProhibitSequentialCharacters;
                existing.ProhibitRepeatingCharacters = dto.ProhibitRepeatingCharacters;
                existing.PasswordExpirationDays = dto.PasswordExpirationDays;
                existing.PasswordHistoryCount = dto.PasswordHistoryCount;
                existing.EnablePasswordExpiry = dto.EnablePasswordExpiry;
                existing.MaxLoginAttempts = dto.MaxLoginAttempts;
                existing.LockoutDurationMinutes = dto.LockoutDurationMinutes;
                existing.Description = dto.Description;

                await repo.UpdateAsync(existing);
                await _unitOfWork.SaveAsync();

                return Ok(new { message = "Password policy updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password policy {PolicyId}", policyId);
                return StatusCode(500, new { message = "An error occurred while updating the password policy" });
            }
        }

        /// <summary>
        /// Deletes a password policy
        /// </summary>
        [HttpDelete("{policyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long policyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IPasswordPolicyRepository>();
                var result = await repo.DeleteAsync(policyId);
                await _unitOfWork.SaveAsync();

                if (!result)
                    return NotFound(new { message = "Password policy not found" });

                return Ok(new { message = "Password policy deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting password policy {PolicyId}", policyId);
                return StatusCode(500, new { message = "An error occurred while deleting the password policy" });
            }
        }

        private PasswordPolicyDto MapToDto(TblPasswordPolicy entity)
        {
            return new PasswordPolicyDto
            {
                PasswordPolicyID = entity.PasswordPolicyID,
                CompanyID = entity.CompanyID,
                MinimumLength = entity.MinimumLength,
                MaximumLength = entity.MaximumLength,
                RequireUppercase = entity.RequireUppercase,
                RequireLowercase = entity.RequireLowercase,
                RequireDigit = entity.RequireDigit,
                RequireSpecialCharacter = entity.RequireSpecialCharacter,
                MinimumUniqueCharacters = entity.MinimumUniqueCharacters,
                ProhibitCommonPasswords = entity.ProhibitCommonPasswords,
                ProhibitSequentialCharacters = entity.ProhibitSequentialCharacters,
                ProhibitRepeatingCharacters = entity.ProhibitRepeatingCharacters,
                PasswordExpirationDays = entity.PasswordExpirationDays,
                PasswordHistoryCount = entity.PasswordHistoryCount,
                EnablePasswordExpiry = entity.EnablePasswordExpiry,
                MaxLoginAttempts = entity.MaxLoginAttempts,
                LockoutDurationMinutes = entity.LockoutDurationMinutes,
                IsActive = entity.IsActive,
                Description = entity.Description
            };
        }

        private TblPasswordPolicy MapToEntity(CreatePasswordPolicyDto dto)
        {
            return new TblPasswordPolicy
            {
                CompanyID = dto.CompanyID,
                MinimumLength = dto.MinimumLength,
                MaximumLength = dto.MaximumLength,
                RequireUppercase = dto.RequireUppercase,
                RequireLowercase = dto.RequireLowercase,
                RequireDigit = dto.RequireDigit,
                RequireSpecialCharacter = dto.RequireSpecialCharacter,
                MinimumUniqueCharacters = dto.MinimumUniqueCharacters,
                ProhibitCommonPasswords = dto.ProhibitCommonPasswords,
                ProhibitSequentialCharacters = dto.ProhibitSequentialCharacters,
                ProhibitRepeatingCharacters = dto.ProhibitRepeatingCharacters,
                PasswordExpirationDays = dto.PasswordExpirationDays,
                PasswordHistoryCount = dto.PasswordHistoryCount,
                EnablePasswordExpiry = dto.EnablePasswordExpiry,
                MaxLoginAttempts = dto.MaxLoginAttempts,
                LockoutDurationMinutes = dto.LockoutDurationMinutes,
                Description = dto.Description
            };
        }
    }
}
