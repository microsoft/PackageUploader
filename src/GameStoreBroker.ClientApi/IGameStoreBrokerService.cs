// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct);
        Task<GamePackageConfiguration> GetPackageConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, CancellationToken ct);
        Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(GameProduct product, GamePackageConfiguration packageConfiguration, CancellationToken ct);
        Task<GamePackage> UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct);
        Task<GamePackageConfiguration> RemovePackagesAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, CancellationToken ct);
        Task<GamePackageConfiguration> SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupId, GamePackageDate availabilityDate, CancellationToken ct);
        Task<GamePackageConfiguration> SetUwpConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, IGameConfiguration gameConfiguration, CancellationToken ct);
        Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupId, bool overwrite, CancellationToken ct);
        Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupId, bool overwrite, IGameConfiguration gameConfiguration, CancellationToken ct);
    }
}
