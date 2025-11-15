using ApiTemplate.Dtos;
using ApiTemplate.Repository;
using DBLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Repositories.Services;
using System.Linq;
using System.Data;

namespace ApiTemplate.Tests
{
    [TestFixture]
    public class RefreshTokenServiceTests
    {
        private Mock<DBLayer.Models.TestContext> _contextMock;
        private Mock<IDbConnection> _connectionMock;
        private Mock<IMemoryCache> _cacheMock;
        private Mock<IOptions<JWTSetting>> _settingsMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IGenericRepositoryWrapper<TblRefreshToken>> _tokenRepoMock;
        private RefreshTokenService _service;
        private JWTSetting _jwtSettings;
        private List<TblRefreshToken> _refreshTokens;

        [SetUp]
        public void SetUp()
        {
            // Setup JWT settings
            _jwtSettings = new JWTSetting
            {
                securitykey = "test-key-at-least-32-characters-long-for-security",
                ValidIssuer = "TestIssuer",
                ValidAudience = "TestAudience",
                AccessTokenExpirationHours = 10,
                RefreshTokenExpirationDays = 7,
                MaxActiveRefreshTokensPerUser = 5
            };

            // Setup mocks
            _contextMock = new Mock<DBLayer.Models.TestContext>();
            _connectionMock = new Mock<IDbConnection>();
            _cacheMock = new Mock<IMemoryCache>();
            _settingsMock = new Mock<IOptions<JWTSetting>>();
            _settingsMock.Setup(s => s.Value).Returns(_jwtSettings);
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _tokenRepoMock = new Mock<IGenericRepositoryWrapper<TblRefreshToken>>();

            // Setup in-memory token list
            _refreshTokens = new List<TblRefreshToken>();

            // Setup DbSet mock for TblRefreshTokens
            var mockSet = CreateDbSetMock(_refreshTokens);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Setup repository mock
            _unitOfWorkMock.Setup(u => u.Repository<TblRefreshToken>()).Returns(_tokenRepoMock.Object);

            // Create service instance
            _service = new RefreshTokenService(
                _contextMock.Object,
                _connectionMock.Object,
                _cacheMock.Object,
                _settingsMock.Object,
                _unitOfWorkMock.Object,
                _serviceProviderMock.Object
            );
        }

        private Mock<DbSet<T>> CreateDbSetMock<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            return mockSet;
        }

        [Test]
        public async Task GenerateRefreshTokenAsync_ShouldCreateValidToken()
        {
            // Arrange
            long userId = 1;
            string ipAddress = "192.168.1.1";
            string deviceInfo = "Mozilla/5.0";
            long expectedTokenId = 1;

            _tokenRepoMock.Setup(r => r.GetMaxID("tblRefreshToken", "RefreshTokenId"))
                .ReturnsAsync(expectedTokenId);

            _tokenRepoMock.Setup(r => r.AddAsync(It.IsAny<TblRefreshToken>()))
                .ReturnsAsync((TblRefreshToken?)null);

            // Act
            var result = await _service.GenerateRefreshTokenAsync(userId, ipAddress, deviceInfo);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RefreshTokenId, Is.EqualTo(expectedTokenId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.IpAddress, Is.EqualTo(ipAddress));
            Assert.That(result.DeviceInfo, Is.EqualTo(deviceInfo));
            Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
            Assert.That(result.IsRevoked, Is.False);
            Assert.That(result.IsUsed, Is.False);
            Assert.That(result.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
            Assert.That(result.ExpiresAt, Is.LessThanOrEqualTo(DateTime.UtcNow.AddDays(7).AddMinutes(1)));

            _tokenRepoMock.Verify(r => r.AddAsync(It.IsAny<TblRefreshToken>()), Times.Once);
        }

        [Test]
        public async Task GetRefreshTokenAsync_WithValidToken_ShouldReturnToken()
        {
            // Arrange
            var expectedToken = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false,
                User = new TblUser { Userid = 1, Username = "testuser" }
            };

            _refreshTokens.Add(expectedToken);
            var mockSet = CreateDbSetMock(_refreshTokens);
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Act
            var result = await _service.GetRefreshTokenAsync("valid-token");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Token, Is.EqualTo("valid-token"));
            Assert.That(result.UserId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var mockSet = CreateDbSetMock(_refreshTokens);
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Act
            var result = await _service.GetRefreshTokenAsync("invalid-token");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
        {
            // Arrange
            var token = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IsUsed = false,
                User = new TblUser { Userid = 1, Username = "testuser" }
            };

            _refreshTokens.Add(token);
            var mockSet = CreateDbSetMock(_refreshTokens);
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            _tokenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<TblRefreshToken>(), It.IsAny<string>()))
                .Returns(Task.FromResult(1));

            string ipAddress = "192.168.1.1";

            // Act
            var result = await _service.RevokeRefreshTokenAsync("valid-token", ipAddress);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(token.IsRevoked, Is.True);
            Assert.That(token.RevokedAt, Is.Not.Null);
            Assert.That(token.RevokedByIp, Is.EqualTo(ipAddress));

            _tokenRepoMock.Verify(r => r.UpdateAsync("tblRefreshToken", token, "RefreshTokenId"), Times.Once);
        }

        [Test]
        public async Task RevokeRefreshTokenAsync_WithAlreadyRevokedToken_ShouldReturnFalse()
        {
            // Arrange
            var token = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "revoked-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = true,
                IsUsed = false,
                RevokedAt = DateTime.UtcNow.AddMinutes(-10),
                User = new TblUser { Userid = 1, Username = "testuser" }
            };

            _refreshTokens.Add(token);
            var mockSet = CreateDbSetMock(_refreshTokens);
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Act
            var result = await _service.RevokeRefreshTokenAsync("revoked-token", "192.168.1.1");

            // Assert
            Assert.That(result, Is.False);
            _tokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<TblRefreshToken>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RevokeRefreshTokenAsync_WithExpiredToken_ShouldReturnFalse()
        {
            // Arrange
            var token = new TblRefreshToken
            {
                RefreshTokenId = 1,
                UserId = 1,
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                IsRevoked = false,
                IsUsed = false,
                User = new TblUser { Userid = 1, Username = "testuser" }
            };

            _refreshTokens.Add(token);
            var mockSet = CreateDbSetMock(_refreshTokens);
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Act
            var result = await _service.RevokeRefreshTokenAsync("expired-token", "192.168.1.1");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task RotateRefreshTokenAsync_ShouldCreateNewTokenAndMarkOldAsUsed()
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

            long newTokenId = 2;
            _tokenRepoMock.Setup(r => r.GetMaxID("tblRefreshToken", "RefreshTokenId"))
                .ReturnsAsync(newTokenId);

            _tokenRepoMock.Setup(r => r.AddAsync(It.IsAny<TblRefreshToken>()))
                .ReturnsAsync((TblRefreshToken?)null);

            _tokenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<TblRefreshToken>(), It.IsAny<string>()))
                .Returns(Task.FromResult(1));

            // Act
            var newToken = await _service.RotateRefreshTokenAsync(oldToken, "192.168.1.2", "Chrome");

            // Assert
            Assert.That(newToken, Is.Not.Null);
            Assert.That(newToken.RefreshTokenId, Is.EqualTo(newTokenId));
            Assert.That(newToken.UserId, Is.EqualTo(oldToken.UserId));
            Assert.That(newToken.Token, Is.Not.EqualTo(oldToken.Token));
            Assert.That(newToken.IpAddress, Is.EqualTo("192.168.1.2"));
            Assert.That(newToken.DeviceInfo, Is.EqualTo("Chrome"));

            Assert.That(oldToken.IsUsed, Is.True);
            Assert.That(oldToken.ReplacedByToken, Is.EqualTo(newToken.Token));

            _tokenRepoMock.Verify(r => r.AddAsync(It.IsAny<TblRefreshToken>()), Times.Once);
            _tokenRepoMock.Verify(r => r.UpdateAsync("tblRefreshToken", oldToken, "RefreshTokenId"), Times.Once);
        }

        [Test]
        public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
        {
            // Arrange
            long userId = 1;
            var activeTokens = new List<TblRefreshToken>
            {
                new TblRefreshToken
                {
                    RefreshTokenId = 1,
                    UserId = userId,
                    Token = "token1",
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false,
                    IsUsed = false
                },
                new TblRefreshToken
                {
                    RefreshTokenId = 2,
                    UserId = userId,
                    Token = "token2",
                    ExpiresAt = DateTime.UtcNow.AddDays(6),
                    IsRevoked = false,
                    IsUsed = false
                },
                new TblRefreshToken
                {
                    RefreshTokenId = 3,
                    UserId = userId,
                    Token = "token3",
                    ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired - should not be included
                    IsRevoked = false,
                    IsUsed = false
                },
                new TblRefreshToken
                {
                    RefreshTokenId = 4,
                    UserId = userId,
                    Token = "token4",
                    ExpiresAt = DateTime.UtcNow.AddDays(5),
                    IsRevoked = true, // Already revoked - should not be included
                    IsUsed = false
                }
            };

            _refreshTokens.AddRange(activeTokens);
            var mockSet = CreateDbSetMock(_refreshTokens);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            _tokenRepoMock.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<TblRefreshToken>(), It.IsAny<string>()))
                .Returns(Task.FromResult(1));

            // Act
            var result = await _service.RevokeAllUserTokensAsync(userId);

            // Assert
            Assert.That(result, Is.True);

            // Verify only active tokens were revoked
            var revokedTokens = activeTokens.Where(t => t.IsRevoked && t.RevokedAt != null).ToList();
            Assert.That(revokedTokens.Count, Is.EqualTo(2)); // Only token1 and token2 should be revoked

            _tokenRepoMock.Verify(r => r.UpdateAsync("tblRefreshToken", It.IsAny<TblRefreshToken>(), "RefreshTokenId"), Times.Exactly(2));
        }

        [Test]
        public async Task RevokeAllUserTokensAsync_WithNoActiveTokens_ShouldReturnFalse()
        {
            // Arrange
            long userId = 1;
            var mockSet = CreateDbSetMock(_refreshTokens);
            _contextMock.Setup(c => c.TblRefreshTokens).Returns(mockSet.Object);

            // Act
            var result = await _service.RevokeAllUserTokensAsync(userId);

            // Assert
            Assert.That(result, Is.False);
            _tokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<TblRefreshToken>(), It.IsAny<string>()), Times.Never);
        }
    }

    internal interface IAsyncQueryProvider : IQueryProvider
    {
        TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken = default);
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider, IQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(System.Linq.Expressions.Expression) })
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}
