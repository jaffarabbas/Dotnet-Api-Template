using ApiTemplate.Controllers;
using ApiTemplate.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository;
using Shared.Dtos;
using Shared.Services;
using System.Security.Claims;

namespace ApiTemplate.Tests
{
    [TestFixture]
    public class AuthControllerRefreshTokenTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IAuthRepository> _authRepoMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<IAuditLoggingService> _auditLoggerMock;
        private Mock<ILogger<AuthController>> _loggerMock;
        private AuthController _controller;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _authRepoMock = new Mock<IAuthRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _auditLoggerMock = new Mock<IAuditLoggingService>();
            _loggerMock = new Mock<ILogger<AuthController>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<IAuthRepository>()).Returns(_authRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.IAuthRepository).Returns(_authRepoMock.Object);

            _controller = new AuthController(
                _unitOfWorkMock.Object,
                _emailServiceMock.Object,
                _auditLoggerMock.Object,
                _loggerMock.Object
            );

            // Setup HttpContext with valid IP address and User-Agent
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0";

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Test]
        public async Task Login_ShouldReturnLoginResponseWithRefreshToken()
        {
            // Arrange
            var loginDto = new ApiTemplate.Dtos.LoginDto
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var expectedResponse = new LoginResponse
            {
                Token = "jwt-access-token",
                RefreshToken = "refresh-token-value",
                LoginDate = DateTime.UtcNow,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                Roles = new List<string> { "User" }
            };

            _authRepoMock.Setup(r => r.LoginAsync(
                    It.IsAny<ApiTemplate.Dtos.LoginDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            _auditLoggerMock.Setup(a => a.LogLoginAttempt(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as LoginResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Token, Is.EqualTo("jwt-access-token"));
            Assert.That(response.RefreshToken, Is.EqualTo("refresh-token-value"));
            Assert.That(response.RefreshTokenExpiresAt, Is.Not.Null);
            Assert.That(response.Roles, Has.Count.EqualTo(1));

            _authRepoMock.Verify(r => r.LoginAsync(
                loginDto,
                "192.168.1.1",
                "Mozilla/5.0"), Times.Once);

            _auditLoggerMock.Verify(a => a.LogLoginAttempt(
                loginDto.Username,
                true,
                "192.168.1.1",
                null), Times.Once);
        }

        [Test]
        public async Task RefreshAccessToken_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid-refresh-token"
            };

            var expectedResponse = new RefreshTokenResponse
            {
                AccessToken = "new-jwt-access-token",
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as RefreshTokenResponse;

            Assert.That(response, Is.Not.Null);
            Assert.That(response.AccessToken, Is.EqualTo("new-jwt-access-token"));
            Assert.That(response.RefreshToken, Is.EqualTo("new-refresh-token"));
            Assert.That(response.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
            Assert.That(response.RefreshTokenExpiresAt, Is.GreaterThan(DateTime.UtcNow));

            _authRepoMock.Verify(r => r.RefreshAccessTokenAsync(
                "valid-refresh-token",
                "192.168.1.1",
                "Mozilla/5.0"), Times.Once);

            _unitOfWorkMock.Verify(u => u.Commit(), Times.Once);
        }

        [Test]
        public async Task RefreshAccessToken_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "invalid-refresh-token"
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshTokenResponse?)null);

            // Act
            var result = await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
            var unauthorizedResult = (UnauthorizedObjectResult)result;
            Assert.That(unauthorizedResult.Value, Is.EqualTo("Invalid or expired refresh token."));

            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Test]
        public async Task RefreshAccessToken_WithExpiredToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "expired-refresh-token"
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshTokenResponse?)null);

            // Act
            var result = await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Test]
        public async Task RefreshAccessToken_WithRevokedToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "revoked-refresh-token"
            };

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshTokenResponse?)null);

            // Act
            var result = await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task RefreshAccessToken_WithModelError_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "" // Empty token should fail validation
            };

            _controller.ModelState.AddModelError("RefreshToken", "Refresh token is required");

            // Act
            var result = await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            _authRepoMock.Verify(r => r.RefreshAccessTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RevokeToken_WithValidToken_ShouldRevokeSuccessfully()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                RefreshToken = "valid-refresh-token"
            };

            _authRepoMock.Setup(r => r.RevokeRefreshTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            // Setup authenticated user
            var claims = new List<Claim>
            {
                new Claim("userId", "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var response = okResult.Value;

            Assert.That(response, Is.Not.Null);

            _authRepoMock.Verify(r => r.RevokeRefreshTokenAsync(
                "valid-refresh-token",
                "192.168.1.1"), Times.Once);

            _unitOfWorkMock.Verify(u => u.Commit(), Times.Once);
        }

        [Test]
        public async Task RevokeToken_WithInvalidToken_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                RefreshToken = "invalid-refresh-token"
            };

            _authRepoMock.Setup(r => r.RevokeRefreshTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(false);

            // Setup authenticated user
            var claims = new List<Claim>
            {
                new Claim("userId", "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult.Value, Is.EqualTo("Invalid refresh token or already revoked."));

            _unitOfWorkMock.Verify(u => u.Commit(), Times.Never);
        }

        [Test]
        public async Task RevokeToken_WithAlreadyRevokedToken_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                RefreshToken = "already-revoked-token"
            };

            _authRepoMock.Setup(r => r.RevokeRefreshTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(false);

            // Setup authenticated user
            var claims = new List<Claim>
            {
                new Claim("userId", "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RevokeToken_WithModelError_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                RefreshToken = "" // Empty token
            };

            _controller.ModelState.AddModelError("RefreshToken", "Refresh token is required");

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            _authRepoMock.Verify(r => r.RevokeRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RefreshAccessToken_ShouldTrackIpAddressAndDeviceInfo()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid-token"
            };

            var expectedResponse = new RefreshTokenResponse
            {
                AccessToken = "new-token",
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(10),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            string capturedIp = null;
            string capturedDevice = null;

            _authRepoMock.Setup(r => r.RefreshAccessTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<string, string, string>((token, ip, device) =>
                {
                    capturedIp = ip;
                    capturedDevice = device;
                })
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.RefreshAccessToken(request);

            // Assert
            Assert.That(capturedIp, Is.EqualTo("192.168.1.1"));
            Assert.That(capturedDevice, Is.EqualTo("Mozilla/5.0"));
        }

        [Test]
        public async Task RevokeToken_ShouldTrackRevocationIpAddress()
        {
            // Arrange
            var request = new RevokeTokenRequest
            {
                RefreshToken = "valid-token"
            };

            string capturedIp = null;

            _authRepoMock.Setup(r => r.RevokeRefreshTokenAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<string, string>((token, ip) =>
                {
                    capturedIp = ip;
                })
                .ReturnsAsync(true);

            // Setup authenticated user
            var claims = new List<Claim>
            {
                new Claim("userId", "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            await _controller.RevokeToken(request);

            // Assert
            Assert.That(capturedIp, Is.EqualTo("192.168.1.1"));
        }
    }
}
