// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace PackageUploader.ClientApi.Tools;

public static class Msixvc2ToolResolverExtensions
{
    /// <summary>
    /// Registers the shared tool path resolver and the MSIXVC2 tool resolver. Safe to call from any
    /// host; hosts that do not use DI can simply <c>new Msixvc2ToolResolver()</c> instead.
    /// </summary>
    public static IServiceCollection AddMsixvc2ToolResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<IToolProbeRunner, ProcessToolProbeRunner>();
        services.TryAddSingleton<IToolPathResolver, ToolPathResolver>();
        services.TryAddSingleton<IMsixvc2ToolResolver>(provider => new Msixvc2ToolResolver(
            provider.GetService<ILogger<Msixvc2ToolResolver>>(),
            provider.GetRequiredService<IToolProbeRunner>(),
            probeTimeout: null,
            provider.GetRequiredService<IToolPathResolver>()));

        return services;
    }
}
