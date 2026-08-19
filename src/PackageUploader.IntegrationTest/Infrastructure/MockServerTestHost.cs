// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.IntegrationTest.Infrastructure.Mocks;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>
/// Composes the real <see cref="IPackageUploaderService"/> wired to in-memory fakes of the Ingestion
/// API and XFUS. The fakes are plugged in as the primary <see cref="System.Net.Http.HttpMessageHandler"/>
/// of the Ingestion and XFUS named clients, so the real pipeline (auth handler, Polly policies,
/// serialization, mappers) runs against deterministic in-memory responses with no network and no
/// third-party dependency. Authentication uses <see cref="FakeAccessTokenProvider"/>.
/// </summary>
internal sealed class MockServerTestHost : IDisposable
{
    // Logical names HttpClientFactory assigns to the Ingestion and XFUS clients; configuring the
    // primary handler by these names overrides the production ones (real network) with the fakes.
    private const string IngestionHttpClientName = "IIngestionHttpClient";
    private const string XfusHttpClientName = "xfus";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>The fake Ingestion API. Configure stubs before exercising the service.</summary>
    public IngestionMockHandler Ingestion { get; }

    /// <summary>The fake XFUS upload service. Configure stubs before exercising the service.</summary>
    public XfusMockHandler Xfus { get; }

    /// <summary>The fully composed, public service under test, wired to the fakes.</summary>
    public IPackageUploaderService Service { get; }

    public MockServerTestHost()
    {
        Ingestion = new IngestionMockHandler();
        Xfus = new XfusMockHandler();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Host is irrelevant (the handler is overridden) but must be a valid absolute URI.
                ["IngestionConfig:BaseAddress"] = "http://ingestion.local/",
                // Keep retry/timeout fast so retry-scenario tests don't sleep on real backoffs.
                ["IngestionConfig:MedianFirstRetryDelayMs"] = "1",
                ["IngestionConfig:RetryCount"] = "3",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        services.AddPackageUploaderService(IngestionExtensions.AuthenticationMethod.Default);

        services.RemoveAll<IAccessTokenProvider>();
        services.AddScoped<IAccessTokenProvider, FakeAccessTokenProvider>();

        // Override the primary handlers with the in-memory fakes (Polly + auth handlers still run).
        services.AddHttpClient(IngestionHttpClientName).ConfigurePrimaryHttpMessageHandler(() => Ingestion);
        services.AddHttpClient(XfusHttpClientName).ConfigurePrimaryHttpMessageHandler(() => Xfus);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
        Service = _scope.ServiceProvider.GetRequiredService<IPackageUploaderService>();
    }

    /// <summary>The XFUS upload domain to embed in a stubbed package response (host is irrelevant).</summary>
    public string XfusUploadDomain => "http://xfus.local";

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
