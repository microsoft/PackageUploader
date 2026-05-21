// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Verifies that the Polly retry policy in XfusExtensions retries
/// TaskCanceledException thrown by HttpClient.Timeout (non-canceled outer token)
/// rather than treating it as intentional cancellation.
/// </summary>
[TestClass]
public class XfusRetryPolicyTests
{
    private const string TestClientName = "xfus-retry-test";

    /// <summary>
    /// Simulates the behavior of HttpClient.Timeout: throws TaskCanceledException
    /// with the inner exception being a TimeoutException, while the caller's
    /// CancellationToken remains non-canceled.
    /// </summary>
    private sealed class TimeoutSimulatingHandler : DelegatingHandler
    {
        public int CallCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CallCount);

            // HttpClient.Timeout throws TaskCanceledException wrapping TimeoutException.
            // The key detail: cancellationToken (the caller's token) is NOT canceled.
            throw new TaskCanceledException(
                "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.",
                new TimeoutException("A task was canceled."));
        }
    }

    /// <summary>
    /// Builds the same Polly retry policy used in XfusExtensions.AddXfusService
    /// to test it in isolation without requiring IConfiguration or the full DI graph.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> BuildXfusRetryPolicy(int retryCount)
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(1), retryCount);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => (int)response.StatusCode >= 500)
            .OrInner<TimeoutException>()
            .OrInner<TaskCanceledException>()
            .WaitAndRetryAsync(delay, (_, _, _, _) => Task.CompletedTask);
    }

    private static (IHttpClientFactory Factory, TimeoutSimulatingHandler Handler) BuildTestFactory(int retryCount)
    {
        var handler = new TimeoutSimulatingHandler();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddHttpClient(TestClientName)
            .AddPolicyHandler(BuildXfusRetryPolicy(retryCount))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IHttpClientFactory>(), handler);
    }

    [TestMethod]
    public async Task PollyRetries_WhenHttpClientTimeoutThrowsTaskCanceledException()
    {
        // Arrange
        const int retryCount = 3;
        var (factory, handler) = BuildTestFactory(retryCount);
        var httpClient = factory.CreateClient(TestClientName);

        // Act — send a request with a non-canceled token
        using var cts = new CancellationTokenSource();
        try
        {
            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/test"), cts.Token);
        }
        catch (Exception) when (!cts.Token.IsCancellationRequested)
        {
            // Expected — the final attempt throws after all retries exhausted.
            // Polly may rethrow the inner TimeoutException or the outer TaskCanceledException.
        }

        // Assert — initial attempt + retryCount retries = retryCount + 1 total calls
        Assert.AreEqual(retryCount + 1, handler.CallCount,
            $"Expected {retryCount + 1} total attempts (1 initial + {retryCount} retries), " +
            $"but got {handler.CallCount}. Polly may be treating the timeout as intentional cancellation.");
    }

    [TestMethod]
    public async Task PollyDoesNotRetry_WhenCallerCancelsToken()
    {
        // Arrange — verify that actual user cancellation is NOT retried
        var (factory, handler) = BuildTestFactory(retryCount: 3);
        var httpClient = factory.CreateClient(TestClientName);

        // Act — cancel the token before sending (simulates user Ctrl+C)
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/test"), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert — should not retry when caller intentionally canceled
        Assert.IsTrue(handler.CallCount <= 1,
            $"Expected at most 1 attempt when caller cancels, but got {handler.CallCount}. " +
            "Polly should not retry intentional cancellation.");
    }
}
