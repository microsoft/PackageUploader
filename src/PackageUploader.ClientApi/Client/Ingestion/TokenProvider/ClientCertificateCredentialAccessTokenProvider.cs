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

public class ClientCertificateCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly AzureApplicationCertificateAuthInfo _aadAuthInfo;

    public ClientCertificateCredentialAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, IOptions<AzureApplicationCertificateAuthInfo> aadAuthInfo, 
        ILogger<ClientCertificateCredentialAccessTokenProvider> logger) : base(config, logger)
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

        if (string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateStore))
        {
            throw new ArgumentException($"CertificateStore not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
        }

        if (string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateThumbprint) && string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateSubject))
        {
            throw new ArgumentException($"CertificateThumbprint or CertificateSubject not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
        }
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var certificate = _aadAuthInfo.GetCertificate();
        var azureCredentialOptions = SetTokenCredentialOptions(new ClientCertificateCredentialOptions());
        var azureCredential = new ClientCertificateCredential(_aadAuthInfo.TenantId, _aadAuthInfo.ClientId, certificate, azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}