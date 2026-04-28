// Copyright(c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.FileLogger;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application
{
    internal class OperationInvoker<T> : AsynchronousCommandLineAction where T : Operation
    {
        private const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken ct)
        {
            using var host = Host.CreateDefaultBuilder()
                .ConfigureLogging((_, logging) => ConfigureLogging(logging, parseResult))
                .ConfigureServices((context, services) => ConfigureServices(context, services, parseResult))
                .ConfigureAppConfiguration((_, builder) => ConfigureAppConfiguration(builder, parseResult))
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<OperationInvoker<T>>>();
            var exitCode = 0;
            try
            {
                var version = GetVersion();
                logger.LogInformation("PackageUploader v.{version} is starting.", version);
                exitCode = await host.Services.GetRequiredService<T>().RunAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError("{errorMessage}", e.Message);
                logger.LogTrace(e, "Exception thrown.");
                exitCode = 2;
            }

            Environment.ExitCode = exitCode;
            return exitCode;
        }

        private static void ConfigureLogging(ILoggingBuilder logging, ParseResult parseResult)
        {
            var isData = parseResult.GetValue(CommandLineHelper.DataOption);
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Error);
            logging.AddFilter("PackageUploader",
                isData ? LogLevel.Error :
                parseResult.GetValue(CommandLineHelper.VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            logging.AddFilter<FileLoggerProvider>("PackageUploader", LogLevel.Trace);
            logging.AddSimpleFile(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            }, file =>
            {
                var logFile = parseResult.GetValue(CommandLineHelper.LogFileOption);
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

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services, ParseResult parseResult)
        {
            var isData = parseResult.GetValue(CommandLineHelper.DataOption);

            services.AddLogging();
            services.AddSingleton(new DataOutputOptions(isData));
            services.AddPackageUploaderService(parseResult.GetValue(CommandLineHelper.AuthenticationMethodOption));

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
        }

        private static void ConfigureAppConfiguration(IConfigurationBuilder builder, ParseResult parseResult)
        {
            var configFile = parseResult.GetValue(CommandLineHelper.ConfigFileOption);
            var authenticationMethod = parseResult.GetValue(CommandLineHelper.AuthenticationMethodOption);
            
            CommandLineHelper.ConfigureParameters(configFile, authenticationMethod, builder, Program.RawArgs);
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
    }
}
