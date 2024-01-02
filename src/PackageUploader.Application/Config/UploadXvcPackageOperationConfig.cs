// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Models;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadXvcPackageOperationValidator : IValidateOptions<UploadXvcPackageOperationConfig>;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    [ValidateObjectMembers]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;
}