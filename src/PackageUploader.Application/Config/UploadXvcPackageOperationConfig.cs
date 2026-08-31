// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Models;
using PackageUploader.ClientApi.Packaging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadXvcPackageOperationValidator : IValidateOptions<UploadXvcPackageOperationConfig>;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig, IXvcGameConfiguration, IValidatableObject
{
    internal override string GetOperationName() => "UploadXvcPackage";

    // Not [Required] at the attribute level: this field is only meaningful for a package PackageUploader
    // uploads itself. MSIXVC2 packages are uploaded by MakePkg.exe, which discovers EKB and
    // submission-validator assets by co-location with the package rather than by path, and loose content is
    // refused outright. Requiredness is enforced in Validate() for everything else, so XVC1 behaviour is
    // unchanged.
    [ValidateObjectMembers]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public GamePackageDate PreDownloadDate { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        // Loose content is exempt for the same reason MSIXVC2 is: it never reaches the upload path that
        // consumes these assets. Demanding them here would replace the operation's specific "this is loose
        // content, PackageUploader cannot build a package" message with a misleading complaint about an
        // unrelated field, which is the opposite of helpful.
        if (GameAssets is null &&
            !PackageFormatDetector.IsLikelyMsixvc2Package(PackageFilePath) &&
            !PackageFormatDetector.IsLooseGameContent(PackageFilePath))
        {
            yield return new ValidationResult($"The {nameof(GameAssets)} field is required.", [nameof(GameAssets)]);
        }

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