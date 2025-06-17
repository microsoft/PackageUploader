// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.FileLogger;
using System;
using System.Collections.Generic;
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

    // Options
    public static readonly Option<bool> DataOption = new (["-d", "--Data"], "Do not log on console and only return data");
    private static readonly Option<bool> VerboseOption = new (["-v", "--Verbose"], "Log verbose messages such as http calls");
    private static readonly Option<FileInfo> LogFileOption = new(["-l", "--LogFile"], "The location of the log file");
    private static readonly Option<string> ClientSecretOption = new (["-s", "--ClientSecret"], "The client secret of the AAD app (only for AppSecret)");
    private static readonly Option<string> TenantIdOption = new (["-t", "--TenantId"], "The Azure tenant ID to use for authentication (primarily for Browser authentication)");
    private static readonly Option<FileInfo> ConfigFileOption = new Option<FileInfo>(["-c", "--ConfigFile"], "The location of the config file").Required();
    private static readonly Option<IngestionExtensions.AuthenticationMethod> AuthenticationMethodOption = new(["-a", "--Authentication"], () => IngestionExtensions.AuthenticationMethod.AppSecret, "The authentication method");
    private static readonly Option<string> ProductIdOption = new(["-p", "--ProductId"], "Product ID, replaces config value productId if present");
    private static readonly Option<string> BigIdOption = new(["-b", "--BigId"], "Big ID, replaces config value bigId if present");
    private static readonly Option<string> BranchFriendlyNameOption = new(["-bf", "--BranchFriendlyName"], "Branch Friendly Name, replaces config value branchFriendlyName if present");
    private static readonly Option<string> FlightNameOption = new(["-f", "--FlightName"], "Flight Name, replaces config value flightName if present");
    private static readonly Option<string> MarketGroupNameOption = new(["-m", "--MarketGroupName"], "Market Group Name, replaces config value marketGroupName if present");
    private static readonly Option<string> DestinationSandboxName = new(["-ds", "--DestinationSandboxName"], "Destination Sandbox Name, replaces config value destinationSandboxName if present");


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
        var isData = invocationContext.GetOptionValue(DataOption);
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Error);
        logging.AddFilter("PackageUploader",
            isData ? LogLevel.Error :
            invocationContext.GetOptionValue(VerboseOption) ? LogLevel.Trace : LogLevel.Information);
        logging.AddFilter<FileLoggerProvider>("PackageUploader", LogLevel.Trace);
        logging.AddSimpleFile(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = LogTimestampFormat;
        }, file =>
        {
            var logFile = invocationContext.GetOptionValue(LogFileOption);
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
        services.AddPackageUploaderService(invocationContext.GetOptionValue(AuthenticationMethodOption));
        services.AddOperations(context);
    }

    private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder, string[] args)
    {
        var invocationContext = context.GetInvocationContext();

        var configFile = invocationContext.GetOptionValue(ConfigFileOption);
        if (configFile is not null)
        {
            builder.AddJsonFile(configFile.FullName, false, false);
        }

        var switchMappings = new Dictionary<string, string>();
        ProductIdOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(BaseOperationConfig.ProductId)}");
        BigIdOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(BaseOperationConfig.BigId)}");
        BranchFriendlyNameOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(PackageBranchOperationConfig.BranchFriendlyName)}");
        FlightNameOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(PackageBranchOperationConfig.FlightName)}");
        MarketGroupNameOption.AddAliasesToSwitchMappings(switchMappings, "MarketGroupName");
        DestinationSandboxName.AddAliasesToSwitchMappings(switchMappings, "DestinationSandboxName");

        var authenticationMethod = invocationContext.GetOptionValue(AuthenticationMethodOption);
        
        // Configure auth options based on the authentication method
        if (authenticationMethod is IngestionExtensions.AuthenticationMethod.AppSecret)
        {
            // Add client secret mapping for AppSecret auth
            foreach (var alias in ClientSecretOption.Aliases)
            {
                switchMappings.Add(alias, $"{ClientSecretAuthInfo.ConfigName}:{nameof(ClientSecretAuthInfo.ClientSecret)}");
            }
        }
        
        // Add tenant ID mapping for browser authentication methods
        if (authenticationMethod is IngestionExtensions.AuthenticationMethod.Browser or 
                                    IngestionExtensions.AuthenticationMethod.CacheableBrowser)
        {
            foreach (var alias in TenantIdOption.Aliases)
            {
                switchMappings.Add(alias, $"{BrowserAuthInfo.ConfigName}:{nameof(BrowserAuthInfo.TenantId)}");
            }
        }

        if (switchMappings.Count > 0)
        {
            builder.AddCommandLine(args, switchMappings);
        }
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        var rootCommand = new RootCommand
        {
            new Command("GetProduct", "Gets metadata of the product")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, DataOption, ProductIdOption, BigIdOption
            }.AddOperationHandler<GetProductOperation>(),
            new Command("UploadUwpPackage", "Uploads Uwp game package")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, MarketGroupNameOption
            }.AddOperationHandler<UploadUwpPackageOperation>(),
            new Command("UploadXvcPackage", "Uploads Xvc game package and assets")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, MarketGroupNameOption
            }.AddOperationHandler<UploadXvcPackageOperation>(),
            new Command("RemovePackages", "Removes all game packages and assets from a branch")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, MarketGroupNameOption
            }.AddOperationHandler<RemovePackagesOperation>(),
            new Command("ImportPackages", "Imports all game packages from a branch to a destination branch")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, MarketGroupNameOption
            }.AddOperationHandler<ImportPackagesOperation>(),
            new Command("PublishPackages", "Publishes all game packages from a branch or flight to a destination sandbox or flight")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, DestinationSandboxName // take a look at conditions
            }.AddOperationHandler<PublishPackagesOperation>(),
            new Command("GetPackages", "Gets the list of packages from a branch or flight")
            {
                ConfigFileOption, ClientSecretOption, TenantIdOption, AuthenticationMethodOption, DataOption, ProductIdOption, BigIdOption, BranchFriendlyNameOption, FlightNameOption, MarketGroupNameOption
            }.AddOperationHandler<GetPackagesOperation>(),
        };
        rootCommand.AddGlobalOption(VerboseOption);
        rootCommand.AddGlobalOption(LogFileOption);
        rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center";
        return new CommandLineBuilder(rootCommand);
    }
}