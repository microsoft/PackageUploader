// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.Config;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System;
using System.Net.Mime;

namespace PackageUploader.ClientApi.Client.Ingestion;

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
            })
            .AddHttpMessageHandler<IngestionAuthenticationDelegatingHandler>()
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var ingestionConfig = serviceProvider.GetRequiredService<IOptions<IngestionConfig>>().Value;
                var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(ingestionConfig.MedianFirstRetryDelayMs), ingestionConfig.RetryCount);
                return HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(delay);
            });

        return services;
    }
}