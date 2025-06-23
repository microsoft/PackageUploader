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

public class InteractiveBrowserCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly string _tenantId;

    public InteractiveBrowserCredentialAccessTokenProvider(
        IOptions<AccessTokenProviderConfig> config,
        ILogger<InteractiveBrowserCredentialAccessTokenProvider> logger,
        IOptions<BrowserAuthInfo> browserAuthInfo = null) : base(config, logger)
    {
        _tenantId = browserAuthInfo?.Value?.TenantId;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var options = new InteractiveBrowserCredentialOptions
        {
            AdditionallyAllowedTenants = { "*" }
        };

        // Set tenant ID if provided
        if (!string.IsNullOrEmpty(_tenantId))
        {
            Logger.LogInformation("Using specified tenant ID for browser authentication: {TenantId}", _tenantId);
            options.TenantId = _tenantId;
        }

        var azureCredentialOptions = SetTokenCredentialOptions(options);
        var azureCredential = new InteractiveBrowserCredential(azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}