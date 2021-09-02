// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("GameStoreBroker.ClientApi.Test")]
namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    internal interface IIngestionHttpClient
    {
        Task Authorize(IAccessTokenProvider accessTokenProvider, CancellationToken ct);
        Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct);
        Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightName(string productId, string flightName, CancellationToken ct);
        Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, CancellationToken ct);
    }
}