// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Ingestion.Config;
using GameStoreBroker.ClientApi.Client.Xfus;
using GameStoreBroker.ClientApi.Client.Xfus.Config;
using GameStoreBroker.ClientApi.TokenProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace GameStoreBroker.ClientApi
{
    public static class GameStoreBrokerExtensions
    {
        public static void AddGameStoreBrokerService(this IServiceCollection services, IConfiguration config)
        {
            // Main service
            services.AddScoped<IGameStoreBrokerService, GameStoreBrokerService>();

            // Ingestion
            services.AddOptions<IngestionConfig>().Bind(config.GetSection(nameof(IngestionConfig))).ValidateDataAnnotations();
            services.AddHttpClient<IIngestionHttpClient, IngestionHttpClient>((serviceProvider, httpClient) =>
            {
                httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);

                var ingestionConfig = serviceProvider.GetRequiredService<IOptions<IngestionConfig>>().Value;
                httpClient.BaseAddress = new Uri(ingestionConfig.BaseAddress);
                httpClient.Timeout = TimeSpan.FromMilliseconds(ingestionConfig.HttpTimeoutMs);

                var accessTokenProvider = serviceProvider.GetRequiredService<IAccessTokenProvider>();
                var accessToken = accessTokenProvider.GetAccessToken().GetAwaiter().GetResult();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            });

            services.AddOptions<AadAuthInfo>().Bind(config.GetSection(nameof(AadAuthInfo))).ValidateDataAnnotations();
            services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();

            // Xfus
            services.AddOptions<UploadConfig>().Bind(config.GetSection(nameof(UploadConfig))).ValidateDataAnnotations();
            services.AddScoped<IXfusUploader, XfusUploader>();
            services.AddHttpClient(XfusUploader.HttpClientName, (serviceProvider, httpClient) =>
            {
                var uploadConfig = serviceProvider.GetRequiredService<IOptions<UploadConfig>>().Value;
                httpClient.Timeout = TimeSpan.FromMilliseconds(uploadConfig.HttpUploadTimeoutMs);
                ConfigureServicePointManager(uploadConfig);
            });
        }

        private static void ConfigureServicePointManager(UploadConfig uploadConfig)
        {
            // Default connection limit is 2 which is too low for this multi-threaded
            // client, we decided to use (12 * # of cores) based on experimentation.
            ServicePointManager.DefaultConnectionLimit = uploadConfig.DefaultConnectionLimit == -1 ? 12 * Environment.ProcessorCount : uploadConfig.DefaultConnectionLimit;

            // Disable the extra handshake on POST requests.
            ServicePointManager.Expect100Continue = uploadConfig.Expect100Continue;

            // Turn off TCP small packet buffering (a.k.a. Nagle's algorithm)
            ServicePointManager.UseNagleAlgorithm = uploadConfig.UseNagleAlgorithm;
        }
    }
}