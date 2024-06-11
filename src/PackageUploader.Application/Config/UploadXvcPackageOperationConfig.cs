// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadXvcPackageOperationValidator : IValidateOptions<UploadXvcPackageOperationConfig>;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig, IXvcGameConfiguration
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    [ValidateObjectMembers]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public GamePackageDate PreDownloadDate { get; set; }

    protected override void Validate(IList<ValidationResult> validationResults)
    {
        base.Validate(validationResults);

        if (PreDownloadDate?.IsEnabled == true && (AvailabilityDate?.IsEnabled != true))
        {
            validationResults.Add(new ValidationResult($"{nameof(PreDownloadDate)} needs {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]));
        }

        if (PreDownloadDate?.IsEnabled == true && AvailabilityDate?.IsEnabled == true && PreDownloadDate.EffectiveDate > AvailabilityDate.EffectiveDate)
        {
            validationResults.Add(new ValidationResult($"{nameof(PreDownloadDate)} needs to be before {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]));
        }
    }
}