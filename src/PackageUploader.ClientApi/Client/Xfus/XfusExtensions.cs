// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Xfus.Config;
using PackageUploader.ClientApi.Client.Xfus.Exceptions;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus;

internal static class XfusExtensions
{
    public static IServiceCollection AddXfusService(this IServiceCollection services)
    {
        // Xfus
        services.AddSingleton<IValidateOptions<UploadConfig>, UploadConfigValidator>();
        services.AddOptions<UploadConfig>().BindConfiguration(nameof(UploadConfig));
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
        }).AddPolicyHandler((serviceProvider, _) =>
        {
            // Use exponential backoff with jitter for retries
            var uploadConfig = serviceProvider.GetRequiredService<IOptions<UploadConfig>>().Value;
            var retryCount = uploadConfig.RetryCount > 0 ? uploadConfig.RetryCount : 3;
            var delay = Backoff.ExponentialBackoff(TimeSpan.FromSeconds(2), retryCount, factor: 2);
            return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => (int)response.StatusCode >= 500)
            .OrInner<TimeoutException>()
            .OrInner<TaskCanceledException>()
            .Or<XfusServerException>(ex => ex.IsRetryable || ex.HttpStatusCode == HttpStatusCode.ServiceUnavailable || (int)ex.HttpStatusCode >= 500)
            .WaitAndRetryAsync(
                delay, (result, timeSpan, retryAttempt, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<XfusUploader>>();
                    string errorMessage;

                    if (result.Exception is XfusServerException xfusEx)
                    {
                        errorMessage = $"Status: {xfusEx.HttpStatusCode}, IsRetryable: {xfusEx.IsRetryable}, Message: {xfusEx.Message}";
                    }
                    else if (result.Exception != null)
                    {
                        errorMessage = $"Exception: {result.Exception.GetType().Name}, Message: {result.Exception.Message}";
                    }
                    else
                    {
                        errorMessage = $"Status: {result.Result?.StatusCode}, Reason: {result.Result?.ReasonPhrase ?? "Unknown"}";
                    }

                    logger.LogWarning("XFUS call failed. {ErrorDetails}. Retrying in {RetryTimeSpan:N1}s. Attempt {RetryAttempt}/{RetryCount}",
                        errorMessage, timeSpan.TotalSeconds, retryAttempt, retryCount);
                }
            );
        });

        return services;
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