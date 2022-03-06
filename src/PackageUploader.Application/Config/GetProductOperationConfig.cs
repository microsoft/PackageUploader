// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Config;

internal class GetProductOperationConfig : BaseOperationConfig
{
    internal override string GetOperationName() => "GetProduct";
    public bool WithBranches { get; set; }
}