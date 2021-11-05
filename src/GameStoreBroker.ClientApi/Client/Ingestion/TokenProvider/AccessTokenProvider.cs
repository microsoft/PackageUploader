﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Config;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly ILogger<AccessTokenProvider> _logger;
        private readonly AccessTokenProviderConfig _config;
        private readonly AadAuthInfo _aadAuthInfo;

        public AccessTokenProvider(IOptions<AccessTokenProviderConfig> config, IOptions<AadAuthInfo> aadAuthInfo, ILogger<AccessTokenProvider> logger)
        {
            _logger = logger;
            _config = config.Value;
            _aadAuthInfo = aadAuthInfo?.Value ?? throw new ArgumentNullException(nameof(aadAuthInfo), $"{nameof(aadAuthInfo)} cannot be null.");

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.TenantId))
            {
                throw new ArgumentException("TenantId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientId))
            {
                throw new ArgumentException("ClientId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientSecret))
            {
                throw new ArgumentException("ClientSecret not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }
        }

        public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
        {
            var authority = _config.AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
            var authenticationContext = new AuthenticationContext(authority, true);

            var clientCredential = new ClientCredential(_aadAuthInfo.ClientId, _aadAuthInfo.ClientSecret);

            _logger.LogDebug("Requesting authentication token");
            var result = await authenticationContext.AcquireTokenAsync(_config.AadResourceForCaller, clientCredential).ConfigureAwait(false);

            if (result is null)
            {
                throw new Exception("Failure while acquiring token.");
            }

            return new IngestionAccessToken { AccessToken = result.AccessToken, ExpiresOn = result.ExpiresOn };
        }
    }
}