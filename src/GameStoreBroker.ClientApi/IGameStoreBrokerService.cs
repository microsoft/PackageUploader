// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightName(GameProduct product, string flightName, CancellationToken ct);
        Task UploadUwpPackageAsync(GameProduct product, GamePackageBranch packageBranch, FileInfo packageFile, CancellationToken ct);
    }
}