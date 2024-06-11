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

internal class ImportPackagesOperationConfig : PackageBranchOperationConfig, IGameConfiguration
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

    protected override void Validate(List<ValidationResult> validationResults)
    {
        base.Validate(validationResults);
        if (string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            validationResults.Add(new ValidationResult($"{nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is required.", [nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName)]));
        }

        if (!string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && !string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            validationResults.Add(new ValidationResult($"Only one {nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is allowed.", [nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName)]));
        }

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