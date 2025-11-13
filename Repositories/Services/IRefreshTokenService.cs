using DBLayer.Models;
using Shared.Dtos;

namespace Repositories.Services
{
    /// <summary>
    /// Service interface for managing refresh tokens.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Generates a new refresh token for a user.
        /// </summary>
        Task<TblRefreshToken> GenerateRefreshTokenAsync(long userId, string? ipAddress = null, string? deviceInfo = null);

        /// <summary>
        /// Validates and retrieves a refresh token.
        /// </summary>
        Task<TblRefreshToken?> GetRefreshTokenAsync(string token);

        /// <summary>
        /// Revokes a refresh token.
        /// </summary>
        Task<bool> RevokeRefreshTokenAsync(string token, string? ipAddress = null);

        /// <summary>
        /// Revokes all refresh tokens for a user.
        /// </summary>
        Task<bool> RevokeAllUserTokensAsync(long userId);

        /// <summary>
        /// Cleans up expired tokens from the database.
        /// </summary>
        Task<int> CleanupExpiredTokensAsync();

        /// <summary>
        /// Rotates a refresh token (revokes old, creates new).
        /// </summary>
        Task<TblRefreshToken> RotateRefreshTokenAsync(TblRefreshToken oldToken, string? ipAddress = null, string? deviceInfo = null);
    }
}
