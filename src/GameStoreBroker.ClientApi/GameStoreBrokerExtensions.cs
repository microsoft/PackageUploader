// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider;
using GameStoreBroker.ClientApi.Client.Xfus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStoreBroker.ClientApi
{
    public static class IngestionExtensions
    {
        public static void AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
            services.AddIngestionService(config);
            services.AddAccessTokenProvider(config);
            services.AddXfusService(config);
        }
    }
}