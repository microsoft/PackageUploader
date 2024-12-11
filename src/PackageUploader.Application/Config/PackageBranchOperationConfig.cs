// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

internal abstract class PackageBranchOperationConfig : BaseOperationConfig, IValidatableObject
{
    public string BranchFriendlyName { get; set; }
    public string FlightName { get; set; }

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (string.IsNullOrWhiteSpace(BranchFriendlyName) && string.IsNullOrWhiteSpace(FlightName))
        {
            yield return new ValidationResult($"{nameof(BranchFriendlyName)} or {nameof(FlightName)} field is required.", [nameof(BranchFriendlyName), nameof(FlightName)]);
        }

        if (!string.IsNullOrWhiteSpace(BranchFriendlyName) && !string.IsNullOrWhiteSpace(FlightName))
        {
            yield return new ValidationResult($"Only one {nameof(BranchFriendlyName)} or {nameof(FlightName)} field is allowed.", [nameof(BranchFriendlyName), nameof(FlightName)]);
        }
    }
}