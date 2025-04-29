// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Utility
{
    public interface IAuthenticationService
    {
        Task<bool> SignInAsync();
        void SignOut();
        bool IsUserLoggedIn { get; }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<AuthenticationService> _logger;
        private CancellationTokenSource _cancellationTokenSource = new();

        public bool IsUserLoggedIn => _userLoggedInProvider.UserLoggedIn;

        public AuthenticationService(
            UserLoggedInProvider userLoggedInProvider,
            IAccessTokenProvider accessTokenProvider,
            ILogger<AuthenticationService> logger)
        {
            _userLoggedInProvider = userLoggedInProvider;
            _accessTokenProvider = accessTokenProvider;
            _logger = logger;
        }

        public async Task<bool> SignInAsync()
        {
            try
            {
                // Cancel any previous login attempt
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                var loginTask = Task.Run(async () =>
                {
                    return await _accessTokenProvider.GetTokenAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                });

                while (!loginTask.IsCompleted)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    await Task.Delay(500); // Check every 500ms
                }

                var login = await loginTask;

                if (login != null)
                {
                    UpdateSignInStatus(login.AccessToken);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signin");
                return false;
            }
        }

        public void SignOut()
        {
            try
            {
                // Cancel any previous login attempt
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                CachableInteractiveBrowserCredentialAccessToken.ClearCache();

                // Reset user state
                _userLoggedInProvider.UserLoggedIn = false;
                _userLoggedInProvider.UserName = string.Empty;
                _userLoggedInProvider.AccessToken = string.Empty;

                _logger.LogInformation("User signed out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signout");
            }
        }

        private void UpdateSignInStatus(string accessToken)
        {
            _userLoggedInProvider.UserName = string.Empty;
            _userLoggedInProvider.AccessToken = accessToken;
            _userLoggedInProvider.UserLoggedIn = true;

            // Try to extract user name from the token
            var handler = new JwtSecurityTokenHandler();

            if (handler.ReadToken(accessToken) is JwtSecurityToken jsonToken)
            {
                var claims = jsonToken.Claims;

                if (claims != null)
                {
                    _userLoggedInProvider.UserName = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                }
            }
        }
    }
}
