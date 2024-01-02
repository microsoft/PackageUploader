// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class GetProductOperationValidator : IValidateOptions<GetProductOperationConfig>;

internal class GetProductOperationConfig : BaseOperationConfig
{
    internal override string GetOperationName() => "GetProduct";
}