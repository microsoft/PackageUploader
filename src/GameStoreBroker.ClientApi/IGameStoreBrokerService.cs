// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightName(GameProduct product, string flightName, CancellationToken ct);
        Task UploadUwpPackageAsync(GameProduct product, GamePackageBranch packageBranch, FileInfo packageFile, int minutesToWaitForProcessing, CancellationToken ct);
        Task UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct);

    }
}