using DBLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Attributes;
using System.Text.Json;

namespace ApiTemplate.Repository
{
    [AutoRegisterRepository(typeof(IApplicationFlagRepository))]
    public class ApplicationFlagRepository : IApplicationFlagRepository
    {
        private readonly TestContext _context;
        private readonly ILogger<ApplicationFlagRepository> _logger;

        public ApplicationFlagRepository(TestContext context, ILogger<ApplicationFlagRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TblApplicationFlag?> GetFlagAsync(long companyId, string flagName)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f =>
                        f.CompanyID == companyId &&
                        f.FlagName == flagName &&
                        f.IsActive &&
                        (f.EffectiveFrom == null || f.EffectiveFrom <= DateTime.UtcNow) &&
                        (f.EffectiveTo == null || f.EffectiveTo >= DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flag {FlagName} for company {CompanyId}", flagName, companyId);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetFlagValuesAsync(long companyId, string commaSeparatedFlagNames)
        {
            try
            {
                var flagNames = commaSeparatedFlagNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var flags = await _context.TblApplicationFlags
                    .AsNoTracking()
                    .Where(f =>
                        f.CompanyID == companyId &&
                        flagNames.Contains(f.FlagName) &&
                        f.IsActive &&
                        (f.EffectiveFrom == null || f.EffectiveFrom <= DateTime.UtcNow) &&
                        (f.EffectiveTo == null || f.EffectiveTo >= DateTime.UtcNow))
                    .ToListAsync();

                return flags.ToDictionary(f => f.FlagName, f => f.FlagValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flags for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<IEnumerable<TblApplicationFlag>> GetAllByCompanyAsync(long companyId)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .Where(f => f.CompanyID == companyId)
                    .OrderBy(f => f.Category)
                    .ThenBy(f => f.DisplayOrder)
                    .ThenBy(f => f.FlagName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all flags for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<IEnumerable<TblApplicationFlag>> GetActiveByCompanyAsync(long companyId)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .Where(f =>
                        f.CompanyID == companyId &&
                        f.IsActive &&
                        (f.EffectiveFrom == null || f.EffectiveFrom <= DateTime.UtcNow) &&
                        (f.EffectiveTo == null || f.EffectiveTo >= DateTime.UtcNow))
                    .OrderBy(f => f.Category)
                    .ThenBy(f => f.DisplayOrder)
                    .ThenBy(f => f.FlagName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active flags for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<IEnumerable<TblApplicationFlag>> GetByCategoryAsync(long companyId, string category)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .Where(f =>
                        f.CompanyID == companyId &&
                        f.Category == category &&
                        f.IsActive)
                    .OrderBy(f => f.DisplayOrder)
                    .ThenBy(f => f.FlagName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flags by category {Category} for company {CompanyId}",
                    category, companyId);
                throw;
            }
        }

        public async Task<IEnumerable<TblApplicationFlag>> GetUserVisibleFlagsAsync(long companyId)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .Where(f =>
                        f.CompanyID == companyId &&
                        f.ShowToUser &&
                        f.IsActive)
                    .OrderBy(f => f.Category)
                    .ThenBy(f => f.DisplayOrder)
                    .ThenBy(f => f.FlagName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user-visible flags for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<TblApplicationFlag> CreateAsync(TblApplicationFlag flag)
        {
            try
            {
                flag.CreatedDate = DateTime.UtcNow;
                flag.IsActive = true;

                await _context.TblApplicationFlags.AddAsync(flag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created application flag {FlagName} for company {CompanyId}",
                    flag.FlagName, flag.CompanyID);

                return flag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application flag {FlagName} for company {CompanyId}",
                    flag.FlagName, flag.CompanyID);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(TblApplicationFlag flag)
        {
            try
            {
                // Prevent updating readonly flags
                var existing = await _context.TblApplicationFlags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FlagID == flag.FlagID);

                if (existing?.IsReadOnly == true)
                {
                    _logger.LogWarning("Attempt to update readonly flag {FlagId}", flag.FlagID);
                    return false;
                }

                flag.ModifiedDate = DateTime.UtcNow;

                _context.TblApplicationFlags.Update(flag);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Updated application flag {FlagId}", flag.FlagID);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application flag {FlagId}", flag.FlagID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long flagId)
        {
            try
            {
                var flag = await _context.TblApplicationFlags.FindAsync(flagId);
                if (flag == null)
                    return false;

                if (flag.IsReadOnly)
                {
                    _logger.LogWarning("Attempt to delete readonly flag {FlagId}", flagId);
                    return false;
                }

                // Soft delete
                flag.IsActive = false;
                flag.ModifiedDate = DateTime.UtcNow;

                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted (soft) application flag {FlagId}", flagId);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application flag {FlagId}", flagId);
                throw;
            }
        }

        public async Task<TblApplicationFlag?> GetByIdAsync(long flagId)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FlagID == flagId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application flag {FlagId}", flagId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(long companyId, string flagName)
        {
            try
            {
                return await _context.TblApplicationFlags
                    .AnyAsync(f => f.CompanyID == companyId && f.FlagName == flagName && f.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking flag existence for {FlagName} in company {CompanyId}",
                    flagName, companyId);
                throw;
            }
        }

        public async Task<T> GetFlagValueAsync<T>(long companyId, string flagName, T defaultValue)
        {
            try
            {
                var flag = await GetFlagAsync(companyId, flagName);
                if (flag == null)
                    return defaultValue;

                return ConvertValue<T>(flag.FlagValue, defaultValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting typed flag value for {FlagName}", flagName);
                return defaultValue;
            }
        }

        public async Task<bool> BulkUpdateAsync(long companyId, Dictionary<string, string> flagValues)
        {
            try
            {
                var flagNames = flagValues.Keys.ToList();
                var flags = await _context.TblApplicationFlags
                    .Where(f => f.CompanyID == companyId && flagNames.Contains(f.FlagName) && !f.IsReadOnly)
                    .ToListAsync();

                foreach (var flag in flags)
                {
                    if (flagValues.ContainsKey(flag.FlagName))
                    {
                        flag.FlagValue = flagValues[flag.FlagName];
                        flag.ModifiedDate = DateTime.UtcNow;
                    }
                }

                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk updated {Count} flags for company {CompanyId}",
                    flags.Count, companyId);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating flags for company {CompanyId}", companyId);
                throw;
            }
        }

        private T ConvertValue<T>(string value, T defaultValue)
        {
            try
            {
                var targetType = typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)value;

                if (targetType == typeof(bool))
                    return (T)(object)bool.Parse(value);

                if (targetType == typeof(int))
                    return (T)(object)int.Parse(value);

                if (targetType == typeof(long))
                    return (T)(object)long.Parse(value);

                if (targetType == typeof(decimal))
                    return (T)(object)decimal.Parse(value);

                if (targetType == typeof(double))
                    return (T)(object)double.Parse(value);

                // Try JSON deserialization for complex types
                return JsonSerializer.Deserialize<T>(value) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
