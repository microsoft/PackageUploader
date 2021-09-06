// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    public interface IIngestionHttpClient
    {
        Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct);
        Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightName(string productId, string flightName, CancellationToken ct);
        Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, CancellationToken ct);
    }
}