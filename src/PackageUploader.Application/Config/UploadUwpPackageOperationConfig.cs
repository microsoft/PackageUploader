// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadUwpPackageOperationValidator : IValidateOptions<UploadUwpPackageOperationConfig>;

internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig, IUwpGameConfiguration, IValidatableObject
{
    internal override string GetOperationName() => "UploadUwpPackage";

    public GamePackageDate MandatoryDate { get; set; }
    public GameGradualRolloutInfo GradualRollout { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (MandatoryDate is { IsEnabled: true, EffectiveDate: null })
        {
            yield return new ValidationResult($"If {nameof(MandatoryDate)} {nameof(MandatoryDate.IsEnabled)} is true, {nameof(MandatoryDate.EffectiveDate)} needs to be set.", [nameof(MandatoryDate)]);
        }
    }
}