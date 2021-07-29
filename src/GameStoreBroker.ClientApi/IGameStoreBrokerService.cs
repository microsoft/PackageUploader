// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigIdAsync(AadAuthInfo aadAuthInfo, string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(AadAuthInfo aadAuthInfo, string productId, CancellationToken ct);
    }
}