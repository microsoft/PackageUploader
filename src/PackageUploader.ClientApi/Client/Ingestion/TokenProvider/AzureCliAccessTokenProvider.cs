// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class AzureCliAccessTokenProvider : IAccessTokenProvider
{
    private readonly AccessTokenProviderConfig _config;
    private readonly ILogger<AzureCliAccessTokenProvider> _logger;

    public AzureCliAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<AzureCliAccessTokenProvider> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = new AzureCliCredentialOptions
        {
            AuthorityHost = new Uri(_config.AadAuthorityBaseUrl),
        };
        var azureCredential = new AzureCliCredential(azureCredentialOptions);

        _logger.LogDebug("Requesting authentication token");
        var scopes = new[] { $"{_config.AadResourceForCaller}/.default" };
        var requestContext = new TokenRequestContext(scopes);
        var token = await azureCredential.GetTokenAsync(requestContext, ct).ConfigureAwait(false);

        return new IngestionAccessToken
        {
            AccessToken = token.Token, 
            ExpiresOn = token.ExpiresOn
        };
    }
}