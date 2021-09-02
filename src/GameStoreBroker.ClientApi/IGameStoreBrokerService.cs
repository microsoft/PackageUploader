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
        Task<GameProduct> GetProductByBigIdAsync(IAccessTokenProvider accessTokenProvider, string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(IAccessTokenProvider accessTokenProvider, string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(IAccessTokenProvider accessTokenProvider, GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightName(IAccessTokenProvider accessTokenProvider, GameProduct product, string flightName, CancellationToken ct);
        Task UploadUwpPackageAsync(IAccessTokenProvider accessTokenProvider, GameProduct product, GamePackageBranch packageBranch, FileInfo packageFile, CancellationToken ct);
    }
}