// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Config;
using Microsoft.Extensions.Options;
using System;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public class AzureAccessTokenProvider : IAccessTokenProvider
    {
        private readonly AccessTokenProviderConfig _config;

        public AzureAccessTokenProvider(IOptions<AccessTokenProviderConfig> config)
        {
            _config = config.Value;
        }

        public string GetAccessToken()
        {
            var azureCredentialOptions = new DefaultAzureCredentialOptions { AuthorityHost = new Uri(_config.AadAuthorityBaseUrl) };
            var azureCredential = new DefaultAzureCredential(azureCredentialOptions);
            var token = azureCredential.GetToken(new TokenRequestContext(new[] { _config.AadResourceForCaller }));
            return token.Token;
        }
    }
}