// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Xfus.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus;

internal static class XfusExtensions
{
    public static void AddXfusService(this IServiceCollection services, IConfiguration config)
    {
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
                MaxConnectionsPerServer = uploadConfig.DefaultConnectionLimit < 0 ? 12 * Environment.ProcessorCount : uploadConfig.DefaultConnectionLimit,
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