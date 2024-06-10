// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Config;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig, IXvcGameConfiguration
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public GamePackageDate PreDownloadDate { get; set; }

    protected override void Validate(IList<ValidationResult> validationResults)
    {
        base.Validate(validationResults);

        if (PreDownloadDate?.IsEnabled == true && (AvailabilityDate?.IsEnabled != true))
        {
            validationResults.Add(new ValidationResult($"{nameof(PreDownloadDate)} needs {nameof(AvailabilityDate)}.", new[] { nameof(PreDownloadDate), nameof(AvailabilityDate) }));
        }

        if (PreDownloadDate?.IsEnabled == true && AvailabilityDate?.IsEnabled == true && PreDownloadDate.EffectiveDate > AvailabilityDate.EffectiveDate)
        {
            validationResults.Add(new ValidationResult($"{nameof(PreDownloadDate)} needs to be before {nameof(AvailabilityDate)}.", new[] { nameof(PreDownloadDate), nameof(AvailabilityDate) }));
        }
    }
}