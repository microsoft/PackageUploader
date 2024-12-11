// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

internal abstract class UploadPackageOperationConfig : PackageBranchOperationConfig, IValidatableObject
{
    public string MarketGroupName { get; set; } = "default";

    [Range(0, 360)]
    public int MinutesToWaitForProcessing { get; set; } = 30;

    [Required]
    public string PackageFilePath { get; set; }

    public GamePackageDate AvailabilityDate { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (string.IsNullOrWhiteSpace(MarketGroupName))
        {
            MarketGroupName = "default";
        }

        if (AvailabilityDate is { IsEnabled: true, EffectiveDate: null })
        {
            yield return new ValidationResult($"If {nameof(AvailabilityDate)} {nameof(AvailabilityDate.IsEnabled)} is true, {nameof(AvailabilityDate.EffectiveDate)} needs to be set.", [nameof(AvailabilityDate)]);
        }
    }
}