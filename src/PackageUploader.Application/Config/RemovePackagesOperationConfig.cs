// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Operations;

namespace PackageUploader.Application.Config
{
    internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.RemovePackages;

        public string MarketGroupName { get; set; } = null;
    }
}
