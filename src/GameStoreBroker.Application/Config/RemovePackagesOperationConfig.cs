// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Schema
{
    internal class RemovePackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override string GetOperationName() => "RemovePackages";

        public string MarketGroupId { get; set; } = null;
    }
}
