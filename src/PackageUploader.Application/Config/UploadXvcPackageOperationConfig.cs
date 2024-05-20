// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Config;

internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
{
    internal override string GetOperationName() => "UploadXvcPackage";

    [Required]
    public GameAssets GameAssets { get; set; }

    public bool DeltaUpload { get; set; } = false;

    public IDictionary<string, object> PackageMetadata { get; set; }

    // While reading configuration, jsonextensiondata is not supported, thus used the below method to get the metadata
    public MarketGroupPackageMetadata GetMarketGroupPackageMetadata()
    {
        return JsonSerializer.Deserialize<MarketGroupPackageMetadata>(JsonSerializer.Serialize(PackageMetadata));
    }
}