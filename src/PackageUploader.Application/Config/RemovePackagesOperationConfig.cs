// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
{
    internal override string GetOperationName() => "RemovePackages";

    public string MarketGroupName { get; set; } = null;

    [Required]
    public string PackageFileName { get; set; } = null;
}