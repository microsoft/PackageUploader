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

public class AzureCliCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    public AzureCliCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<AzureCliCredentialAccessTokenProvider> logger) : base(config, logger)
    { }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new AzureCliCredentialOptions());
        var azureCredential = new AzureCliCredential(azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}