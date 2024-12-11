// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class AzurePipelinesCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly AzurePipelinesAuthInfo _azurePipelinesAuthInfo;

    public AzurePipelinesCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<AzurePipelinesCredentialAccessTokenProvider> logger, 
        IOptions<AzurePipelinesAuthInfo> azurePipelinesAuthInfo) : base(config, logger)
    {
        _azurePipelinesAuthInfo = azurePipelinesAuthInfo.Value;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new AzurePipelinesCredentialOptions());
        var azureCredential = new AzurePipelinesCredential(_azurePipelinesAuthInfo.TenantId, _azurePipelinesAuthInfo.ClientId, _azurePipelinesAuthInfo.ServiceConnectionId,
            _azurePipelinesAuthInfo.SystemAccessToken, azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}