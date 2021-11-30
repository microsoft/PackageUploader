// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net.Mime;

namespace PackageUploader.ClientApi.Client.Ingestion
{
    internal static class IngestionExtensions
    {
        public static IServiceCollection AddIngestionService(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<IngestionConfig>().Bind(config.GetSection(nameof(IngestionConfig))).ValidateDataAnnotations();
            services.AddScoped<IngestionAuthenticationDelegatingHandler>();
            services.AddHttpClient<IIngestionHttpClient, IngestionHttpClient>((serviceProvider, httpClient) =>
                {
                    httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);

                    var ingestionConfig = serviceProvider.GetRequiredService<IOptions<IngestionConfig>>().Value;
                    httpClient.BaseAddress = new Uri(ingestionConfig.BaseAddress);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(ingestionConfig.HttpTimeoutMs);
                }).AddHttpMessageHandler<IngestionAuthenticationDelegatingHandler>();

            return services;
        }
    }
}
