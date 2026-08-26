// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Command line and configuration values that are not part of the bound operation configuration but are
/// still needed to build MakePkg.exe arguments — principally the authentication surface, which
/// PackageUploader resolves from a mixture of command line options and configuration sections.
///
/// Every parameter except <paramref name="AuthenticationMethod"/> is OPTIONAL and may be null or empty —
/// which credentials are present depends on the authentication method the user chose. The builder decides
/// which ones a given method actually requires and fails with an actionable message when one is missing;
/// consumers must not assume any of them is populated.
/// </summary>
/// <param name="AuthenticationMethod">The --Authentication value the user selected.</param>
/// <param name="TenantId">AAD tenant, forwarded to MakePkg.exe via /tenantid.</param>
/// <param name="ClientId">AAD application (client) id, forwarded via /clientid.</param>
/// <param name="ClientSecret">AAD application secret, forwarded via /clientsecret.</param>
/// <param name="CertificateThumbprint">Certificate thumbprint, forwarded via /certthumbprint.</param>
/// <param name="CertificateSubject">Certificate subject name, forwarded via /certsubject.</param>
/// <param name="CertificateStore">Certificate store name, forwarded via /certstore.</param>
/// <param name="CertificateLocation">Certificate store location, forwarded via /certlocation.</param>
/// <param name="CertificatePath">Path to a certificate file, forwarded via /certpath.</param>
/// <param name="CertificatePassword">
/// Password for a password-protected PFX/PKCS12 file, forwarded via /certpassword. A CREDENTIAL: it must
/// never reach a log.
/// </param>
/// <param name="ResourceId">Azure resource id, forwarded via /resourceid for ManagedIdentityFederated.</param>
internal sealed record Msixvc2CommandLineContext(
    IngestionExtensions.AuthenticationMethod AuthenticationMethod,
    string TenantId = null,
    string ClientId = null,
    string ClientSecret = null,
    string CertificateThumbprint = null,
    string CertificateSubject = null,
    string CertificateStore = null,
    string CertificateLocation = null,
    string CertificatePath = null,
    string CertificatePassword = null,
    string ResourceId = null);
