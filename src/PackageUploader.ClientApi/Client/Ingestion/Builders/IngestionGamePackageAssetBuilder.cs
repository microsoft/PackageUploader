// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Extensions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.IO;

namespace PackageUploader.ClientApi.Client.Ingestion.Builders;

internal class IngestionGamePackageAssetBuilder : IBuilder<IngestionGamePackageAsset>
{
    private readonly string _packageId;
    private readonly FileInfo _fileInfo;
    private readonly GamePackageAssetType _packageAssetType;
    private const string ResourceType = "PackageAsset";

    public IngestionGamePackageAssetBuilder(string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType)
    {
        _packageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
        _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        _packageAssetType = packageAssetType;
    }

    public IngestionGamePackageAsset Build() =>
        new()
        {
            PackageId = _packageId,
            Type = _packageAssetType.GetGamePackageAssetType(),
            ResourceType = ResourceType,
            FileName = _fileInfo.Name,
            BinarySizeInBytes = _fileInfo.Length,
            CreatedDate = _fileInfo.CreationTime,
            Name = _fileInfo.Name,
        };
}