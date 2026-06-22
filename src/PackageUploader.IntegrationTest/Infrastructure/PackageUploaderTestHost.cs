// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>Composes the real <see cref="IPackageUploaderService"/> with the Ingestion network handler and access-token provider replaced by test doubles.</summary>
internal sealed class PackageUploaderTestHost : IDisposable
{
    // Logical name HttpClientFactory assigns to AddHttpClient<IIngestionHttpClient, IngestionHttpClient>().
    // Configuring the primary handler by this name overrides the production one without needing access
    // to the internal IIngestionHttpClient/IngestionHttpClient types.
    private const string IngestionHttpClientName = "IIngestionHttpClient";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public MockHttpMessageHandler IngestionHandler { get; }

    public IPackageUploaderService Service { get; }

    public PackageUploaderTestHost(
        Action<MockHttpMessageHandler>? configureIngestion = null,
        string ingestionBaseAddress = "https://ingestion.test.local/")
    {
        IngestionHandler = new MockHttpMessageHandler();
        configureIngestion?.Invoke(IngestionHandler);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IngestionConfig:BaseAddress"] = ingestionBaseAddress,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        services.AddPackageUploaderService(IngestionExtensions.AuthenticationMethod.Default);

        services.RemoveAll<IAccessTokenProvider>();
        services.AddScoped<IAccessTokenProvider, FakeAccessTokenProvider>();

        services.AddHttpClient(IngestionHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => IngestionHandler);

        // IPackageUploaderService and the Ingestion auth handler are scoped; resolve them from an
        // explicit scope (with scope validation on) rather than the root provider.
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
        Service = _scope.ServiceProvider.GetRequiredService<IPackageUploaderService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
