// Copyright (C) Microsoft. All rights reserved.

using GameStoreBroker.Api;
using GameStoreBroker.ClientApi.ExternalModels;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigId(AadAuthInfo aadAuthInfo, string bigId);
        Task<GameProduct> GetProductByProductId(AadAuthInfo aadAuthInfo, string productId);
    }
}