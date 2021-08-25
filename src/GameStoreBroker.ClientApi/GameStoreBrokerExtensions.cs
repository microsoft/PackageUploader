// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Xfus;
using Microsoft.Extensions.DependencyInjection;

namespace GameStoreBroker.ClientApi
{
    public static class GameStoreBrokerExtensions
    {
        public static void AddGameStoreBrokerService(this IServiceCollection services)
        {
            services.AddHttpClient<IIngestionHttpClient, IngestionHttpClient>();
            services.AddHttpClient<IXfusHttpClient, XfusHttpClient>();
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
        }
    }
}