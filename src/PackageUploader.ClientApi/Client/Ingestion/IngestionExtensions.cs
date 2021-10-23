// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace PackageUploader.ClientApi.Client.Ingestion
{
    internal static class IngestionExtensions
    {
        public static void AddIngestionService(this IServiceCollection services, IConfiguration config)
        {
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
        }
    }
}