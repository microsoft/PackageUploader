// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Schema
{
    internal class RemovePackagesOperationSchema : PackageBranchOperationSchema
    {
        internal override string GetOperationName() => "RemovePackages";

        public string MarketGroupId { get; set; } = null;
    }
}
