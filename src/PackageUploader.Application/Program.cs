﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.FileLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PackageUploader.Application
{
    internal class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

        // Options
        private static readonly Option<bool> VerboseOption = new (new[] { "-v", "--Verbose" }, "Log verbose messages such as http calls");
        private static readonly Option<FileInfo> LogFileOption = new(new[] { "-l", "--LogFile" }, "The location of the log file");
        private static readonly Option<string> ClientSecretOption = new (new[] { "-s", "--ClientSecret" }, "The client secret of the AAD app");
        private static readonly Option<FileInfo> ConfigFileOption = new Option<FileInfo>(new[] { "-c", "--ConfigFile" }, "The location of the config file").Required();
        private static readonly Option<ConfigFileFormat> ConfigFileFormatOption = new(new[] { "-f", "--ConfigFileFormat" }, () => ConfigFileFormat.Json, "The format of the config file");
        private static readonly Option<bool> RetailOption = new(new[] { "--Retail" }, "Allow publish packages to RETAIL sandbox");

        internal enum ConfigFileFormat
        {
            Json,
            Xml,
            Ini,
        }

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
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("PackageUploader", invocationContext.GetOptionValue(VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            logging.AddSimpleFile(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            }, file =>
            {
                var logFile = invocationContext.GetOptionValue(LogFileOption);
                file.Path = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"PackageUploader_{DateTime.Now:yyyyMMddHHmmss}.log");
                file.Append = true;
            });
            logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            });
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddLogging();
            services.AddPackageUploaderService(context.Configuration);

            services.AddOperation<GetProductOperation, GetProductOperationConfig>(context);
            services.AddOperation<UploadUwpPackageOperation, UploadUwpPackageOperationConfig>(context);
            services.AddOperation<UploadXvcPackageOperation, UploadXvcPackageOperationConfig>(context);
            services.AddOperation<RemovePackagesOperation, RemovePackagesOperationConfig>(context);
            services.AddOperation<ImportPackagesOperation, ImportPackagesOperationConfig>(context);
            services.AddOperation<PublishPackagesOperation, PublishPackagesOperationConfig>(context);
        }

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder, string[] args)
        {
            var invocationContext = context.GetInvocationContext();
            var configFile = invocationContext.GetOptionValue(ConfigFileOption);
            if (configFile is not null)
            {
                var configFileFormat = invocationContext.GetOptionValue(ConfigFileFormatOption);
                builder.AddConfigFile(configFile, configFileFormat);
            }

            var switchMappings = ClientSecretOption.Aliases.ToDictionary(s => s, _ => $"{nameof(AadAuthInfo)}:{nameof(AadAuthInfo.ClientSecret)}");
            builder.AddCommandLine(args, switchMappings);

            if (invocationContext.GetOptionValue(RetailOption))
            {
                builder.AddInMemoryCollection(new List<KeyValuePair<string, string>>
                {
                    new (nameof(PublishPackagesOperationConfig.Retail), true.ToString())
                });
            }
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var rootCommand = new RootCommand
            {
                new Command("GetProduct", "Gets metadata of the product")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption,
                }.AddOperationHandler<GetProductOperation>(),
                new Command("UploadUwpPackage", "Uploads Uwp game package")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption,
                }.AddOperationHandler<UploadUwpPackageOperation>(),
                new Command("UploadXvcPackage", "Uploads Xvc game package and assets")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption,
                }.AddOperationHandler<UploadXvcPackageOperation>(),
                new Command("RemovePackages", "Removes all game packages and assets from a branch")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption,
                }.AddOperationHandler<RemovePackagesOperation>(),
                new Command("ImportPackages", "Imports all game packages from a branch to a destination branch")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption,
                }.AddOperationHandler<ImportPackagesOperation>(),
                new Command("PublishPackages", "Publishes all game packages from a branch or flight to a destination sandbox or flight")
                {
                    ConfigFileOption, ConfigFileFormatOption, ClientSecretOption, RetailOption,
                }.AddOperationHandler<PublishPackagesOperation>(),
            };
            rootCommand.AddGlobalOption(VerboseOption);
            rootCommand.AddGlobalOption(LogFileOption);
            rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center";
            return new CommandLineBuilder(rootCommand);
        }
    }
}
