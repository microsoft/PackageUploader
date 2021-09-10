// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Config;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    internal static class AccessTokenProviderExtensions
    {
        public static void AddAccessTokenProvider(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<AadAuthInfo>().Bind(config.GetSection(nameof(AadAuthInfo))).ValidateDataAnnotations();
            services.AddOptions<AccessTokenProviderConfig>().Bind(config.GetSection(nameof(AccessTokenProviderConfig))).ValidateDataAnnotations();
            services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
        }
    }
}