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
        public static IServiceCollection AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config, bool useDefaultAzureCredential = false)
        {
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
            services.AddIngestionService(config);
            if (useDefaultAzureCredential)
            {
                services.AddAzureAccessTokenProvider(config);
            }
            else
            {
                services.AddAccessTokenProvider(config);
            }
            services.AddXfusService(config);

            return services;
        }
    }
}