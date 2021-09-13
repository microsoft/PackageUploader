// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Schema
{
    internal class RemovePackagesOperationSchema : PackageBranchOperationSchema
    {
        protected override string GetOperationName() => "RemovePackages";

        public string MarketGroupId { get; set; } = null;
    }
}
