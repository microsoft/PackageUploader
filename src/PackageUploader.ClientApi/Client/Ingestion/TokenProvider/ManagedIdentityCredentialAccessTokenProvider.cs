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

public class ManagedIdentityCredentialAccessTokenProvider : IAccessTokenProvider
{
    private readonly AccessTokenProviderConfig _config;
    private readonly ILogger<ManagedIdentityCredentialAccessTokenProvider> _logger;
    private readonly ManagedIdentityAuthInfo _managedIdentityAuthInfo;

    public ManagedIdentityCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<ManagedIdentityCredentialAccessTokenProvider> logger, IOptions<ManagedIdentityAuthInfo> managedIdentityAuthInfo)
    {
        _config = config.Value;
        _logger = logger;
        _managedIdentityAuthInfo = managedIdentityAuthInfo.Value;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = new ManagedIdentityCredentialOptions
        {
            AuthorityHost = new Uri(_config.AadAuthorityBaseUrl),
        };
        var azureCredential = new ManagedIdentityCredential(_managedIdentityAuthInfo.ClientId, azureCredentialOptions);

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