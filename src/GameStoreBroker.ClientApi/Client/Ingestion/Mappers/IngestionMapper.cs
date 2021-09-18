// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Exceptions;
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
            ingestionGameProduct is null ? null : new()
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds?.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value,
            };

        public static GamePackage Map(this IngestionGamePackage ingestionGamePackage) =>
            ingestionGamePackage is null ? null : new()
            {
                Id = ingestionGamePackage.Id,
                State = ingestionGamePackage.GetState(),
                UploadInfo = ingestionGamePackage.UploadInfo.Map(),
                ODataETag = ingestionGamePackage.ODataETag,
            };

        private static GamePackageState GetState(this IngestionGamePackage ingestionGamePackage) =>
            ingestionGamePackage.State is null ? GamePackageState.Unknown :
            Enum.TryParse(ingestionGamePackage.State, true, out GamePackageState gamePackageState)
                ? gamePackageState
                : GamePackageState.Unknown;

        private static XfusUploadInfo Map(this IngestionXfusUploadInfo ingestionXfusUploadInfo) =>
            ingestionXfusUploadInfo is null ? null : new()
            {
                XfusId = new Guid(ingestionXfusUploadInfo.XfusId),
                Token = ingestionXfusUploadInfo.Token,
                UploadDomain = ingestionXfusUploadInfo.UploadDomain,
                XfusTenant = ingestionXfusUploadInfo.XfusTenant,
            };

        public static IngestionGamePackage Map(this GamePackage gamePackage) =>
            gamePackage is null ? null : new()
            {
                ResourceType = "GamePackage",
                Id = gamePackage.Id,
                State = gamePackage.State.ToString(),
                UploadInfo = gamePackage.UploadInfo.Map(),
                ODataETag = gamePackage.ODataETag,
            };

        private static IngestionXfusUploadInfo Map(this XfusUploadInfo xfusUploadInfo) =>
            xfusUploadInfo is null ? null : new()
            {
                XfusId = xfusUploadInfo.XfusId.ToString(),
                Token = xfusUploadInfo.Token,
                UploadDomain = xfusUploadInfo.UploadDomain,
                XfusTenant = xfusUploadInfo.XfusTenant,
            };

        public static GamePackageBranch Map(this IngestionBranch ingestionBranch) =>
            ingestionBranch is null ? null : new()
            {
                Id = ingestionBranch.Id,
                Name = ingestionBranch.FriendlyName,
                CurrentDraftInstanceId = ingestionBranch.CurrentDraftInstanceId,
            };

        public static GamePackageAsset Map(this IngestionGamePackageAsset ingestionGamePackageAsset) =>
            ingestionGamePackageAsset is null ? null : new()
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
            ingestionPackageSet is null ? null : new()
            {
                MarketGroupPackages = ingestionPackageSet.MarketGroupPackages?.Select(x => x.Map()).ToList(),
                ODataETag = ingestionPackageSet.ODataETag,
                Id = ingestionPackageSet.Id,
                BranchName = ingestionPackageSet.BranchName,
                BranchId = ingestionPackageSet.BranchId,
                CreatedDate = ingestionPackageSet.CreatedDate,
                ModifiedDate = ingestionPackageSet.ModifiedDate,
            };

        private static GameMarketGroupPackage Map(this IngestionMarketGroupPackage ingestionMarketGroupPackage) =>
            ingestionMarketGroupPackage is null ? null : new()
            {
                MarketGroupId = ingestionMarketGroupPackage.MarketGroupId,
                Name = ingestionMarketGroupPackage.Name,
                Markets = ingestionMarketGroupPackage.Markets,
                PackageIds = ingestionMarketGroupPackage.PackageIds,
                MandatoryUpdateInfo = ingestionMarketGroupPackage.MandatoryUpdateInfo.Map(),
                AvailabilityDate = ingestionMarketGroupPackage.AvailabilityDate,
                PackageAvailabilityDates = ingestionMarketGroupPackage.PackageAvailabilityDates,
            };

        private static GameMandatoryUpdateInfo Map(this IngestionMandatoryUpdateInfo ingestionMandatoryUpdateInfo) =>
            ingestionMandatoryUpdateInfo is null ? null : new()
            {
                IsEnabled = ingestionMandatoryUpdateInfo.IsEnabled,
                MandatoryVersion = ingestionMandatoryUpdateInfo.MandatoryVersion,
                EffectiveDate = ingestionMandatoryUpdateInfo.EffectiveDate,
            };

        public static IngestionPackageSet Merge(this IngestionPackageSet ingestionPackageSet, GamePackageConfiguration gamePackageConfiguration)
        {
            if (gamePackageConfiguration is not null)
            {
                if (!string.Equals(ingestionPackageSet.Id, gamePackageConfiguration.Id))
                {
                    throw new IngestionClientException("Error trying to merge GamePackageConfiguration. Id is not the same.");
                }
                ingestionPackageSet.MarketGroupPackages = gamePackageConfiguration.MarketGroupPackages?.Select(x => x.Map()).ToList();
                ingestionPackageSet.ETag = gamePackageConfiguration.ODataETag;
            }
            return ingestionPackageSet;
        }

        private static IngestionMarketGroupPackage Map(this GameMarketGroupPackage gameMarketGroupPackage) =>
            gameMarketGroupPackage is null ? null :new()
            {
                AvailabilityDate = gameMarketGroupPackage.AvailabilityDate,
                MandatoryUpdateInfo = gameMarketGroupPackage.MandatoryUpdateInfo.Map(),
                MarketGroupId = gameMarketGroupPackage.MarketGroupId,
                Markets = gameMarketGroupPackage.Markets,
                Name = gameMarketGroupPackage.Name,
                PackageAvailabilityDates = gameMarketGroupPackage.PackageAvailabilityDates,
                PackageIds = gameMarketGroupPackage.PackageIds,
            };

        private static IngestionMandatoryUpdateInfo Map(this GameMandatoryUpdateInfo gameMandatoryUpdateInfo) =>
            gameMandatoryUpdateInfo is null ? null : new ()
            {
                MandatoryVersion = gameMandatoryUpdateInfo.MandatoryVersion,
                EffectiveDate = gameMandatoryUpdateInfo.EffectiveDate,
                IsEnabled = gameMandatoryUpdateInfo.IsEnabled,
            };
    }
}
