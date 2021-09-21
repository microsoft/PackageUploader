// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Extensions
{
    internal static class GameStoreBrokerExtensions
    {
        public static async Task<GameProduct> GetProductAsync(this IGameStoreBrokerService storeBroker, BaseOperationConfig config, CancellationToken ct)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), $"{nameof(config)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(config.BigId))
            {
                return await storeBroker.GetProductByBigIdAsync(config.BigId, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(config.ProductId))
            {
                return await storeBroker.GetProductByProductIdAsync(config.ProductId, ct).ConfigureAwait(false);
            }

            throw new Exception("BigId or ProductId needed.");
        }

        public static async Task<GamePackageBranch> GetGamePackageBranch(this IGameStoreBrokerService storeBroker, GameProduct product, PackageBranchOperationConfig config, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), $"{nameof(config)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(config.BranchFriendlyName))
            {
                return await storeBroker.GetPackageBranchByFriendlyNameAsync(product, config.BranchFriendlyName, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(config.FlightName))
            {
                return await storeBroker.GetPackageBranchByFlightNameAsync(product, config.FlightName, ct).ConfigureAwait(false);
            }

            throw new Exception("BranchFriendlyName or FlightName needed.");
        }

        public static async Task<GamePackageBranch> GetDestinationGamePackageBranch(this IGameStoreBrokerService storeBroker, GameProduct product, ImportPackagesOperationConfig config, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config), $"{nameof(config)} cannot be null.");
            }

            if (!string.IsNullOrWhiteSpace(config.DestinationBranchFriendlyName))
            {
                return await storeBroker.GetPackageBranchByFriendlyNameAsync(product, config.DestinationBranchFriendlyName, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(config.DestinationFlightName))
            {
                return await storeBroker.GetPackageBranchByFlightNameAsync(product, config.DestinationFlightName, ct).ConfigureAwait(false);
            }

            throw new Exception("DestinationBranchFriendlyName or DestinationFlightName needed.");
        }
    }
}
