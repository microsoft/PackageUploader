// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public class GameStoreBrokerService : IGameStoreBrokerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameStoreBrokerService> _logger;

        public GameStoreBrokerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<GameStoreBrokerService>>();
        }

        public async Task<GameProduct> GetProductByBigIdAsync(IAccessTokenProvider accessTokenProvider, string bigId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException($"{nameof(bigId)} cannot be null or empty.", nameof(bigId));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider, ct).ConfigureAwait(false);

            _logger.LogDebug("Requesting game product by BigId");
            return await ingestionHttpClient.GetGameProductByBigIdAsync(bigId, ct).ConfigureAwait(false);
        }

        public async Task<GameProduct> GetProductByProductIdAsync(IAccessTokenProvider accessTokenProvider, string productId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider, ct).ConfigureAwait(false);

            _logger.LogDebug("Requesting game product by ProductId");
            return await ingestionHttpClient.GetGameProductByLongIdAsync(productId, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFlightName(IAccessTokenProvider accessTokenProvider, GameProduct product, string flightName, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(flightName))
            {
                throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider, ct).ConfigureAwait(false);

            _logger.LogDebug("Requesting game package branch by flight name '{flightName}'.", flightName);
            return await ingestionHttpClient.GetPackageBranchByFlightName(product.ProductId, flightName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(IAccessTokenProvider accessTokenProvider, GameProduct product, string branchFriendlyName, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(branchFriendlyName))
            {
                throw new ArgumentException($"{nameof(branchFriendlyName)} cannot be null or empty.", nameof(branchFriendlyName));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider, ct).ConfigureAwait(false);

            _logger.LogDebug("Requesting game package branch by branch friendly name '{branchFriendlyName}'.", branchFriendlyName);
            return await ingestionHttpClient.GetPackageBranchByFriendlyNameAsync(product.ProductId, branchFriendlyName, ct).ConfigureAwait(false);
        }

        public async Task UploadUwpPackageAsync(IAccessTokenProvider accessTokenProvider, GameProduct product, GamePackageBranch packageBranch, FileInfo packageFile, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageFile is null)
            {
                throw new ArgumentNullException(nameof(packageFile), $"{nameof(packageFile)} cannot be null.");
            }

            if (!packageFile.Exists)
            {
                throw new FileNotFoundException("Package file not found.", packageFile.FullName);
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider, ct).ConfigureAwait(false);

            _logger.LogDebug("Creating game package for file '{fileName}', product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFile.Name, product.ProductId, packageBranch.CurrentDraftInstanceID);
            var package = await ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceID, packageFile.Name, ct).ConfigureAwait(false);

        }
    }
}