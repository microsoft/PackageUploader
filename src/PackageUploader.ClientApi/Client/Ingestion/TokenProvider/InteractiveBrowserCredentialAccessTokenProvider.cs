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