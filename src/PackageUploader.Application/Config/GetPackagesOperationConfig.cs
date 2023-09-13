// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Config;

internal class GetPackagesOperationConfig : PackageBranchOperationConfig
{
    internal override string GetOperationName() => "GetBranch";

    public string MarketGroupName { get; set; } = "default";
}