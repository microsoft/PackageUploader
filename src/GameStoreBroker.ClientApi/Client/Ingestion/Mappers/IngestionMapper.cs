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
        public static GameProduct Map(this IngestionGameProduct ingestionGameProduct)
        {
            return new GameProduct
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds?.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value,
            };
        }

        public static GamePackage Map(this IngestionGamePackage ingestionGamePackage)
        {
            return new GamePackage
            {
                Id = ingestionGamePackage.Id,
                State = ingestionGamePackage.State,
                UploadInfo = ingestionGamePackage.UploadInfo.Map(),
            };
        }

        public static XfusUploadInfo Map(this IngestionXfusUploadInfo ingestionXfusUploadInfo)
        {
            return new XfusUploadInfo
            {
                FileName = ingestionXfusUploadInfo.FileName,
                XfusId = ingestionXfusUploadInfo.XfusId,
                FileSasUri = ingestionXfusUploadInfo.FileSasUri,
                Token = ingestionXfusUploadInfo.Token,
                UploadDomain = ingestionXfusUploadInfo.UploadDomain,
                XfusTenant = ingestionXfusUploadInfo.XfusTenant,
            };
        }

        public static GamePackageBranch Map(this IngestionBranch ingestionBranch)
        {
            return new GamePackageBranch
            {
                Id = ingestionBranch.Id,
                Name = ingestionBranch.FriendlyName,
                CurrentDraftInstanceId = ingestionBranch.CurrentDraftInstanceId,
            };
        }

        public static GamePackageAsset Map(this IngestionGamePackageAsset ingestionGamePackageAsset)
        {
            return new GamePackageAsset
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
        }
    }
}
