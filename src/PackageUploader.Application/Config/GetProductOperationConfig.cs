// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class GetProductOperationValidator : IValidateOptions<GetProductOperationConfig>;

internal class GetProductOperationConfig : BaseOperationConfig, IValidatableObject
{
    internal override string GetOperationName() => "GetProduct";

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return base.Validate(validationContext);
    }
}