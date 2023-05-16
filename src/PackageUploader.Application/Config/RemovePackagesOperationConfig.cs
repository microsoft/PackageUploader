// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Config;

internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
{
    internal override string GetOperationName() => "RemovePackages";

    public string MarketGroupName { get; set; } = null;
    public string PackageFileName { get; set; } = null;
    public bool UseRegexMatch { get; set; } = false;
}