// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class ImportPackagesOperationValidator : IValidateOptions<ImportPackagesOperationConfig>;

internal class ImportPackagesOperationConfig : PackageBranchOperationConfig, IGameConfiguration, IValidatableObject
{
    internal override string GetOperationName() => "ImportPackages";

    public string MarketGroupName { get; set; } = null;

    public string DestinationBranchFriendlyName { get; set; }
    public string DestinationFlightName { get; set; }

    public GamePackageDate AvailabilityDate { get; set; }
    public GamePackageDate PreDownloadDate { get; set; }
    public GamePackageDate MandatoryDate { get; set; }
    public GameGradualRolloutInfo GradualRollout { get; set; }
    public bool Overwrite { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            yield return new ValidationResult($"{nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is required.", [nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName)]);
        }

        if (!string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && !string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            yield return new ValidationResult($"Only one {nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is allowed.", [nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName)]);
        }

        if (PreDownloadDate?.IsEnabled == true && (AvailabilityDate?.IsEnabled != true))
        {
            yield return new ValidationResult($"{nameof(PreDownloadDate)} needs {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]);
        }

        if (PreDownloadDate?.IsEnabled == true && AvailabilityDate?.IsEnabled == true && PreDownloadDate.GetEffectiveDate() > AvailabilityDate.GetEffectiveDate())
        {
            yield return new ValidationResult($"{nameof(PreDownloadDate)} needs to be before {nameof(AvailabilityDate)}.", [nameof(PreDownloadDate), nameof(AvailabilityDate)]);
        }
    }
}