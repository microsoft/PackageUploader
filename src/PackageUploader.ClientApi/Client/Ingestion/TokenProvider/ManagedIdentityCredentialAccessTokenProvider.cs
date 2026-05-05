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

public class ManagedIdentityCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly ManagedIdentityAuthInfo _managedIdentityAuthInfo;

    public ManagedIdentityCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<ManagedIdentityCredentialAccessTokenProvider> logger, 
        IOptions<ManagedIdentityAuthInfo> managedIdentityAuthInfo) : base(config, logger)
    {
        _managedIdentityAuthInfo = managedIdentityAuthInfo.Value;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var managedIdentity = ManagedIdentityId.FromUserAssignedClientId(_managedIdentityAuthInfo.ClientId);
        var azureCredentialOptions = SetTokenCredentialOptions(new ManagedIdentityCredentialOptions(managedIdentity));
        var azureCredential = new ManagedIdentityCredential(azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}