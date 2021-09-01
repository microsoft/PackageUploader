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
    }
}
