// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Config;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public class AzureApplicationSecretAccessTokenProvider : IAccessTokenProvider
    {
        private readonly ILogger<AzureApplicationSecretAccessTokenProvider> _logger;
        private readonly AccessTokenProviderConfig _config;
        private readonly AzureApplicationSecretAuthInfo _aadAuthInfo;

        public AzureApplicationSecretAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, IOptions<AzureApplicationSecretAuthInfo> aadAuthInfo, 
            ILogger<AzureApplicationSecretAccessTokenProvider> logger)
        {
            _logger = logger;
            _config = config.Value;
            _aadAuthInfo = aadAuthInfo?.Value ?? throw new ArgumentNullException(nameof(aadAuthInfo), $"{nameof(aadAuthInfo)} cannot be null.");

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.TenantId))
            {
                throw new ArgumentException($"TenantId not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientId))
            {
                throw new ArgumentException($"ClientId not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientSecret))
            {
                throw new ArgumentException($"ClientSecret not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
            }
        }

        public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
        {
            var authority = _config.AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
            var msalClient = ConfidentialClientApplicationBuilder
               .Create(_aadAuthInfo.ClientId)
               .WithClientSecret(_aadAuthInfo.ClientSecret)
               .WithAuthority(authority, true)
               .Build();

            _logger.LogDebug("Requesting authentication token");
            var scopes = new[] { $"{_config.AadResourceForCaller}/.default" };
            var result = await msalClient.AcquireTokenForClient(scopes).ExecuteAsync(ct).ConfigureAwait(false);

            if (result is null)
            {
                throw new Exception("Failure while acquiring token.");
            }

            return new IngestionAccessToken
            {
                AccessToken = result.AccessToken,
                ExpiresOn = result.ExpiresOn,
            };

        }
    }
}
