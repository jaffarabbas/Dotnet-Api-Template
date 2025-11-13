using ApiTemplate.Dtos;
using ApiTemplate.Repository;
using DBLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Repositories.Attributes;
using System.Data;
using System.Security.Cryptography;

namespace Repositories.Services
{
    [AutoRegisterRepository(typeof(IRefreshTokenService))]
    public class RefreshTokenService : BaseRepository<TblRefreshToken>, IRefreshTokenService
    {
        private readonly TestContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JWTSetting _settings;

        public RefreshTokenService(
            TestContext context,
            IDbConnection connection,
            IMemoryCache cache,
            IDbTransaction? transaction,
            IOptions<JWTSetting> settings,
            IUnitOfWork unitOfWork,
            IServiceProvider serviceProvider)
            : base(context, connection, cache, transaction, serviceProvider)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
        }

        public async Task<TblRefreshToken> GenerateRefreshTokenAsync(long userId, string? ipAddress = null, string? deviceInfo = null)
        {
            // Clean up old tokens if user has too many active tokens
            await EnforceMaxActiveTokensAsync(userId);

            var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();
            var refreshTokenId = await tokenRepo.GetMaxID("tblRefreshToken", "RefreshTokenId");

            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

            var refreshToken = new TblRefreshToken
            {
                RefreshTokenId = refreshTokenId,
                UserId = userId,
                Token = token,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false
            };

            await tokenRepo.AddAsync("tblRefreshToken", refreshToken);
            return refreshToken;
        }

        public async Task<TblRefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.TblRefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string token, string? ipAddress = null)
        {
            var refreshToken = await GetRefreshTokenAsync(token);

            if (refreshToken == null || !refreshToken.IsActive)
                return false;

            var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            await tokenRepo.UpdateAsync("tblRefreshToken", refreshToken, "RefreshTokenId");
            return true;
        }

        public async Task<bool> RevokeAllUserTokensAsync(long userId)
        {
            var tokens = await _context.TblRefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            if (!tokens.Any())
                return false;

            var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await tokenRepo.UpdateAsync("tblRefreshToken", token, "RefreshTokenId");
            }

            return true;
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.TblRefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.IsRevoked || rt.IsUsed)
                .Where(rt => rt.CreatedAt < DateTime.UtcNow.AddDays(-30)) // Only delete tokens older than 30 days
                .ToListAsync();

            if (!expiredTokens.Any())
                return 0;

            var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();

            foreach (var token in expiredTokens)
            {
                await tokenRepo.DeleteAsync("tblRefreshToken", token.RefreshTokenId.ToString(), "RefreshTokenId");
            }

            return expiredTokens.Count;
        }

        public async Task<TblRefreshToken> RotateRefreshTokenAsync(TblRefreshToken oldToken, string? ipAddress = null, string? deviceInfo = null)
        {
            // Generate new token
            var newToken = await GenerateRefreshTokenAsync(
                oldToken.UserId,
                ipAddress ?? oldToken.IpAddress,
                deviceInfo ?? oldToken.DeviceInfo
            );

            // Mark old token as used and link to new token
            var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();
            oldToken.IsUsed = true;
            oldToken.ReplacedByToken = newToken.Token;
            await tokenRepo.UpdateAsync("tblRefreshToken", oldToken, "RefreshTokenId");

            return newToken;
        }

        private async Task EnforceMaxActiveTokensAsync(long userId)
        {
            var activeTokens = await _context.TblRefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .OrderBy(rt => rt.CreatedAt)
                .ToListAsync();

            var maxTokens = _settings.MaxActiveRefreshTokensPerUser;

            if (activeTokens.Count >= maxTokens)
            {
                // Revoke the oldest tokens
                var tokensToRevoke = activeTokens.Take(activeTokens.Count - maxTokens + 1).ToList();
                var tokenRepo = _unitOfWork.Repository<TblRefreshToken>();

                foreach (var token in tokensToRevoke)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    await tokenRepo.UpdateAsync("tblRefreshToken", token, "RefreshTokenId");
                }
            }
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
