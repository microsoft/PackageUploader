// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class ClientCertificateCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly ClientCertificateAuthInfo _clientCertificateAuthInfo;

    public ClientCertificateCredentialAccessTokenProvider(
        IOptions<AccessTokenProviderConfig> config,
        ILogger<ClientCertificateCredentialAccessTokenProvider> logger,
        IOptions<ClientCertificateAuthInfo> clientCertificateAuthInfo) : base(config, logger)
    {
        _clientCertificateAuthInfo = clientCertificateAuthInfo.Value;
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        X509Certificate2 certificate;

        if (!string.IsNullOrEmpty(_clientCertificateAuthInfo.CertificatePassword))
        {
            certificate = new X509Certificate2(
                _clientCertificateAuthInfo.CertificatePath,
                _clientCertificateAuthInfo.CertificatePassword,
                X509KeyStorageFlags.DefaultKeySet);
        }
        else
        {
            certificate = new X509Certificate2(_clientCertificateAuthInfo.CertificatePath);
        }

        var azureCredentialOptions = SetTokenCredentialOptions(new ClientCertificateCredentialOptions());
        var azureCredential = new ClientCertificateCredential(
            _clientCertificateAuthInfo.TenantId,
            _clientCertificateAuthInfo.ClientId,
            certificate,
            azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }
}