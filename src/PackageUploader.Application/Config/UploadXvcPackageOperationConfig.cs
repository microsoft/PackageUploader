// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Config;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public MarketGroupPackageMetadata PackageMetadata { get; set; }
}