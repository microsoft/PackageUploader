// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.FileLogger;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
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
    private static readonly Option<FileInfo> ConfigFileOption = new Option<FileInfo>(["-c", "--ConfigFile"], "The location of the config file").Required();
    private static readonly Option<IngestionExtensions.AuthenticationMethod> AuthenticationMethodOption = new(["-a", "--Authentication"], () => IngestionExtensions.AuthenticationMethod.AppSecret, "The authentication method");
    public static readonly Option<string> ProductIdOption = new(["-p", "--Product"], "Product ID");
    public static readonly Option<string> BigIdOption = new(["-b", "--BigId"], "Big ID");
    public static readonly Option<string> BranchOption = new(["-br", "--Branch"], "Branch Friendly Name");


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

        var authenticationMethod = invocationContext.GetOptionValue(AuthenticationMethodOption);
        if (authenticationMethod is IngestionExtensions.AuthenticationMethod.AppSecret)
        {
            var switchMappings = ClientSecretOption.Aliases.ToDictionary(s => s, _ => $"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}");
            builder.AddCommandLine(args, switchMappings);
        }
    }

    private static CommandLineBuilder BuildCommandLine()
    {
        var rootCommand = new RootCommand
        {
            new Command("GetProduct", "Gets metadata of the product")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption, DataOption, ProductIdOption, BigIdOption
            }.AddOperationHandler<GetProductOperation>(),
            new Command("UploadUwpPackage", "Uploads Uwp game package")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption
            }.AddOperationHandler<UploadUwpPackageOperation>(),
            new Command("UploadXvcPackage", "Uploads Xvc game package and assets")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption
            }.AddOperationHandler<UploadXvcPackageOperation>(),
            new Command("RemovePackages", "Removes all game packages and assets from a branch")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption
            }.AddOperationHandler<RemovePackagesOperation>(),
            new Command("ImportPackages", "Imports all game packages from a branch to a destination branch")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption
            }.AddOperationHandler<ImportPackagesOperation>(),
            new Command("PublishPackages", "Publishes all game packages from a branch or flight to a destination sandbox or flight")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption
            }.AddOperationHandler<PublishPackagesOperation>(),
            new Command("GetPackages", "Gets the list of packages from a branch or flight")
            {
                ConfigFileOption, ClientSecretOption, AuthenticationMethodOption, DataOption
            }.AddOperationHandler<GetPackagesOperation>(),
        };
        rootCommand.AddGlobalOption(VerboseOption);
        rootCommand.AddGlobalOption(LogFileOption);
        rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center";
        return new CommandLineBuilder(rootCommand);
    }
}