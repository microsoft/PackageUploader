// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Utility
{
    public interface IAuthenticationService
    {
        Task<bool> SignInAsync();
        void SignOut();
        bool IsUserLoggedIn { get; }
        Task<AzureTenantList> GetAvailableTenants();
        AzureTenant? Tenant { get; set; }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<AuthenticationService> _logger;
        private CancellationTokenSource _cancellationTokenSource = new();

        public bool IsUserLoggedIn => _userLoggedInProvider.UserLoggedIn;

        public AzureTenant? Tenant { get; set; } = null;

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

                // If using advanced sign-in options (selecting a tenant), set that here.
                if (_accessTokenProvider is CachableInteractiveBrowserCredentialAccessToken cachableTokenProvider)
                {
                    if (Tenant == null)
                    {
                        cachableTokenProvider.SetTenantId(null);
                    }
                    else
                    {
                        _logger.LogInformation("Setting tenant ID for authentication to {TenantId}", Tenant.DisplayName);
                        cachableTokenProvider.SetTenantId(Tenant.TenantId);
                    }
                }

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

        public async Task<AzureTenantList> GetAvailableTenants()
        {
            try
            {
                // Cancel any previous token request
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                if (_accessTokenProvider is CachableInteractiveBrowserCredentialAccessToken cachableTokenProvider)
                {
                    _logger.LogInformation("Getting tenant list for authentication");
                    return await cachableTokenProvider.GetTenantsAsync(_cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available tenants");
            }
            return new AzureTenantList();
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

                // Clear selected tenant
                Tenant = null;

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
            _userLoggedInProvider.TenantName = string.Empty;
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

                    var tenantId = claims.FirstOrDefault(c => c.Type == "tid")?.Value ?? string.Empty;

                    // This is expected to match the tenant ID of the selected tenant
                    if (!string.IsNullOrEmpty(tenantId) && Tenant != null && string.Equals(tenantId, Tenant.TenantId))
                    {
                        _userLoggedInProvider.TenantName = Tenant.DisplayName;
                    }
                }
            }
        }
    }
}
