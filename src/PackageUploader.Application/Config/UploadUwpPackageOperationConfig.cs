// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Config;

[OptionsValidator]
internal partial class UploadUwpPackageOperationValidator : IValidateOptions<UploadUwpPackageOperationConfig>;

internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig, IGameConfiguration
{
    internal override string GetOperationName() => "UploadUwpPackage";

    public GamePackageDate MandatoryDate { get; set; }
    public GameGradualRolloutInfo GradualRollout { get; set; }
}