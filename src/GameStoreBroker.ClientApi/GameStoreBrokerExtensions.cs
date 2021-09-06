// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Ingestion.Config;
using GameStoreBroker.ClientApi.Client.Xfus;
using GameStoreBroker.ClientApi.Client.Xfus.Config;
using GameStoreBroker.ClientApi.TokenProvider;
using GameStoreBroker.ClientApi.TokenProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

                // Disable the extra handshake on POST requests.
                httpClient.DefaultRequestHeaders.ExpectContinue = uploadConfig.Expect100Continue;
            }).ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var uploadConfig = serviceProvider.GetRequiredService<IOptions<UploadConfig>>().Value;
                return new SocketsHttpHandler
                {
                    // Default connection limit is 2 which is too low for this multi-threaded
                    // client, we decided to use (12 * # of cores) based on experimentation.
                    // https://docs.microsoft.com/en-gb/archive/blogs/timomta/controlling-the-number-of-outgoing-connections-from-httpclient-net-core-or-full-framework
                    MaxConnectionsPerServer = uploadConfig.DefaultConnectionLimit == -1 ? 12 * Environment.ProcessorCount : uploadConfig.DefaultConnectionLimit,
                    ConnectCallback = (context, ct) => ConnectCallback(context, uploadConfig.UseNagleAlgorithm, ct),
                };
            });
        }

        private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, bool useNagleAlgorithm, CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                // Turn on/off TCP small packet buffering (a.k.a. Nagle's algorithm)
                NoDelay = !useNagleAlgorithm,
            };

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);

                // The stream should take the ownership of the underlying socket,
                // closing it when it's disposed.
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}