// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Config
{
    internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override string GetOperationName() => "RemovePackages";

        public string MarketGroupName { get; set; } = null;
    }
}
