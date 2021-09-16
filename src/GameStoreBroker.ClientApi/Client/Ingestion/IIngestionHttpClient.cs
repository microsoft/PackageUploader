// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    public interface IIngestionHttpClient
    {
        Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct);
        Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(string productId, string flightName, CancellationToken ct);
        Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, string marketGroupId, CancellationToken ct);
        Task<GamePackage> GetPackageByIdAsync(string productId, string packageId, CancellationToken ct);
        Task<GamePackageAsset> CreatePackageAssetRequestAsync(string productId, string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType, CancellationToken ct);
        Task<GamePackage> ProcessPackageRequestAsync(string productId, GamePackage gamePackage, CancellationToken ct);
        Task<GamePackageAsset> CommitPackageAssetAsync(string productId, string packageId, string packageAssetId, CancellationToken ct);
        Task RemovePackagesAsync(string productId, string currentDraftInstanceId, string marketGroupId, CancellationToken ct);
        Task SetAvailabilityDateXvcPackage(string productId, string currentDraftInstanceId, string marketGroupId, string packageId, DateTime? availabilityDate, CancellationToken ct);
        Task SetAvailabilityDateUwpPackage(string productId, string currentDraftInstanceId, string marketGroupId, DateTime? availabilityDate, CancellationToken ct);
        Task SetMandatoryDateUwpPackage(string productId, string currentDraftInstanceId, string marketGroupId, DateTime? mandatoryDate, CancellationToken ct);
    }
}