// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public abstract class CredentialAccessTokenProvider
{
    private readonly AccessTokenProviderConfig _config;
    private readonly ILogger _logger;

    protected CredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    protected T SetTokenCredentialOptions<T>(T tokenCredentialOptions) where T : TokenCredentialOptions
    {
        tokenCredentialOptions.AuthorityHost = new Uri(_config.AadAuthorityBaseUrl);
        return tokenCredentialOptions;
    }

    protected async Task<IngestionAccessToken> GetIngestionAccessTokenAsync(TokenCredential tokenCredential, CancellationToken ct)
    {
        _logger.LogDebug("Requesting authentication token");
        var scopes = new[] { $"{_config.AadResourceForCaller}/.default" };
        var requestContext = new TokenRequestContext(scopes);
        var token = await tokenCredential.GetTokenAsync(requestContext, ct).ConfigureAwait(false);

        return new IngestionAccessToken
        {
            AccessToken = token.Token, 
            ExpiresOn = token.ExpiresOn
        };
    }
}