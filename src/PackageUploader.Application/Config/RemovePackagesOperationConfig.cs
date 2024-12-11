// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class RemovePackagesOperationValidator : IValidateOptions<RemovePackagesOperationConfig>;

internal class RemovePackagesOperationConfig : PackageBranchOperationConfig, IValidatableObject
{
    internal override string GetOperationName() => "RemovePackages";

    public string MarketGroupName { get; set; } = null;

    [Required] public string PackageFileName { get; set; } = null;

    public new IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return base.Validate(validationContext);
    }
}