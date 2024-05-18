// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.IO;
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

    public static Option<T> Required<T>(this Option<T> option, bool required = true)
    {
        option.IsRequired = required;
        return option;
    }

    public static IServiceCollection AddOperation<T1, T2>(this IServiceCollection services, HostBuilderContext context) where T1 : Operation where T2 : BaseOperationConfig
    {
        services.AddScoped<T1>();
        services.AddOptions<T2>().Bind(context.Configuration).ValidateDataAnnotations();
        return services;
    }

    public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, FileInfo configFile, Program.ConfigFileFormat configFileFormat) =>
        configFileFormat switch
        {
            Program.ConfigFileFormat.Json => builder.AddJsonFile(configFile.FullName, false, false),
            Program.ConfigFileFormat.Xml => builder.AddXmlFile(configFile.FullName, false, false),
            Program.ConfigFileFormat.Ini => builder.AddIniFile(configFile.FullName, false, false),
            _ => throw new ArgumentOutOfRangeException(nameof(configFileFormat), "Invalid config file format."),
        };
}