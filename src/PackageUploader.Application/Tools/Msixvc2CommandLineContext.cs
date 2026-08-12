// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using PackageUploader.ClientApi;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Command line and configuration values that are not part of the bound operation configuration but are
/// still needed to build MakePkg.exe arguments — principally the authentication surface, which
/// PackageUploader resolves from a mixture of command line options and configuration sections.
/// </summary>
/// <param name="AuthenticationMethod">The --Authentication value the user selected.</param>
/// <param name="TenantId">AAD tenant, forwarded to MakePkg.exe via /tenantid.</param>
/// <param name="ClientId">AAD application (client) id, forwarded via /clientid.</param>
/// <param name="ClientSecret">AAD application secret, forwarded via /clientsecret.</param>
/// <param name="CertificateThumbprint">Certificate thumbprint, forwarded via /certthumbprint.</param>
/// <param name="CertificateSubject">
/// Certificate subject name. MakePkg.exe has no equivalent flag, so this is only carried so the builder
/// can fail with an actionable message instead of silently authenticating as the wrong identity.
/// </param>
/// <param name="CertificateStore">Certificate store name, forwarded via /certstore.</param>
/// <param name="CertificateLocation">Certificate store location, forwarded via /certlocation.</param>
/// <param name="CertificatePath">
/// Path to a PFX/PKCS12 certificate file. MakePkg.exe exposes /certpassword but no flag naming the
/// certificate file itself, so this is only carried so the builder can fail with an actionable message.
/// </param>
/// <param name="ResourceId">Azure resource id, forwarded via /resourceid for ManagedIdentityFederated.</param>
internal sealed record Msixvc2CommandLineContext(
    IngestionExtensions.AuthenticationMethod AuthenticationMethod,
    string? TenantId = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? CertificateThumbprint = null,
    string? CertificateSubject = null,
    string? CertificateStore = null,
    string? CertificateLocation = null,
    string? CertificatePath = null,
    string? ResourceId = null);
