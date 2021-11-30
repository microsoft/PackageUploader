// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;

namespace GameStoreBroker.Application.Config
{
    internal class GetProductOperationConfig : BaseOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.GetProduct;
    }
}
