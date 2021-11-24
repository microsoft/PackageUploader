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
            AppSecret,
            AppCert,
            Default, 
            Browser,
        }

        public static IServiceCollection AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config, 
            AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret)
        {
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();
            services.AddIngestionService(config);
            services.AddIngestionAuthentication(config, authenticationMethod);
            services.AddXfusService(config);

            return services;
        }

        private static IServiceCollection AddIngestionAuthentication(this IServiceCollection services, IConfiguration config, 
            AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret) =>
            authenticationMethod switch
            {
                AuthenticationMethod.AppSecret => services.AddAzureApplicationSecretAccessTokenProvider(config),
                AuthenticationMethod.AppCert => services.AddAzureApplicationCertificateAccessTokenProvider(config),
                AuthenticationMethod.Browser => services.AddInteractiveBrowserCredentialAccessTokenProvider(config),
                AuthenticationMethod.Default => services.AddDefaultAzureCredentialAccessTokenProvider(config),
                _ => services.AddAzureApplicationSecretAccessTokenProvider(config),
            };
    }
}
