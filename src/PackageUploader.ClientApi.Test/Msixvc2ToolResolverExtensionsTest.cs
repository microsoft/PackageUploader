// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Tools;

namespace PackageUploader.ClientApi.Test;

/// <summary>
/// Verifies that the MSIXVC2 tool resolver can be consumed from a non-UI host (for example the
/// PackageUploader.exe console app) with no UI services registered, and with or without DI at all.
/// </summary>
[TestClass]
public class Msixvc2ToolResolverExtensionsTest
{
    [TestMethod]
    public void AddMsixvc2ToolResolver_ResolvesFromABareServiceCollection_WithNoLoggingRegistered()
    {
        // A bare ServiceCollection is strictly harsher than HostApplicationBuilder, which
        // pre-registers logging. If this works, a plain host works.
        var services = new ServiceCollection();
        services.AddMsixvc2ToolResolver();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var resolver = provider.GetRequiredService<IMsixvc2ToolResolver>();

        Assert.IsNotNull(resolver);
        Assert.IsInstanceOfType<Msixvc2ToolResolver>(resolver);
        Assert.IsInstanceOfType<ProcessToolProbeRunner>(provider.GetRequiredService<IToolProbeRunner>());
    }

    [TestMethod]
    public void AddMsixvc2ToolResolver_ResolvesWhenLoggingIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMsixvc2ToolResolver();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsNotNull(provider.GetRequiredService<IMsixvc2ToolResolver>());
    }

    [TestMethod]
    public void AddMsixvc2ToolResolver_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddMsixvc2ToolResolver();
        services.AddMsixvc2ToolResolver();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IMsixvc2ToolResolver)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IToolProbeRunner)));
        Assert.IsNotNull(provider.GetRequiredService<IMsixvc2ToolResolver>());
    }

    [TestMethod]
    public void AddMsixvc2ToolResolver_HonoursAPreRegisteredProbeRunner()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IToolProbeRunner, StubProbeRunner>();
        services.AddMsixvc2ToolResolver();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsInstanceOfType<StubProbeRunner>(provider.GetRequiredService<IToolProbeRunner>());
    }

    [TestMethod]
    public void AddMsixvc2ToolResolver_RegistersTheResolverAsASingleton()
    {
        var services = new ServiceCollection();
        services.AddMsixvc2ToolResolver();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.AreSame(
            provider.GetRequiredService<IMsixvc2ToolResolver>(),
            provider.GetRequiredService<IMsixvc2ToolResolver>());
    }

    [TestMethod]
    public void Msixvc2ToolResolver_IsConstructibleWithNoDependencyInjectionAtAll()
    {
        // Console hosts that don't build a container can just new it up.
        var resolver = new Msixvc2ToolResolver();

        // Self-discovery must not throw even when no tool is installed on the machine.
        var tool = resolver.Resolve();

        Assert.AreEqual(tool is not null, resolver.IsMsixvc2Supported());
    }

    private sealed class StubProbeRunner : IToolProbeRunner
    {
        public ToolProbeResult Run(string executablePath, string arguments, TimeSpan timeout) =>
            ToolProbeResult.Failed;
    }
}
