// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.ClientApi.Client.Ingestion.Extensions;

internal static class GamePackageAssetTypeExtensions
{
    public static string GetGamePackageAssetType(this GamePackageAssetType gamePackageAssetType) =>
        gamePackageAssetType switch
        {
            GamePackageAssetType.EkbFile => "EraEkb",
            GamePackageAssetType.SubmissionValidatorLog => "EraSubmissionValidatorLog",
            GamePackageAssetType.SymbolsZip => "EraSymbolFile",
            GamePackageAssetType.DiscLayoutFile => "EraSubsetFile",
            _ => string.Empty,
        };
}