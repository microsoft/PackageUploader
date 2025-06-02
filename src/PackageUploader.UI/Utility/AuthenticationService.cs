// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text.Json;

namespace PackageUploader.UI.Utility
{
    public interface IAuthenticationService
    {
        Task<bool> SignInAsync();
        void SignOut();
        bool IsUserLoggedIn { get; }
        Task<List<TenantInfo>> GetAvailableTenants();
        string? TenantId { get; set; }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<AuthenticationService> _logger;
        private CancellationTokenSource _cancellationTokenSource = new();

        public bool IsUserLoggedIn => _userLoggedInProvider.UserLoggedIn;

        public string? TenantId { get; set; } = null;

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
                    CachableInteractiveBrowserCredentialAccessToken.TenantId = TenantId;
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

        public async Task<List<TenantInfo>> GetAvailableTenants()
        {
            try
            {
                // Cancel any previous token request
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                return await GetAvailableTenantsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available tenants");
                return new List<TenantInfo>();
            }
        }

        public async Task<IngestionAccessToken> GetAzureManagementTokenAsync(CancellationToken ct)
        {
            var azureCredentialOptions = new InteractiveBrowserCredentialOptions
            {
                AuthorityHost = new Uri($"https://login.microsoftonline.com/")
            };
            var azureCredential = new InteractiveBrowserCredential(azureCredentialOptions);

            return await GetAzureManagementAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
        }

        private async Task<IngestionAccessToken> GetAzureManagementAccessTokenAsync(TokenCredential tokenCredential, CancellationToken ct)
        {
            _logger.LogDebug("Requesting authentication token");
            var scopes = new[] { $"https://management.azure.com/.default" };
            var requestContext = new TokenRequestContext(scopes);
            var token = await tokenCredential.GetTokenAsync(requestContext, ct).ConfigureAwait(false);

            return new IngestionAccessToken
            {
                AccessToken = token.Token,
                ExpiresOn = token.ExpiresOn
            };
        }

        // TODO: This isn't the right place to do this but for a hacky prototype it's fine.
        private async Task<List<TenantInfo>> GetAvailableTenantsAsync(CancellationToken ct)
        {
            List<TenantInfo> tenants = [];
            try
            {
                HttpClient httpClient = new();

                var token = await GetAzureManagementTokenAsync(ct).ConfigureAwait(false);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.AccessToken);

                var request = await httpClient.GetAsync("https://management.azure.com/tenants?api-version=2022-12-01", ct);

                request.EnsureSuccessStatusCode();
                var content = await request.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Received tenant info: {Content}", content);

                // Deserialize the JSON response
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var response = JsonSerializer.Deserialize<TenantListResponse>(content, options);
                    
                    if (response?.Value != null)
                    {
                        foreach (var tenant in response.Value)
                        {
                            tenants.Add(new TenantInfo
                            {
                                Id = tenant.TenantId,
                                Name = tenant.DisplayName ?? tenant.TenantId
                            });
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to parse tenant JSON response");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user's tenant info.");
            }
            return tenants;
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
                TenantId = null;

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

        // Classes to deserialize the tenant API response
        private class TenantListResponse
        {
            public List<TenantDetails> Value { get; set; } = [];
        }

        private class TenantDetails
        {
            public string TenantId { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string? DefaultDomain { get; set; }
            public string? CountryCode { get; set; }
            public List<string>? Domains { get; set; }
        }
    }
}
