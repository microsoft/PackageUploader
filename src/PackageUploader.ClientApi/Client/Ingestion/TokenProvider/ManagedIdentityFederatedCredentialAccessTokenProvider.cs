// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class ManagedIdentityFederatedCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly ManagedIdentityFederatedAuthInfo _managedIdentityFederatedAuthInfo;

    public ManagedIdentityFederatedCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, ILogger<ManagedIdentityFederatedCredentialAccessTokenProvider> logger, 
        IOptions<ManagedIdentityFederatedAuthInfo> managedIdentityFederatedAuthInfo) : base(config, logger)
    {
        _managedIdentityFederatedAuthInfo = managedIdentityFederatedAuthInfo.Value;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var miCredentialOptions = SetTokenCredentialOptions(new ManagedIdentityCredentialOptions());
        var miCredential = new ManagedIdentityCredential(_managedIdentityFederatedAuthInfo.ClientId, miCredentialOptions);
        var tokenRequestContext = new TokenRequestContext(["api://AzureADTokenExchange/.default"]);

        var azureCredentialOptions = SetTokenCredentialOptions(new ClientAssertionCredentialOptions());
        var azureCredential = new ClientAssertionCredential(
            _managedIdentityFederatedAuthInfo.ApplicationTenantId,
            _managedIdentityFederatedAuthInfo.ApplicationClientId,
            async ct =>
            (await miCredential
                .GetTokenAsync(tokenRequestContext, ct)
                .ConfigureAwait(false)).Token,
            azureCredentialOptions
            );

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}