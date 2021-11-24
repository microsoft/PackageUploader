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
        public enum AuthenticationMethod 
        {
            AzureApplicationSecret, 
            DefaultAzureCredential, 
            InteractiveBrowserCredential,
        }

        public static IServiceCollection AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config, 
            AuthenticationMethod authenticationMethod = AuthenticationMethod.AzureApplicationSecret)
        {
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
            services.AddIngestionService(config);
            services.AddIngestionAuthentication(config, authenticationMethod);
            services.AddXfusService(config);

            return services;
        }

        private static IServiceCollection AddIngestionAuthentication(this IServiceCollection services, IConfiguration config, 
            AuthenticationMethod authenticationMethod = AuthenticationMethod.AzureApplicationSecret) =>
            authenticationMethod switch
            {
                AuthenticationMethod.AzureApplicationSecret => services.AddAzureApplicationSecretAccessTokenProvider(config),
                AuthenticationMethod.InteractiveBrowserCredential => services.AddInteractiveBrowserCredentialAccessTokenProvider(config),
                AuthenticationMethod.DefaultAzureCredential => services.AddDefaultAzureCredentialAccessTokenProvider(config),
                _ => services.AddAzureApplicationSecretAccessTokenProvider(config),
            };
    }
}
