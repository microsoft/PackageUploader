// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class AzureApplicationCertificateAccessTokenProvider : IAccessTokenProvider
{
    private readonly ILogger<AzureApplicationCertificateAccessTokenProvider> _logger;
    private readonly AccessTokenProviderConfig _config;
    private readonly AzureApplicationCertificateAuthInfo _aadAuthInfo;
    private readonly X509Certificate2 _certificate;

    public AzureApplicationCertificateAccessTokenProvider(IOptions<AccessTokenProviderConfig> config, IOptions<AzureApplicationCertificateAuthInfo> aadAuthInfo, 
        ILogger<AzureApplicationCertificateAccessTokenProvider> logger)
    {
        _logger = logger;
        _config = config.Value;
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

        if (string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateThumbprint))
        {
            throw new ArgumentException($"CertificateThumbprint not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
        }

        var certificates = GetCertificates();
        _certificate = (certificates?.Count) switch
        {
            1 => certificates[0],
            > 1 => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} found more than once.", nameof(aadAuthInfo)),
            _ => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} not found.", nameof(aadAuthInfo)),
        };
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var authority = _config.AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
        var msalClient = ConfidentialClientApplicationBuilder
            .Create(_aadAuthInfo.ClientId)
            .WithCertificate(_certificate)
            .WithAuthority(authority, true)
            .Build();

        _logger.LogDebug("Requesting authentication token");
        var scopes = new[] { $"{_config.AadResourceForCaller}/.default" };
        var result = await msalClient.AcquireTokenForClient(scopes).ExecuteAsync(ct).ConfigureAwait(false);

        if (result is null)
        {
            throw new Exception("Failure while acquiring token.");
        }

        return new IngestionAccessToken
        {
            AccessToken = result.AccessToken,
            ExpiresOn = result.ExpiresOn,
        };
    }

    private X509Certificate2Collection GetCertificates()
    {
        using var store = new X509Store(_aadAuthInfo.CertificateStore, _aadAuthInfo.CertificateLocation);

        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, _aadAuthInfo.CertificateThumbprint, true);
        store.Close();

        return certs;
    }
}