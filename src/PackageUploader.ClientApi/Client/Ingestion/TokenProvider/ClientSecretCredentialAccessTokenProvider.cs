// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class ClientSecretCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly AzureApplicationSecretAuthInfo _aadAuthInfo;

    public ClientSecretCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, IOptions<AzureApplicationSecretAuthInfo> aadAuthInfo, 
        ILogger<ClientSecretCredentialAccessTokenProvider> logger) : base(config, logger)
    {
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
        var azureCredentialOptions = SetTokenCredentialOptions(new ClientSecretCredentialOptions());
        var azureCredential = new ClientSecretCredential(_aadAuthInfo.TenantId, _aadAuthInfo.ClientId, _aadAuthInfo.ClientSecret, azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}