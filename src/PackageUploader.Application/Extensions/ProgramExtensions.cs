// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Extensions;

internal static class ProgramExtensions
{
    public static Command AddOperationHandler<T>(this Command command) where T : Operation
    {
        command.Handler = CommandHandler.Create(async (IHost host, CancellationToken ct) => await RunAsyncOperation<T>(host, ct).ConfigureAwait(false));
        return command;
    }

    private static async Task<int> RunAsyncOperation<T>(IHost host, CancellationToken ct) where T : Operation
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        try
        {
            var version = GetVersion();
            logger.LogDebug("PackageUploader v.{version} is starting.", version);
            return await host.Services.GetRequiredService<T>().RunAsync(ct).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError("{errorMessage}", e.Message);
            logger.LogTrace(e, "Exception thrown.");
            return 2;
        }
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        
        if (assemblyVersionAttribute is not null)
        {
            return assemblyVersionAttribute.InformationalVersion;
        }
        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }

    public static T GetOptionValue<T>(this InvocationContext invocationContext, Option<T> option)
    {
        return invocationContext.ParseResult.GetValueForOption(option);
    }

    public static void AddAliasesToSwitchMappings(this Option option, Dictionary<string, string> switchMappings, string configPath)
    {
        foreach (var alias in option.Aliases)
        {
            switchMappings[alias] = configPath;
        }
    }

    public static Option<T> Required<T>(this Option<T> option, bool required = true)
    {
        option.IsRequired = required;
        return option;
    }

    public static IServiceCollection AddOperations(this IServiceCollection services, HostBuilderContext context)
    {
        services
            .AddScoped<GetProductOperation>()
            .AddSingleton<IValidateOptions<GetProductOperationConfig>, GetProductOperationValidator>()
            .AddOptions<GetProductOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<GetPackagesOperation>()
            .AddSingleton<IValidateOptions<GetPackagesOperationConfig>, GetPackagesOperationValidator>()
            .AddOptions<GetPackagesOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<UploadUwpPackageOperation>()
            .AddSingleton<IValidateOptions<UploadUwpPackageOperationConfig>, UploadUwpPackageOperationValidator>()
            .AddOptions<UploadUwpPackageOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<UploadXvcPackageOperation>()
            .AddSingleton<IValidateOptions<UploadXvcPackageOperationConfig>, UploadXvcPackageOperationValidator>()
            .AddOptions<UploadXvcPackageOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<RemovePackagesOperation>()
            .AddSingleton<IValidateOptions<RemovePackagesOperationConfig>, RemovePackagesOperationValidator>()
            .AddOptions<RemovePackagesOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<ImportPackagesOperation>()
            .AddSingleton<IValidateOptions<ImportPackagesOperationConfig>, ImportPackagesOperationValidator>()
            .AddOptions<ImportPackagesOperationConfig>().Bind(context.Configuration);

        services
            .AddScoped<PublishPackagesOperation>()
            .AddSingleton<IValidateOptions<PublishPackagesOperationConfig>, PublishPackagesOperationValidator>()
            .AddOptions<PublishPackagesOperationConfig>().Bind(context.Configuration);

        return services;
    }
}