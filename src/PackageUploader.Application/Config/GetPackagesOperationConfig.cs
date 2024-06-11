// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class GetPackagesOperationValidator : IValidateOptions<GetPackagesOperationConfig>;

internal class GetPackagesOperationConfig : PackageBranchOperationConfig, IValidatableObject
{
    internal override string GetOperationName() => "GetPackages";

    public string MarketGroupName { get; set; } = "default";

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var validationResult in base.Validate(validationContext))
            yield return validationResult;

        if (string.IsNullOrWhiteSpace(MarketGroupName))
        {
            MarketGroupName = "default";
        }
    }
}