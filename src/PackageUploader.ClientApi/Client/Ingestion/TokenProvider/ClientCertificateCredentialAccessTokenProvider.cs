// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class ClientCertificateCredentialAccessTokenProvider : CredentialAccessTokenProvider, IAccessTokenProvider
{
    private readonly AzureApplicationCertificateAuthInfo _aadAuthInfo;
    private readonly X509Certificate2 _certificate;

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

        if (!string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateThumbprint))
        {
            var certificates = GetCertificatesByThumbprint();
            _certificate = (certificates?.Count) switch
            {
                1 => certificates[0],
                > 1 => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} found more than once.", nameof(aadAuthInfo)),
                _ => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} not found.", nameof(aadAuthInfo)),
            };
        }
        else
        {
            var certificates = GetCertificatesBySubject();
            _certificate = (certificates?.Count) switch
            {
                1 => certificates[0],
                > 1 => certificates.OrderByDescending(x => x.NotBefore).First(),
                _ => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} not found.", nameof(aadAuthInfo)),
            };
        }
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var azureCredentialOptions = SetTokenCredentialOptions(new ClientCertificateCredentialOptions());
        var azureCredential = new ClientCertificateCredential(_aadAuthInfo.TenantId, _aadAuthInfo.ClientId, _certificate, azureCredentialOptions);

        return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
    }

    private X509Certificate2Collection GetCertificatesByThumbprint()
    {
        using var store = new X509Store(_aadAuthInfo.CertificateStore, _aadAuthInfo.CertificateLocation);

        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, _aadAuthInfo.CertificateThumbprint, false);
        store.Close();

        return certs;
    }

    private X509Certificate2Collection GetCertificatesBySubject()
    {
        using var store = new X509Store(_aadAuthInfo.CertificateStore, _aadAuthInfo.CertificateLocation);

        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, _aadAuthInfo.CertificateSubject, false);
        store.Close();

        return certs;
    }
}