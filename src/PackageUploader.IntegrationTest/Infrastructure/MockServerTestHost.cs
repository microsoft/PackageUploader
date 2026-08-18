// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.IntegrationTest.FakeApi;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>
/// Hosts a fake-API ASP.NET Core app (Ingestion + XFUS controllers) on a random loopback port via
/// Kestrel, then composes the real <see cref="IPackageUploaderService"/> pointed at it. The client
/// makes real HTTP calls over a loopback socket — its full pipeline (auth handler, Polly policies,
/// the XFUS <c>SocketsHttpHandler</c>, serialization, mappers) runs for real against the fake app.
/// Authentication uses <see cref="FakeAccessTokenProvider"/>. Uses only the ASP.NET Core shared
/// framework — no third-party package.
/// </summary>
internal sealed class MockServerTestHost : IDisposable
{
    private readonly WebApplication _app;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>The fake Ingestion API. Configure stubs before exercising the service.</summary>
    public IngestionScenarioStore Ingestion { get; }

    /// <summary>The fake XFUS upload service. Configure stubs before exercising the service.</summary>
    public XfusScenarioStore Xfus { get; }

    /// <summary>The fully composed, public service under test, wired to the fake app.</summary>
    public IPackageUploaderService Service { get; }

    /// <summary>The fake app's base URL, also used as the XFUS upload domain in package responses.</summary>
    public string XfusUploadDomain { get; }

    public MockServerTestHost()
    {
        Ingestion = new IngestionScenarioStore();
        Xfus = new XfusScenarioStore();

        _app = BuildFakeApp(Ingestion, Xfus);
        _app.StartAsync().GetAwaiter().GetResult();
        try
        {
            XfusUploadDomain = _app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.First();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["IngestionConfig:BaseAddress"] = $"{XfusUploadDomain}/",
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

            _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            _scope = _provider.CreateScope();
            Service = _scope.ServiceProvider.GetRequiredService<IPackageUploaderService>();
        }
        catch
        {
            // Avoid leaking the started Kestrel listener if composition fails.
            StopApp();
            throw;
        }
    }

    private static WebApplication BuildFakeApp(IngestionScenarioStore ingestion, XfusScenarioStore xfus)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddSingleton(ingestion);
        builder.Services.AddSingleton(xfus);
        builder.Services.AddControllers().AddApplicationPart(typeof(IngestionController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        return app;
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _provider?.Dispose();
        StopApp();
    }

    private void StopApp()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        ((IAsyncDisposable)_app).DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
