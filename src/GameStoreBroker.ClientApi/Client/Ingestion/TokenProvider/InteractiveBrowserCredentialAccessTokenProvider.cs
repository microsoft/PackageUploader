﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Config;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public class InteractiveBrowserCredentialAccessTokenProvider : IAccessTokenProvider
    {
        private readonly AccessTokenProviderConfig _config;
        private readonly ILogger<InteractiveBrowserCredentialAccessTokenProvider> _logger;

        public InteractiveBrowserCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<InteractiveBrowserCredentialAccessTokenProvider> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
        {
            var azureCredentialOptions = new InteractiveBrowserCredentialOptions
            {
                AuthorityHost = new Uri(_config.AadAuthorityBaseUrl),
            };
            var azureCredential = new InteractiveBrowserCredential(azureCredentialOptions);

            _logger.LogDebug("Authenticating user via the default browser");
            await azureCredential.AuthenticateAsync(ct).ConfigureAwait(false);

            var requestContext = new TokenRequestContext(new[] { _config.AadResourceForCaller });

            _logger.LogDebug("Requesting authentication token");
            var token = await azureCredential.GetTokenAsync(requestContext, ct).ConfigureAwait(false);

            return new IngestionAccessToken
            {
                AccessToken = token.Token, 
                ExpiresOn = token.ExpiresOn
            };
        }
    }
}