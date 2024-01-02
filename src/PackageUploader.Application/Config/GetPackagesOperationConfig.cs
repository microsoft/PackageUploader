// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class GetPackagesOperationValidator : IValidateOptions<GetPackagesOperationConfig>;

internal class GetPackagesOperationConfig : PackageBranchOperationConfig
{
    internal override string GetOperationName() => "GetPackages";

    public string MarketGroupName { get; set; } = "default";

    protected override void Validate(IList<ValidationResult> validationResults)
    {
        if (string.IsNullOrWhiteSpace(MarketGroupName))
        {
            MarketGroupName = "default";
        }
    }
}