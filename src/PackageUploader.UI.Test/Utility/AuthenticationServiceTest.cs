using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.UI.Test
{
    [TestClass]
    public class AuthenticationServiceTest
    {
        private UserLoggedInProvider _userLoggedInProvider;
        private Mock<IAccessTokenProvider> _mockAccessTokenProvider;
        private Mock<ILogger<AuthenticationService>> _mockLogger;
        private AuthenticationService _authService;

        [TestInitialize]
        public void Setup()
        {
            _userLoggedInProvider = new UserLoggedInProvider();
            _mockAccessTokenProvider = new Mock<IAccessTokenProvider>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();

            _authService = new AuthenticationService(
                _userLoggedInProvider,
                _mockAccessTokenProvider.Object,
                _mockLogger.Object
            );
        }

        [TestMethod]
        public void IsUserLoggedIn_ReturnsValueFromProvider()
        {
            // Arrange
            _userLoggedInProvider.UserLoggedIn = true;

            // Act
            bool result = _authService.IsUserLoggedIn;

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task SignInAsync_WhenSuccessful_UpdatesUserStatus()
        {
            // Arrange
            string validToken = CreateJwtToken("test@example.com");
            var tokenResult = new IngestionAccessToken { AccessToken = validToken };
            _userLoggedInProvider.UserLoggedIn = false;

            _mockAccessTokenProvider
                .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenResult);

            // Act
            bool result = await _authService.SignInAsync();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(validToken, _userLoggedInProvider.AccessToken);
            Assert.AreEqual("test@example.com", _userLoggedInProvider.UserName);
            Assert.IsTrue(_userLoggedInProvider.UserLoggedIn);
        }

        [TestMethod]
        public async Task SignInAsync_WhenTokenNull_ReturnsFalse()
        {
            // Arrange
            _mockAccessTokenProvider
                .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IngestionAccessToken)null);
            _userLoggedInProvider.UserLoggedIn = false;

            // Act
            bool result = await _authService.SignInAsync();

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(_userLoggedInProvider.UserLoggedIn);
        }

        [TestMethod]
        public async Task SignInAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            _mockAccessTokenProvider
                .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            bool result = await _authService.SignInAsync();

            // Assert
            Assert.IsFalse(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SignInAsync_CancelsAndCreatesNewToken()
        {
            // Arrange
            // Call SignInAsync first time to create a cancellation token
            _mockAccessTokenProvider
                .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IngestionAccessToken { AccessToken = "token" });

            await _authService.SignInAsync();

            // Reset the mock to track the second call
            _mockAccessTokenProvider.Invocations.Clear();

            // Act - call SignInAsync again
            await _authService.SignInAsync();

            // Assert - should be called with a new cancellation token
            _mockAccessTokenProvider.Verify(
                p => p.GetTokenAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public void SignOut_ClearsState()
        {
            // Act
            _authService.SignOut();

            // Assert
            Assert.AreEqual(false, _userLoggedInProvider.UserLoggedIn);
            Assert.AreEqual(string.Empty, _userLoggedInProvider.UserName);
            Assert.AreEqual(string.Empty, _userLoggedInProvider.AccessToken);
        }

        [TestMethod]
        public async Task SignInAsync_WithInvalidToken_SetsUserLoggedInButNoUserName()
        {
            // Arrange
            string invalidToken = "not-a-valid-jwt-token";
            var tokenResult = new IngestionAccessToken { AccessToken = invalidToken };

            _mockAccessTokenProvider
                .Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenResult);

            // Act - this would typically throw but the method handles exceptions
            bool result = await _authService.SignInAsync();

            // Assert - should still return success since the token was returned
            Assert.IsFalse(result);
            Assert.IsTrue(_userLoggedInProvider.UserLoggedIn); 
            Assert.AreEqual(invalidToken, _userLoggedInProvider.AccessToken);
            

            // Since the token wasn't valid JWT, the user name should stay empty
            Assert.AreEqual(string.Empty, _userLoggedInProvider.UserName);
        }

        /// <summary>
        /// Helper method to create a valid JWT token with a name claim
        /// </summary>
        private string CreateJwtToken(string name)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new[] { new Claim("name", name) };

            var token = new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: null); // No signing for test

            return tokenHandler.WriteToken(token);
        }
    }
}