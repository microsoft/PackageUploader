// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
            if (bigId == null)
            {
                throw new ArgumentNullException(nameof(bigId));
            }

            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException(null, nameof(bigId));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider).ConfigureAwait(false);

            _logger.LogDebug("Requesting game product by BigId");
            return await ingestionHttpClient.GetGameProductByBigIdAsync(bigId, ct).ConfigureAwait(false);
        }

        public async Task<GameProduct> GetProductByProductIdAsync(IAccessTokenProvider accessTokenProvider, string productId, CancellationToken ct)
        {
            if (productId == null)
            {
                throw new ArgumentNullException(nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException(null, nameof(productId));
            }

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IIngestionHttpClient>();
            await ingestionHttpClient.Authorize(accessTokenProvider).ConfigureAwait(false);

            _logger.LogDebug("Requesting game product by ProductId");
            return await ingestionHttpClient.GetGameProductByLongIdAsync(productId, ct).ConfigureAwait(false);
        }
    }
}