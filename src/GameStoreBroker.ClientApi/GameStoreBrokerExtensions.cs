// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Xfus;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Mime;

namespace GameStoreBroker.ClientApi
{
    public static class GameStoreBrokerExtensions
    {
        public static void AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<AadAuthInfo>().Bind(config.GetSection(nameof(AadAuthInfo))).ValidateDataAnnotations();
            services.AddHttpClient<IIngestionHttpClient, IngestionHttpClient>(httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://api.partner.microsoft.com/v1.0/ingestion/");
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
            });
            services.AddHttpClient<IXfusHttpClient, XfusHttpClient>();
            services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
        }
    }
}