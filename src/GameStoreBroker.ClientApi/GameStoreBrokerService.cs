// Copyright (C) Microsoft. All rights reserved.

using GameStoreBroker.Api;
using GameStoreBroker.ClientApi.ExternalModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    internal class GameStoreBrokerService : IGameStoreBrokerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameStoreBrokerService> _logger;

        public GameStoreBrokerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<GameStoreBrokerService>>();
        }

        public async Task<GameProduct> GetProductByBigId(AadAuthInfo aadAuthInfo, string bigId)
        {
            if (string.IsNullOrEmpty(bigId))
                throw new ArgumentNullException(nameof(bigId));

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IngestionHttpClient>();
            await ingestionHttpClient.Authorize(aadAuthInfo).ConfigureAwait(false);

            return await ingestionHttpClient.GetGameProductByBigIdAsync(bigId);
        }

        public async Task<GameProduct> GetProductByProductId(AadAuthInfo aadAuthInfo, string productId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentNullException(nameof(productId));

            var ingestionHttpClient = _serviceProvider.GetRequiredService<IngestionHttpClient>();
            await ingestionHttpClient.Authorize(aadAuthInfo).ConfigureAwait(false);
            
            return await ingestionHttpClient.GetGameProductByLongIdAsync(productId);
        }
    }
}