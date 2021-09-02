// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class ProductService
    {
        private readonly IGameStoreBrokerService _storeBroker;
        private readonly IAccessTokenProvider _accessTokenProvider;

        public ProductService(IGameStoreBrokerService storeBroker, IAccessTokenProvider accessTokenProvider)
        {
            _storeBroker = storeBroker;
            _accessTokenProvider = accessTokenProvider;
        }

        public async Task<GameProduct> GetProductAsync(BaseOperationSchema schema, CancellationToken ct)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema), $"{nameof(schema)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(schema.BigId))
            {
                return await _storeBroker.GetProductByBigIdAsync(_accessTokenProvider, schema.BigId, ct).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(schema.ProductId))
            {
                return await _storeBroker.GetProductByProductIdAsync(_accessTokenProvider, schema.ProductId, ct).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("BigId or ProductId needed.");
            }
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
                return await _storeBroker.GetPackageBranchByFriendlyNameAsync(_accessTokenProvider, product, schema.BranchFriendlyName, ct).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(schema.FlightName))
            {
                return await _storeBroker.GetPackageBranchByFlightName(_accessTokenProvider, product, schema.FlightName, ct).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("BranchFriendlyName or FlightName needed.");
            }
        }
    }
}
