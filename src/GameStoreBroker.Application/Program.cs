﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Operations;
using GameStoreBroker.ClientApi;
using GameStoreBroker.FileLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application
{
    internal static class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd hh:mm:ss.fff ";
        private static readonly Option<bool> VerboseOption = new (new[] { "-v", "--Verbose" }, "Log verbose messages such as http calls.");
        private static readonly Option<FileInfo> LogFileOption = new(new[] { "-l", "--LogFile" }, "The location of the log file.");

        private static async Task<int> Main(string[] args)
        {
            return await BuildCommandLine()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureLogging(ConfigureLogging)
                    .ConfigureServices(ConfigureServices)
                    )
                .UseDefaults()
                .Build()
                .InvokeAsync(args)
                .ConfigureAwait(false);
        }

        private static void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder logging)
        {
            var invocationContext = ctx.GetInvocationContext();
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("GameStoreBroker", invocationContext.GetOptionValue(VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            logging.AddSimpleFile(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            }, file =>
            {
                var logFile = invocationContext.GetOptionValue(LogFileOption);
                file.Path = logFile?.FullName ?? Path.Combine(Path.GetTempPath(), $"GameStoreBroker_{DateTime.Now:yyyyMMddhhmmss}.log");
                file.Append = true;
            });
            logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            });
        }

        private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddLogging();
            services.AddGameStoreBrokerService();
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            // Options
            var configFile = new Option<FileInfo>(new[] {"-c", "--ConfigFile"}, "The location of the json config file.")
            {
                IsRequired = true,
            };
            var clientSecret = new Option<string>(new[] {"-s", "--ClientSecret"}, "The client secret of the AAD app.");

            // Root Command
            var rootCommand = new RootCommand
            {
                new Command("GetProduct", "Gets metadata of the product.")
                {
                    configFile,
                    clientSecret,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(GetProductAsync)),
                new Command("UploadUwpPackage", "Gets metadata of the product.")
                {
                    configFile,
                    clientSecret,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(UploadUwpPackageAsync)),
            };
            rootCommand.AddGlobalOption(VerboseOption);
            rootCommand.AddGlobalOption(LogFileOption);
            rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center.";
            return new CommandLineBuilder(rootCommand);
        }

        private static async Task<int> GetProductAsync(IHost host, Options options, CancellationToken ct) => 
            await new GetProductOperation(host, options).RunAsync(ct).ConfigureAwait(false);

        private static async Task<int> UploadUwpPackageAsync(IHost host, Options options, CancellationToken ct) =>
            await new UploadUwpPackageOperation(host, options).RunAsync(ct).ConfigureAwait(false);
    }
}
