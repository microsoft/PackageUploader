// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

internal static class CertificateAccessTokenProviderExtensions
{
    internal static X509Certificate2 GetCertificate(this AzureApplicationCertificateAuthInfo aadAuthInfo)
    {
        if (!string.IsNullOrWhiteSpace(aadAuthInfo.CertificateThumbprint))
        {
            var certificates = GetCertificatesByThumbprint(aadAuthInfo);
            return (certificates?.Count) switch
            {
                1 => certificates[0],
                > 1 => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} found more than once.", nameof(aadAuthInfo)),
                _ => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} not found.", nameof(aadAuthInfo)),
            };
        }
        else
        {
            var certificates = GetCertificatesBySubject(aadAuthInfo);
            return (certificates?.Count) switch
            {
                1 => certificates[0],
                > 1 => certificates.OrderByDescending(x => x.NotBefore).First(),
                _ => throw new ArgumentException($"Certificate provided in {AadAuthInfo.ConfigName} not found.", nameof(aadAuthInfo)),
            };
        }
    }

    private static X509Certificate2Collection GetCertificatesByThumbprint(AzureApplicationCertificateAuthInfo aadAuthInfo)
    {
        using var store = new X509Store(aadAuthInfo.CertificateStore, aadAuthInfo.CertificateLocation);

        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, aadAuthInfo.CertificateThumbprint, false);
        store.Close();

        return certs;
    }

    private static X509Certificate2Collection GetCertificatesBySubject(AzureApplicationCertificateAuthInfo aadAuthInfo)
    {
        using var store = new X509Store(aadAuthInfo.CertificateStore, aadAuthInfo.CertificateLocation);

        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, aadAuthInfo.CertificateSubject, false);
        store.Close();

        return certs;
    }
}