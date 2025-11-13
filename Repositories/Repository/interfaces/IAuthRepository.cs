using ApiTemplate.Dtos;
using Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repository
{
    public interface IAuthRepository
    {
        public Task<LoginResponse?> LoginAsync(LoginDto loginDto, string? ipAddress = null, string? deviceInfo = null);
        public Task<object> RegisterAsync(RegisterUserDto userDto);
        public Task<bool> ChangePasswordAsync(long userId, ChangePasswordDto passwordDto);
        public Task<string> GeneratePasswordResetTokenAsync(ForgotPasswordDto forgotPasswordDto);
        public Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        public Task<string> GenerateJwtResetTokenAsync(long userId);
        public Task<TokenResponse?> RefreshTokenWithResetTokenAsync(string resetToken);
        public Task<RefreshTokenResponse?> RefreshAccessTokenAsync(string refreshToken, string? ipAddress = null, string? deviceInfo = null);
        public Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? ipAddress = null);
    }
}
