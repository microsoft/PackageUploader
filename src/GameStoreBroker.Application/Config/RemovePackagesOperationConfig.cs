// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;

namespace GameStoreBroker.Application.Config
{
    internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.RemovePackages;

        public string MarketGroupName { get; set; } = null;
    }
}
