// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Config
{
    internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override Operations.OperationName GetOperationName() => Operations.OperationName.RemovePackages;

        public string MarketGroupName { get; set; } = null;
    }
}
