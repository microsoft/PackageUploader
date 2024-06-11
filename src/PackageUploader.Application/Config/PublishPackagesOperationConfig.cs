// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class PublishPackagesOperationValidator : IValidateOptions<PublishPackagesOperationConfig>;

internal sealed class PublishPackagesOperationConfig : PackageBranchOperationConfig, IValidatableObject
{
    internal override string GetOperationName() => "PublishPackages";

    public string DestinationSandboxName { get; set; }
    public int MinutesToWaitForPublishing { get; set; }
    public GamePublishConfiguration PublishConfiguration { get; set; }

    private const string RetailSandboxName = "RETAIL";

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if ((!string.IsNullOrWhiteSpace(FlightName) || string.IsNullOrWhiteSpace(BranchFriendlyName) || string.IsNullOrWhiteSpace(DestinationSandboxName)) &&
            (string.IsNullOrWhiteSpace(FlightName) || !string.IsNullOrWhiteSpace(BranchFriendlyName) || !string.IsNullOrWhiteSpace(DestinationSandboxName)))
        {
            yield return new ValidationResult($"{nameof(FlightName)} or ({nameof(BranchFriendlyName)} and {nameof(DestinationSandboxName)}) field is required.",
                [nameof(FlightName), nameof(BranchFriendlyName), nameof(DestinationSandboxName)]);
        }

        if (!string.IsNullOrWhiteSpace(DestinationSandboxName) && DestinationSandboxName.Equals(RetailSandboxName, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult($"Publishing packages to {RetailSandboxName} sandbox is not permitted through this tool.", 
                [nameof(DestinationSandboxName)]);
        }
    }
}