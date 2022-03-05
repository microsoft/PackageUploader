// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

internal class ImportPackagesOperationConfig : PackageBranchOperationConfig, IGameConfiguration
{
    internal override string GetOperationName() => "ImportPackages";

    public string MarketGroupName { get; set; } = null;

    public string DestinationBranchFriendlyName { get; set; }
    public string DestinationFlightName { get; set; }

    public GamePackageDate AvailabilityDate { get; set; }
    public GamePackageDate MandatoryDate { get; set; }
    public GameGradualRolloutInfo GradualRollout { get; set; }

    public bool Overwrite { get; set; }

    protected override void Validate(IList<ValidationResult> validationResults)
    {
        base.Validate(validationResults);
        if (string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            validationResults.Add(new ValidationResult($"{nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is required.", new[] { nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName) }));
        }

        if (!string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && !string.IsNullOrWhiteSpace(DestinationFlightName))
        {
            validationResults.Add(new ValidationResult($"Only one {nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is allowed.", new[] { nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName) }));
        }
    }
}