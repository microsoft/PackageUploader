// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Extensions
{
    internal static class GameStoreBrokerExtensions
    {
        public static async Task<GameProduct> GetProductAsync(this IGameStoreBrokerService storeBroker, BaseOperationSchema schema, CancellationToken ct)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema), $"{nameof(schema)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(schema.BigId))
            {
                return await storeBroker.GetProductByBigIdAsync(schema.BigId, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(schema.ProductId))
            {
                return await storeBroker.GetProductByProductIdAsync(schema.ProductId, ct).ConfigureAwait(false);
            }

            throw new Exception("BigId or ProductId needed.");
        }

        public static async Task<GamePackageBranch> GetGamePackageBranch(this IGameStoreBrokerService storeBroker, GameProduct product, PackageBranchOperationSchema schema, CancellationToken ct)
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
                return await storeBroker.GetPackageBranchByFriendlyNameAsync(product, schema.BranchFriendlyName, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(schema.FlightName))
            {
                return await storeBroker.GetPackageBranchByFlightNameAsync(product, schema.FlightName, ct).ConfigureAwait(false);
            }

            throw new Exception("BranchFriendlyName or FlightName needed.");
        }
    }
}
