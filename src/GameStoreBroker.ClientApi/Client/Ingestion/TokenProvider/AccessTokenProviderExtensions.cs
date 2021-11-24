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
        public static IServiceCollection AddAzureApplicationSecretAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
            services.AddAccessTokenProvider<AzureApplicationSecretAccessTokenProvider, AadAuthInfo>(config);

        public static IServiceCollection AddDefaultAzureCredentialAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
            services.AddAccessTokenProvider<DefaultAzureCredentialAccessTokenProvider>(config);

        public static IServiceCollection AddInteractiveBrowserCredentialAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
            services.AddAccessTokenProvider<InteractiveBrowserCredentialAccessTokenProvider>(config);

        private static IServiceCollection AddAccessTokenProvider<TProvider, TAuthInfo>(this IServiceCollection services, IConfiguration config)
            where TProvider : class, IAccessTokenProvider where TAuthInfo : class
        {
            services.AddOptions<TAuthInfo>().Bind(config.GetSection("AadAuthInfo")).ValidateDataAnnotations();
            services.AddAccessTokenProvider<TProvider>(config);
            return services;
        }

        private static IServiceCollection AddAccessTokenProvider<TProvider>(this IServiceCollection services, IConfiguration config)
            where TProvider : class, IAccessTokenProvider
        {
            services.AddOptions<AccessTokenProviderConfig>().Bind(config.GetSection(nameof(AccessTokenProviderConfig))).ValidateDataAnnotations();
            services.AddScoped<IAccessTokenProvider, TProvider>();
            return services;
        }
    }
}
