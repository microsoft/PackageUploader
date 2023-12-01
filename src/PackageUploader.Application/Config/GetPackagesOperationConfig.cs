// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

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