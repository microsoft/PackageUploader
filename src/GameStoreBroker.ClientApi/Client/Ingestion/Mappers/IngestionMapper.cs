// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;
using GameStoreBroker.ClientApi.Client.Xfus.Models;
using System;
using System.Linq;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Mappers
{
    internal static class IngestionMapper
    {
        public static GameProduct Map(this IngestionGameProduct ingestionGameProduct) =>
            new()
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds?.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value,
            };

        public static GamePackage Map(this IngestionGamePackage ingestionGamePackage) =>
            new()
            {
                Id = ingestionGamePackage.Id,
                State = ingestionGamePackage.GetState(),
                UploadInfo = ingestionGamePackage.UploadInfo.Map(),
                ODataETag = ingestionGamePackage.ODataETag,
            };

        public static GamePackageState GetState(this IngestionGamePackage ingestionGamePackage) =>
            Enum.TryParse(ingestionGamePackage.State, true, out GamePackageState gamePackageState)
                ? gamePackageState
                : GamePackageState.Unknown;

        public static XfusUploadInfo Map(this IngestionXfusUploadInfo ingestionXfusUploadInfo) =>
            new()
            {
                XfusId = new Guid(ingestionXfusUploadInfo.XfusId),
                Token = ingestionXfusUploadInfo.Token,
                UploadDomain = ingestionXfusUploadInfo.UploadDomain,
                XfusTenant = ingestionXfusUploadInfo.XfusTenant,
            };

        public static GamePackageBranch Map(this IngestionBranch ingestionBranch) =>
            new()
            {
                Id = ingestionBranch.Id,
                Name = ingestionBranch.FriendlyName,
                CurrentDraftInstanceId = ingestionBranch.CurrentDraftInstanceId,
            };

        public static GamePackageAsset Map(this IngestionGamePackageAsset ingestionGamePackageAsset) =>
            new()
            {
                Id = ingestionGamePackageAsset.Id,
                Type = ingestionGamePackageAsset.Type,
                Name = ingestionGamePackageAsset.Name,
                IsCommitted = ingestionGamePackageAsset.IsCommitted,
                PackageId = ingestionGamePackageAsset.PackageId,
                PackageType = ingestionGamePackageAsset.PackageType,
                CreatedDate = ingestionGamePackageAsset.CreatedDate,
                BinarySizeInBytes = ingestionGamePackageAsset.BinarySizeInBytes,
                UploadInfo = ingestionGamePackageAsset.UploadInfo.Map(),
                FileName = ingestionGamePackageAsset.FileName,
            };

        public static GamePackageConfiguration Map(this IngestionPackageSet ingestionPackageSet) =>
            new()
            {
                MarketGroupPackages = ingestionPackageSet.MarketGroupPackages?.Select(x => x.Map()).ToList(),
                ODataETag = ingestionPackageSet.ODataETag,
                Id = ingestionPackageSet.Id,
                BranchName = ingestionPackageSet.BranchName,
                BranchId = ingestionPackageSet.BranchId,
                CreatedDate = ingestionPackageSet.CreatedDate,
                ModifiedDate = ingestionPackageSet.ModifiedDate,
            };

        private static GameMarketGroupPackage Map(this IngestionMarketGroupPackage ingestionMarketGroupPackage)
        {
            if (ingestionMarketGroupPackage is null)
                return null;

            return new GameMarketGroupPackage
            {
                MarketGroupId = ingestionMarketGroupPackage.MarketGroupId,
                Name = ingestionMarketGroupPackage.Name,
                Markets = ingestionMarketGroupPackage.Markets,
                PackageIds = ingestionMarketGroupPackage.PackageIds,
                MandatoryUpdateInfo = ingestionMarketGroupPackage.MandatoryUpdateInfo.Map(),
                AvailabilityDate = ingestionMarketGroupPackage.AvailabilityDate,
                PackageAvailabilityDates = ingestionMarketGroupPackage.PackageAvailabilityDates,
            };
        }

        private static GameMandatoryUpdateInfo Map(this IngestionMandatoryUpdateInfo ingestionMandatoryUpdateInfo)
        {
            if (ingestionMandatoryUpdateInfo is null)
                return null;

            return new GameMandatoryUpdateInfo
            {
                IsEnabled = ingestionMandatoryUpdateInfo.IsEnabled,
                MandatoryVersion = ingestionMandatoryUpdateInfo.MandatoryVersion,
                EffectiveDate = ingestionMandatoryUpdateInfo.EffectiveDate,
            };
        }

        public static IngestionPackageSet Merge(this IngestionPackageSet ingestionPackageSet, GamePackageConfiguration gamePackageConfiguration)
        {
            ingestionPackageSet.MarketGroupPackages = gamePackageConfiguration.MarketGroupPackages?.Select(x => x.Map()).ToList();
            ingestionPackageSet.ETag = gamePackageConfiguration.ODataETag;
            return ingestionPackageSet;
        }

        private static IngestionMarketGroupPackage Map(this GameMarketGroupPackage gameMarketGroupPackage)
        {
            if (gameMarketGroupPackage is null)
                return null;

            return new IngestionMarketGroupPackage
            {
                AvailabilityDate = gameMarketGroupPackage.AvailabilityDate,
                MandatoryUpdateInfo = gameMarketGroupPackage.MandatoryUpdateInfo.Map(),
                MarketGroupId = gameMarketGroupPackage.MarketGroupId,
                Markets = gameMarketGroupPackage.Markets,
                Name = gameMarketGroupPackage.Name,
                PackageAvailabilityDates = gameMarketGroupPackage.PackageAvailabilityDates,
                PackageIds = gameMarketGroupPackage.PackageIds,
            };
        }

        private static IngestionMandatoryUpdateInfo Map(this GameMandatoryUpdateInfo gameMandatoryUpdateInfo)
        {
            if (gameMandatoryUpdateInfo is null)
                return null;

            return new IngestionMandatoryUpdateInfo
            {
                MandatoryVersion = gameMandatoryUpdateInfo.MandatoryVersion,
                EffectiveDate = gameMandatoryUpdateInfo.EffectiveDate,
                IsEnabled = gameMandatoryUpdateInfo.IsEnabled,
            };
        }
    }
}
