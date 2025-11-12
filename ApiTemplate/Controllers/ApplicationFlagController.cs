using ApiTemplate.Repository;
using DBLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace ApiTemplate.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ApplicationFlagController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApplicationFlagController> _logger;

        public ApplicationFlagController(IUnitOfWork unitOfWork, ILogger<ApplicationFlagController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Gets a single flag value by company and flag name
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="flagName">Flag name</param>
        [HttpGet("company/{companyId}/flag/{flagName}")]
        public async Task<IActionResult> GetFlag(long companyId, string flagName)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flag = await repo.GetFlagAsync(companyId, flagName);

                if (flag == null)
                    return NotFound(new { message = $"Flag '{flagName}' not found for company {companyId}" });

                return Ok(new FlagValueDto
                {
                    FlagName = flag.FlagName,
                    FlagValue = flag.FlagValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flag {FlagName} for company {CompanyId}", flagName, companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving the flag" });
            }
        }

        /// <summary>
        /// Gets multiple flag values by comma-separated flag names
        /// Returns as key-value dictionary
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <param name="flagNames">Comma-separated flag names (e.g., "Feature1,Feature2,Setting1")</param>
        [HttpGet("company/{companyId}/flags")]
        public async Task<IActionResult> GetFlags(long companyId, [FromQuery] string flagNames)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(flagNames))
                {
                    return BadRequest(new { message = "Flag names parameter is required" });
                }

                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flags = await repo.GetFlagValuesAsync(companyId, flagNames);

                return Ok(flags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flags for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving the flags" });
            }
        }

        /// <summary>
        /// Gets all flags for a company
        /// </summary>
        [HttpGet("company/{companyId}/all")]
        public async Task<IActionResult> GetAllByCompany(long companyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flags = await repo.GetAllByCompanyAsync(companyId);

                var dtos = flags.Select(MapToDto);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all flags for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving flags" });
            }
        }

        /// <summary>
        /// Gets active flags for a company
        /// </summary>
        [HttpGet("company/{companyId}/active")]
        public async Task<IActionResult> GetActiveByCompany(long companyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flags = await repo.GetActiveByCompanyAsync(companyId);

                var dtos = flags.Select(MapToDto);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active flags for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving active flags" });
            }
        }

        /// <summary>
        /// Gets flags by category
        /// </summary>
        [HttpGet("company/{companyId}/category/{category}")]
        public async Task<IActionResult> GetByCategory(long companyId, string category)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flags = await repo.GetByCategoryAsync(companyId, category);

                var dtos = flags.Select(MapToDto);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flags by category {Category} for company {CompanyId}", category, companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving flags by category" });
            }
        }

        /// <summary>
        /// Gets user-visible flags (for UI configuration)
        /// </summary>
        [HttpGet("company/{companyId}/user-visible")]
        public async Task<IActionResult> GetUserVisibleFlags(long companyId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var flags = await repo.GetUserVisibleFlagsAsync(companyId);

                var dtos = flags.Select(MapToDto);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user-visible flags for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while retrieving user-visible flags" });
            }
        }

        /// <summary>
        /// Creates a new application flag
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateApplicationFlagDto dto)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();

                // Check if flag already exists
                if (await repo.ExistsAsync(dto.CompanyID, dto.FlagName))
                {
                    return BadRequest(new { message = $"Flag '{dto.FlagName}' already exists for this company" });
                }

                var flag = MapToEntity(dto);
                var created = await repo.CreateAsync(flag);
                await _unitOfWork.SaveAsync();

                var resultDto = MapToDto(created);
                return CreatedAtAction(nameof(GetFlag), new { companyId = created.CompanyID, flagName = created.FlagName }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application flag");
                return StatusCode(500, new { message = "An error occurred while creating the flag" });
            }
        }

        /// <summary>
        /// Updates an existing flag
        /// </summary>
        [HttpPut("{flagId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(long flagId, [FromBody] UpdateApplicationFlagDto dto)
        {
            try
            {
                if (flagId != dto.FlagID)
                {
                    return BadRequest(new { message = "Flag ID mismatch" });
                }

                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var existing = await repo.GetByIdAsync(flagId);

                if (existing == null)
                    return NotFound(new { message = "Flag not found" });

                // Update fields
                existing.FlagValue = dto.FlagValue;
                existing.Description = dto.Description;
                existing.ShowToUser = dto.ShowToUser;
                existing.IsActive = dto.IsActive;
                existing.DisplayOrder = dto.DisplayOrder;
                existing.EffectiveFrom = dto.EffectiveFrom;
                existing.EffectiveTo = dto.EffectiveTo;

                var result = await repo.UpdateAsync(existing);
                await _unitOfWork.SaveAsync();

                if (!result)
                    return BadRequest(new { message = "Cannot update readonly flag" });

                return Ok(new { message = "Flag updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating flag {FlagId}", flagId);
                return StatusCode(500, new { message = "An error occurred while updating the flag" });
            }
        }

        /// <summary>
        /// Bulk updates multiple flag values
        /// </summary>
        [HttpPut("company/{companyId}/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUpdate(long companyId, [FromBody] Dictionary<string, string> flagValues)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var result = await repo.BulkUpdateAsync(companyId, flagValues);
                await _unitOfWork.SaveAsync();

                return Ok(new { message = $"Updated {flagValues.Count} flags successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating flags for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "An error occurred while bulk updating flags" });
            }
        }

        /// <summary>
        /// Deletes a flag
        /// </summary>
        [HttpDelete("{flagId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long flagId)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<IApplicationFlagRepository>();
                var result = await repo.DeleteAsync(flagId);
                await _unitOfWork.SaveAsync();

                if (!result)
                    return NotFound(new { message = "Flag not found or is readonly" });

                return Ok(new { message = "Flag deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting flag {FlagId}", flagId);
                return StatusCode(500, new { message = "An error occurred while deleting the flag" });
            }
        }

        private ApplicationFlagDto MapToDto(TblApplicationFlag entity)
        {
            return new ApplicationFlagDto
            {
                FlagID = entity.FlagID,
                CompanyID = entity.CompanyID,
                FlagName = entity.FlagName,
                FlagValue = entity.FlagValue,
                DataType = entity.DataType,
                Description = entity.Description,
                PossibleValues = entity.PossibleValues,
                DefaultValue = entity.DefaultValue,
                ShowToUser = entity.ShowToUser,
                Category = entity.Category,
                IsActive = entity.IsActive,
                IsReadOnly = entity.IsReadOnly,
                DisplayOrder = entity.DisplayOrder,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                ModuleNamespace = entity.ModuleNamespace
            };
        }

        private TblApplicationFlag MapToEntity(CreateApplicationFlagDto dto)
        {
            return new TblApplicationFlag
            {
                CompanyID = dto.CompanyID,
                FlagName = dto.FlagName,
                FlagValue = dto.FlagValue,
                DataType = dto.DataType,
                Description = dto.Description,
                PossibleValues = dto.PossibleValues,
                DefaultValue = dto.DefaultValue,
                ShowToUser = dto.ShowToUser,
                Category = dto.Category,
                IsReadOnly = dto.IsReadOnly,
                DisplayOrder = dto.DisplayOrder,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                ModuleNamespace = dto.ModuleNamespace
            };
        }
    }
}
