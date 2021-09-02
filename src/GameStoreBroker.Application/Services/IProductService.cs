// Copyright (C) Microsoft. All rights reserved.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Services
{
    internal interface IProductService
    {
        Task<GameProduct> GetProductAsync(BaseOperationSchema schema, CancellationToken ct);
        Task<GamePackageBranch> GetGamePackageBranch(GameProduct product, UploadPackageOperationSchema schema, CancellationToken ct);
    }
}