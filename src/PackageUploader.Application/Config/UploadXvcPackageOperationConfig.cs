// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadXvcPackageOperationValidator : IValidateOptions<UploadXvcPackageOperationConfig>;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig, IXvcGameConfiguration, IValidatableObject
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    [ValidateObjectMembers]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public GamePackageDate PreDownloadDate { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (PreDownloadDate is { IsEnabled: true, EffectiveDate: null })
        {
            yield return new ValidationResult($"If {nameof(PreDownloadDate)} {nameof(PreDownloadDate.IsEnabled)} is true, {nameof(PreDownloadDate.EffectiveDate)} needs to be set.", [nameof(PreDownloadDate)]);
        }

        if (PreDownloadDate?.IsEnabled == true && (AvailabilityDate?.IsEnabled != true))
        {
            yield return new ValidationResult($"{nameof(PreDownloadDate)} needs {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]);
        }

        if (PreDownloadDate?.IsEnabled == true && AvailabilityDate?.IsEnabled == true && PreDownloadDate.EffectiveDate > AvailabilityDate.EffectiveDate)
        {
            yield return new ValidationResult($"{nameof(PreDownloadDate)} needs to be before {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]);
        }
    }
}