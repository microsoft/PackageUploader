﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class DefaultAzureCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    public DefaultAzureCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<DefaultAzureCredentialAccessTokenProvider> logger) : base(config, logger)
    { }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new DefaultAzureCredentialOptions());
        var azureCredential = new DefaultAzureCredential(azureCredentialOptions);   // CodeQL [SM05137] Intentially using default Azure credentials as provider implementation.

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}