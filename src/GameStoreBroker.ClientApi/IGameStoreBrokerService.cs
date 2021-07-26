// Copyright (C) Microsoft. All rights reserved.

using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.ClientApi
{
    public interface IGameStoreBrokerService
    {
        Task<GameProduct> GetProductByBigId(AadAuthInfo aadAuthInfo, string bigId);
        Task<GameProduct> GetProductByProductId(AadAuthInfo aadAuthInfo, string productId);
    }
}