// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Services
{
    internal class ProductService : IProductService
    {
        private readonly IGameStoreBrokerService _storeBroker;

        public ProductService(IGameStoreBrokerService storeBroker)
        {
            _storeBroker = storeBroker;
        }

        public async Task<GameProduct> GetProductAsync(BaseOperationSchema schema, CancellationToken ct)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema), $"{nameof(schema)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(schema.BigId))
            {
                return await _storeBroker.GetProductByBigIdAsync(schema.BigId, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(schema.ProductId))
            {
                return await _storeBroker.GetProductByProductIdAsync(schema.ProductId, ct).ConfigureAwait(false);
            }

            throw new Exception("BigId or ProductId needed.");
        }

        public async Task<GamePackageBranch> GetGamePackageBranch(GameProduct product, UploadPackageOperationSchema schema, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema), $"{nameof(schema)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(schema.BranchFriendlyName))
            {
                return await _storeBroker.GetPackageBranchByFriendlyNameAsync(product, schema.BranchFriendlyName, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(schema.FlightName))
            {
                return await _storeBroker.GetPackageBranchByFlightName(product, schema.FlightName, ct).ConfigureAwait(false);
            }

            throw new Exception("BranchFriendlyName or FlightName needed.");
        }
    }
}
