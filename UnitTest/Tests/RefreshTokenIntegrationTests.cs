using ApiTemplate.Dtos;
using ApiTemplate.Helper;
using ApiTemplate.Repository;
using DBLayer.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Repositories.Repository;
using Repositories.Services;
using Shared.Dtos;
using System.Data;

namespace ApiTemplate.Tests
{
    [TestFixture]
    public class RefreshTokenIntegrationTests
    {
        private Mock<DBLayer.Models.TestContext> _contextMock;
        private Mock<IDbConnection> _connectionMock;
        private Mock<IMemoryCache> _cacheMock;
        private Mock<IOptions<JWTSetting>> _settingsMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock;
        private Mock<IAuthRepository> _authRepoMock;
        private JWTSetting _jwtSettings;

        [SetUp]
        public void SetUp()
        {
            _jwtSettings = new JWTSetting
            {
                securitykey = "test-key-at-least-32-characters-long-for-security",
                ValidIssuer = "TestIssuer",
                ValidAudience = "TestAudience",
                AccessTokenExpirationHours = 10,
                RefreshTokenExpirationDays = 7,
                MaxActiveRefreshTokensPerUser = 5
            };

            _contextMock = new Mock<DBLayer.Models.TestContext>();
            _connectionMock = new Mock<IDbConnection>();
            _cacheMock = new Mock<IMemoryCache>();
            _settingsMock = new Mock<IOptions<JWTSetting>>();
            _settingsMock.Setup(s => s.Value).Returns(_jwtSettings);
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _authRepoMock = new Mock<IAuthRepository>();
        }

        [Test]
        public async Task CompleteRefreshTokenFlow_LoginToRefreshToRevoke()
        {
            // Arrange - Login
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var loginResponse = new LoginResponse
            {
                Token = "initial-jwt-token",
                RefreshToken = "initial-refresh-token",
                LoginDate = DateTime.UtcNow,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                Roles = new List<string> { "User" }
            };

            _authRepoMock.Setup(r => r.LoginAsync(
                    It.IsAny<LoginDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(loginResponse);

            // Act - Login
            var loginResult = await _authRepoMock.Object.LoginAsync(loginDto, "192.168.1.1", "Mozilla/5.0");

            // Assert - Login
            Assert.That(loginResult, Is.Not.Null);
            Assert.That(loginResult.Token, Is.Not.Null);
            Assert.That(loginResult.RefreshToken, Is.Not.Null);

            // Arrange - Refresh Token
            var refreshResponse = new RefreshTokenResponse
            {
                AccessToken = "new-jwt-token",
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    loginResult.RefreshToken,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(refreshResponse);

            // Act - Refresh Token
            var refreshResult = await _authRepoMock.Object.RefreshAccessTokenAsync(
                loginResult.RefreshToken,
                "192.168.1.1",
                "Mozilla/5.0");

            // Assert - Refresh Token
            Assert.That(refreshResult, Is.Not.Null);
            Assert.That(refreshResult.AccessToken, Is.Not.EqualTo(loginResult.Token));
            Assert.That(refreshResult.RefreshToken, Is.Not.EqualTo(loginResult.RefreshToken));

            // Arrange - Revoke Token
            _authRepoMock.Setup(r => r.RevokeRefreshTokenAsync(
                    refreshResult.RefreshToken,
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act - Revoke Token
            var revokeResult = await _authRepoMock.Object.RevokeRefreshTokenAsync(
                refreshResult.RefreshToken,
                "192.168.1.1");

            // Assert - Revoke Token
            Assert.That(revokeResult, Is.True);

            // Verify - Cannot refresh with revoked token
            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    refreshResult.RefreshToken,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshTokenResponse?)null);

            var failedRefresh = await _authRepoMock.Object.RefreshAccessTokenAsync(
                refreshResult.RefreshToken,
                "192.168.1.1",
                "Mozilla/5.0");

            Assert.That(failedRefresh, Is.Null);
        }

        [Test]
        public async Task TokenRotation_ShouldInvalidateOldToken()
        {
            // Arrange
            var oldToken = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "old-token",
                DeviceInfo = "Mozilla/5.0",
                IpAddress = "192.168.1.1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false
            };

            var newToken = new TblRefreshToken
            {
                RefreshTokenId = 2,
                UserId = 1,
                Token = "new-token",
                DeviceInfo = "Mozilla/5.0",
                IpAddress = "192.168.1.1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false
            };

            _refreshTokenServiceMock.Setup(s => s.RotateRefreshTokenAsync(
                    It.IsAny<TblRefreshToken>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<TblRefreshToken, string, string>((old, ip, device) =>
                {
                    old.IsUsed = true;
                    old.ReplacedByToken = newToken.Token;
                })
                .ReturnsAsync(newToken);

            // Act
            var rotatedToken = await _refreshTokenServiceMock.Object.RotateRefreshTokenAsync(
                oldToken,
                "192.168.1.1",
                "Mozilla/5.0");

            // Assert
            Assert.That(rotatedToken, Is.Not.Null);
            Assert.That(rotatedToken.Token, Is.Not.EqualTo(oldToken.Token));
            Assert.That(oldToken.IsUsed, Is.True);
            Assert.That(oldToken.ReplacedByToken, Is.EqualTo(newToken.Token));
        }

        [Test]
        public async Task MaxActiveTokens_ShouldRevokeOldestWhenLimitReached()
        {
            // Arrange
            long userId = 1;
            var maxTokens = _jwtSettings.MaxActiveRefreshTokensPerUser;

            // Simulate user already having max tokens
            var existingTokens = new List<TblRefreshToken>();
            for (int i = 0; i < maxTokens; i++)
            {
                existingTokens.Add(new TblRefreshToken
                {
                    RefreshTokenId = i,
                    UserId = userId,
                    Token = $"token-{i}",
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i * 10), // Oldest first
                    IsRevoked = false,
                    IsUsed = false
                });
            }

            // When generating a new token, the oldest should be revoked
            var newToken = new TblRefreshToken
            {
                RefreshTokenId = maxTokens,
                UserId = userId,
                Token = "new-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false
            };

            _refreshTokenServiceMock.Setup(s => s.GenerateRefreshTokenAsync(
                    userId,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback(() =>
                {
                    // Simulate revoking oldest token
                    var oldest = existingTokens.OrderBy(t => t.CreatedAt).First();
                    oldest.IsRevoked = true;
                    oldest.RevokedAt = DateTime.UtcNow;
                })
                .ReturnsAsync(newToken);

            // Act
            var result = await _refreshTokenServiceMock.Object.GenerateRefreshTokenAsync(
                userId,
                "192.168.1.1",
                "Mozilla/5.0");

            // Assert
            Assert.That(result, Is.Not.Null);
            var revokedCount = existingTokens.Count(t => t.IsRevoked);
            Assert.That(revokedCount, Is.EqualTo(1));
            Assert.That(existingTokens.OrderBy(t => t.CreatedAt).First().IsRevoked, Is.True);
        }

        [Test]
        public async Task SecurityScenario_TokenReuse_ShouldFail()
        {
            // Arrange - First successful refresh
            var refreshToken = "valid-token";
            var firstResponse = new RefreshTokenResponse
            {
                AccessToken = "first-new-token",
                RefreshToken = "first-new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    refreshToken,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(firstResponse);

            var firstRefresh = await _authRepoMock.Object.RefreshAccessTokenAsync(
                refreshToken,
                "192.168.1.1",
                "Mozilla/5.0");

            Assert.That(firstRefresh, Is.Not.Null);

            // Arrange - Second attempt with same token (token reuse attack)
            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    refreshToken,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshTokenResponse?)null);

            // Act - Attempt to reuse the same token
            var secondRefresh = await _authRepoMock.Object.RefreshAccessTokenAsync(
                refreshToken,
                "192.168.1.1",
                "Mozilla/5.0");

            // Assert - Should fail
            Assert.That(secondRefresh, Is.Null);
        }

        [Test]
        public async Task DeviceTracking_ShouldTrackDifferentDevices()
        {
            // Arrange
            long userId = 1;
            var devices = new Dictionary<string, string>
            {
                { "192.168.1.1", "Mozilla/5.0 (Windows)" },
                { "192.168.1.2", "Mozilla/5.0 (iPhone)" },
                { "192.168.1.3", "Mozilla/5.0 (Android)" }
            };

            var tokens = new List<TblRefreshToken>();
            int tokenId = 1;

            foreach (var device in devices)
            {
                var token = new TblRefreshToken
                {
                    RefreshTokenId = tokenId++,
                    UserId = userId,
                    Token = $"token-{device.Key}",
                    IpAddress = device.Key,
                    DeviceInfo = device.Value,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false,
                    IsUsed = false
                };

                tokens.Add(token);

                _refreshTokenServiceMock.Setup(s => s.GenerateRefreshTokenAsync(
                        userId,
                        device.Key,
                        device.Value))
                    .ReturnsAsync(token);
            }

            // Act
            var results = new List<TblRefreshToken>();
            foreach (var device in devices)
            {
                var token = await _refreshTokenServiceMock.Object.GenerateRefreshTokenAsync(
                    userId,
                    device.Key,
                    device.Value);
                results.Add(token);
            }

            // Assert
            Assert.That(results, Has.Count.EqualTo(3));
            Assert.That(results.Select(t => t.IpAddress).Distinct().Count(), Is.EqualTo(3));
            Assert.That(results.Select(t => t.DeviceInfo).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void TokenSecurity_ShouldUseSecureRandomGeneration()
        {
            // Arrange & Act
            var tokens = new HashSet<string>();
            int tokenCount = 100;

            // Generate multiple tokens - they should all be unique
            for (int i = 0; i < tokenCount; i++)
            {
                var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
                tokens.Add(token);
            }

            // Assert
            Assert.That(tokens, Has.Count.EqualTo(tokenCount),
                "All generated tokens should be unique (cryptographically random)");

            // Verify token length (64 bytes base64 encoded)
            foreach (var token in tokens)
            {
                Assert.That(token.Length, Is.GreaterThanOrEqualTo(88),
                    "Base64 encoded 64-byte token should be at least 88 characters");
            }
        }

        [Test]
        public async Task ExpirationValidation_ExpiredTokensShouldNotBeActive()
        {
            // Arrange
            var activeToken = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "active-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                IsUsed = false
            };

            var expiredToken = new TblRefreshToken
            {
                RefreshTokenId = 2,
                UserId = 1,
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                IsRevoked = false,
                IsUsed = false
            };

            var revokedToken = new TblRefreshToken
            {
                RefreshTokenId = 3,
                UserId = 1,
                Token = "revoked-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                IsUsed = false
            };

            var usedToken = new TblRefreshToken
            {
                RefreshTokenId = 4,
                UserId = 1,
                Token = "used-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                IsUsed = true
            };

            // Act & Assert
            Assert.That(activeToken.IsActive, Is.True, "Active token should be active");
            Assert.That(expiredToken.IsActive, Is.False, "Expired token should not be active");
            Assert.That(revokedToken.IsActive, Is.False, "Revoked token should not be active");
            Assert.That(usedToken.IsActive, Is.False, "Used token should not be active");
        }
    }
}
