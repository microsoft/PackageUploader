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

public class EnvironmentCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    public EnvironmentCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<EnvironmentCredentialAccessTokenProvider> logger) : base(config, logger)
    {
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new EnvironmentCredentialOptions());
        var azureCredential = new EnvironmentCredential(azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}