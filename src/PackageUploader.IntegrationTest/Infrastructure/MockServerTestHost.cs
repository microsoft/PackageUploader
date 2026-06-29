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
/// Composes the real <see cref="IPackageUploaderService"/> wired to live WireMock.Net fakes of the
/// Ingestion API and XFUS. The Ingestion base address points at the <see cref="Ingestion"/> server;
/// XFUS is reached via the upload domain returned in a stubbed package response (see
/// <see cref="IngestionMockServer.StubCreatePackage"/> and <see cref="Xfus"/>). Authentication uses
/// <see cref="FakeAccessTokenProvider"/>. Everything else (auth handler, Polly policies,
/// serialization, mappers) runs for real. Dispose to stop both servers and the provider.
/// </summary>
internal sealed class MockServerTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>The fake Ingestion API. Configure stubs before exercising the service.</summary>
    public IngestionMockServer Ingestion { get; }

    /// <summary>The fake XFUS upload service. Configure stubs before exercising the service.</summary>
    public XfusMockServer Xfus { get; }

    /// <summary>The fully composed, public service under test, wired to the fakes.</summary>
    public IPackageUploaderService Service { get; }

    public MockServerTestHost()
    {
        Ingestion = new IngestionMockServer();
        Xfus = new XfusMockServer();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Trailing slash so the client's relative paths (e.g. "products/{id}") resolve under it.
                ["IngestionConfig:BaseAddress"] = $"{Ingestion.Url}/",
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

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
        Service = _scope.ServiceProvider.GetRequiredService<IPackageUploaderService>();
    }

    /// <summary>The XFUS server's base URL, for use as the upload domain in a stubbed package response.</summary>
    public string XfusUploadDomain => Xfus.Url;

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        Ingestion.Dispose();
        Xfus.Dispose();
    }
}
