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
        private static GameSubmissionState GetGameSubmissionState(this IngestionSubmission ingestionSubmission) =>
            ingestionSubmission is null ? default :
            (ingestionSubmission.PendingUpdateInfo.Status.ToLower(), ingestionSubmission.State.ToLower(), ingestionSubmission.Substate.ToLower()) switch
            {
                ("failed", _, _) => GameSubmissionState.Failed,
                ("running", _, _) => GameSubmissionState.InProgress,
                ("completed", "published", _) => GameSubmissionState.Published,
                ("completed", "inprogress", "indraft") => GameSubmissionState.InProgress,
                ("completed", "inprogress", "submitted") => GameSubmissionState.Published, // not sure
                ("completed", "inprogress", "failed") => GameSubmissionState.Failed,
                ("completed", "inprogress", "failedincertification") => GameSubmissionState.Failed,
                ("completed", "inprogress", "readytopublish") => GameSubmissionState.Published, // not sure
                ("completed", "inprogress", "publishing") => GameSubmissionState.InProgress,
                ("completed", "inprogress", "published") => GameSubmissionState.Published,
                ("completed", "inprogress", "instore") => GameSubmissionState.Published,
                _ => GameSubmissionState.NotStarted
            };

        public static GameSubmission Map(this IngestionSubmission ingestionSubmission) =>
            ingestionSubmission is null ? null : new()
            {
                Id = ingestionSubmission.Id,
                FriendlyName = ingestionSubmission.FriendlyName,
                PublishedTimeInUtc = ingestionSubmission.PublishedTimeInUtc,
                ReleaseNumber = ingestionSubmission.ReleaseNumber,
                GameSubmissionState = ingestionSubmission.GetGameSubmissionState(),
            };

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
                ETag = ingestionGamePackage.ETag,
                ODataETag = ingestionGamePackage.ODataETag,
            };

        private static GamePackageState GetState(this IngestionGamePackage ingestionGamePackage) =>
            ingestionGamePackage?.State is null ? default : GetEnum<GamePackageState>(ingestionGamePackage.State);

        private static TEnum GetEnum<TEnum>(string value) where TEnum : struct =>
            value is null ? default : Enum.TryParse(value, true, out TEnum result) ? result : default;

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
                ETag = gamePackage.ETag,
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
                ETag = ingestionGamePackageAsset.ETag,
                ODataETag = ingestionGamePackageAsset.ODataETag,
            };

        public static GamePackageConfiguration Map(this IngestionGamePackageConfiguration ingestionGamePackageConfiguration) =>
            ingestionGamePackageConfiguration is null ? null : new()
            {
                MarketGroupPackages = ingestionGamePackageConfiguration.MarketGroupPackages?.Select(x => x.Map()).ToList(),
                GradualRolloutInfo = ingestionGamePackageConfiguration.GradualRolloutInfo.Map(),
                Id = ingestionGamePackageConfiguration.Id,
                BranchName = ingestionGamePackageConfiguration.BranchName,
                BranchId = ingestionGamePackageConfiguration.BranchId,
                CreatedDate = ingestionGamePackageConfiguration.CreatedDate,
                ModifiedDate = ingestionGamePackageConfiguration.ModifiedDate,
                ETag = ingestionGamePackageConfiguration.ETag,
                ODataETag = ingestionGamePackageConfiguration.ODataETag,
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

        private static GameGradualRolloutInfo Map(this IngestionGradualRolloutInfo ingestionGradualRolloutInfo) =>
            ingestionGradualRolloutInfo is null ? null : new()
            {
                IsEnabled = ingestionGradualRolloutInfo.IsEnabled,
                IsSeekEnabled = ingestionGradualRolloutInfo.IsSeekEnabled,
                Percentage = ingestionGradualRolloutInfo.Percentage,
            };

        public static IngestionGamePackageConfiguration Merge(this IngestionGamePackageConfiguration ingestionGamePackageConfiguration, GamePackageConfiguration gamePackageConfiguration)
        {
            if (gamePackageConfiguration is not null)
            {
                if (!string.Equals(ingestionGamePackageConfiguration.Id, gamePackageConfiguration.Id))
                {
                    throw new IngestionClientException("Error trying to merge GamePackageConfiguration. Id is not the same.");
                }
                ingestionGamePackageConfiguration.MarketGroupPackages = gamePackageConfiguration.MarketGroupPackages?.Select(x => x.Map()).ToList();
                ingestionGamePackageConfiguration.GradualRolloutInfo = gamePackageConfiguration.GradualRolloutInfo.Map();
            }
            return ingestionGamePackageConfiguration;
        }

        private static IngestionMarketGroupPackage Map(this GameMarketGroupPackage gameMarketGroupPackage) =>
            gameMarketGroupPackage is null ? null :new()
            {
                AvailabilityDate = gameMarketGroupPackage.AvailabilityDate?.ToUniversalTime(),
                MandatoryUpdateInfo = gameMarketGroupPackage.MandatoryUpdateInfo.Map(),
                MarketGroupId = gameMarketGroupPackage.MarketGroupId,
                Markets = gameMarketGroupPackage.Markets,
                Name = gameMarketGroupPackage.Name,
                PackageAvailabilityDates = gameMarketGroupPackage.PackageAvailabilityDates?.ToDictionary(a => a.Key, a => a.Value?.ToUniversalTime()),
                PackageIds = gameMarketGroupPackage.PackageIds,
            };

        private static IngestionMandatoryUpdateInfo Map(this GameMandatoryUpdateInfo gameMandatoryUpdateInfo) =>
            gameMandatoryUpdateInfo is null ? null : new ()
            {
                MandatoryVersion = gameMandatoryUpdateInfo.MandatoryVersion,
                EffectiveDate = gameMandatoryUpdateInfo.EffectiveDate?.ToUniversalTime(),
                IsEnabled = gameMandatoryUpdateInfo.IsEnabled,
            };

        private static IngestionGradualRolloutInfo Map(this GameGradualRolloutInfo gameGradualRolloutInfo) =>
            gameGradualRolloutInfo is null ? null : new()
            {
                IsEnabled = gameGradualRolloutInfo.IsEnabled,
                IsSeekEnabled = gameGradualRolloutInfo.IsSeekEnabled,
                Percentage = gameGradualRolloutInfo.Percentage,
            };

        public static GameSubmissionValidationItem Map(this IngestionSubmissionValidationItem ingestionSubmissionValidationItem) =>
            ingestionSubmissionValidationItem is null ? null : new()
            {
                ErrorCode = ingestionSubmissionValidationItem.ErrorCode,
                Message = ingestionSubmissionValidationItem.Message,
                Resource = ingestionSubmissionValidationItem.Resource,
                Severity = ingestionSubmissionValidationItem.Severity,
            };
    }
}
