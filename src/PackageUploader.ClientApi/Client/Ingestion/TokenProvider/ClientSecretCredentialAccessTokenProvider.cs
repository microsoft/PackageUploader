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
    private readonly ClientSecretAuthInfo _clientSecretAuthInfo;

    public ClientSecretCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, 
        ILogger<ClientSecretCredentialAccessTokenProvider> logger,
        IOptions<ClientSecretAuthInfo> clientSecretAuthInfo) : base(config, logger)
    {
        _clientSecretAuthInfo = clientSecretAuthInfo?.Value ?? throw new ArgumentNullException(nameof(clientSecretAuthInfo), $"{nameof(clientSecretAuthInfo)} cannot be null.");

        if (string.IsNullOrWhiteSpace(_clientSecretAuthInfo.TenantId))
        {
            throw new ArgumentException($"TenantId not provided in {ClientSecretAuthInfo.ConfigName}.", nameof(clientSecretAuthInfo));
        }

        if (string.IsNullOrWhiteSpace(_clientSecretAuthInfo.ClientId))
        {
            throw new ArgumentException($"ClientId not provided in {ClientSecretAuthInfo.ConfigName}.", nameof(clientSecretAuthInfo));
        }

        if (string.IsNullOrWhiteSpace(_clientSecretAuthInfo.ClientSecret))
        {
            throw new ArgumentException($"ClientSecret not provided in {ClientSecretAuthInfo.ConfigName}.", nameof(clientSecretAuthInfo));
        }
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new ClientSecretCredentialOptions());
        var azureCredential = new ClientSecretCredential(_clientSecretAuthInfo.TenantId, 
            _clientSecretAuthInfo.ClientId, _clientSecretAuthInfo.ClientSecret, azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}