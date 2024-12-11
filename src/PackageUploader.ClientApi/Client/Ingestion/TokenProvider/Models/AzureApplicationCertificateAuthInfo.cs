// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class AzureApplicationCertificateAuthInfoValidator : IValidateOptions<AzureApplicationCertificateAuthInfo>
{ }

public sealed class AzureApplicationCertificateAuthInfo : AadAuthInfo, IValidatableObject
{
    [Required]
    public string CertificateStore { get; set; } = "My";

    [Required]
    public StoreLocation CertificateLocation { get; set; } = StoreLocation.CurrentUser;

    public string CertificateThumbprint { get; set; }

    public string CertificateSubject { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(CertificateThumbprint) && string.IsNullOrWhiteSpace(CertificateSubject))
        {
            yield return new ValidationResult($"{nameof(CertificateThumbprint)} or {nameof(CertificateSubject)} field is required.", [nameof(CertificateThumbprint), nameof(CertificateSubject)]);
        }

        if (!string.IsNullOrWhiteSpace(CertificateThumbprint) && !string.IsNullOrWhiteSpace(CertificateSubject))
        {
            yield return new ValidationResult($"Only one {nameof(CertificateThumbprint)} or {nameof(CertificateSubject)} field is allowed.", [nameof(CertificateThumbprint), nameof(CertificateSubject)]);
        }
    }
}