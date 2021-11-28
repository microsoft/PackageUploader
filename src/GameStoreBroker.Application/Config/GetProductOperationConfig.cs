// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.Application.Config
{
    internal class GetProductOperationConfig : BaseOperationConfig
    {
        internal override Operations.OperationName GetOperationName() => Operations.OperationName.GetProduct;
    }
}
