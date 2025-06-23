// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi;
using PackageUploader.FileLogger;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace PackageUploader.Application;

internal class Program
{
    private const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

    private static async Task<int> Main(string[] args)
    {
        return await BuildCommandLine()
            .UseHost(hostBuilder => hostBuilder
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices(ConfigureServices)
                .ConfigureAppConfiguration((context, builder) => ConfigureAppConfiguration(context, builder, args))
            )
            .UseDefaults()
            .Build()
            .InvokeAsync(args)
            .ConfigureAwait(false);
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logging)
    {
        var invocationContext = context.GetInvocationContext();
        var isData = invocationContext.GetOptionValue(ParameterHelper.DataOption);
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Error);
        logging.AddFilter("PackageUploader",
            isData ? LogLevel.Error :
            invocationContext.GetOptionValue(ParameterHelper.VerboseOption) ? LogLevel.Trace : LogLevel.Information);
        logging.AddFilter<FileLoggerProvider>("PackageUploader", LogLevel.Trace);
        logging.AddSimpleFile(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = LogTimestampFormat;
        }, file =>
        {
            var logFile = invocationContext.GetOptionValue(ParameterHelper.LogFileOption);
            file.Path = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"PackageUploader_{DateTime.Now:yyyyMMddHHmmss}.log");
            file.Append = true;
        });
        if (isData)
        {
            logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Error);
        }
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = LogTimestampFormat;
        });
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var invocationContext = context.GetInvocationContext();

        services.AddLogging();
        services.AddPackageUploaderService(invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption));
        services.AddOperations(context);
    }

    private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder, string[] args)
    {
        var invocationContext = context.GetInvocationContext();

        var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
        var authenticationMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

        ParameterHelper.ConfigureParameters(configFile, authenticationMethod, builder, args);
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        return ParameterHelper.BuildCommandLine();
    }
}