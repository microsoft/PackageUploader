// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Operations;

namespace PackageUploader.Application.Config
{
    internal class GetProductOperationConfig : BaseOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.GetProduct;
    }
}
