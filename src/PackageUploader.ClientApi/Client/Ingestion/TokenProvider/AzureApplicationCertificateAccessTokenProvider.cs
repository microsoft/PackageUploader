// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

public class AzureApplicationCertificateAccessTokenProvider : IAccessTokenProvider
{
    private readonly ILogger<AzureApplicationCertificateAccessTokenProvider> _logger;
    private readonly AccessTokenProviderConfig _config;
    private readonly AzureApplicationCertificateAuthInfo _aadAuthInfo;

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

        if (string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateThumbprint) && string.IsNullOrWhiteSpace(_aadAuthInfo.CertificateSubject))
        {
            throw new ArgumentException($"CertificateThumbprint or CertificateSubject not provided in {AadAuthInfo.ConfigName}.", nameof(aadAuthInfo));
        }
    }

    public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
    {
        var certificate = _aadAuthInfo.GetCertificate();
        var authority = _config.AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
        var msalClient = ConfidentialClientApplicationBuilder
            .Create(_aadAuthInfo.ClientId)
            .WithCertificate(certificate)
            .WithAuthority(authority)
            .Build();

        _logger.LogDebug("Requesting authentication token");
        var scopes = new[] { $"{_config.AadResourceForCaller}/.default" };
        var result = await msalClient.AcquireTokenForClient(scopes).ExecuteAsync(ct).ConfigureAwait(false) 
            ?? throw new Exception("Failure while acquiring token.");

        return new IngestionAccessToken
        {
            AccessToken = result.AccessToken,
            ExpiresOn = result.ExpiresOn,
        };
    }
}