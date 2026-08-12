// Copyright(c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.FileLogger;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace PackageUploader.Application.Extensions
{
    internal static class HostExtensions
    {
        public static HostApplicationBuilder CreateHostApplicationBuilder(this ParseResult parseResult)
        {
            return Host.CreateEmptyApplicationBuilder(null)
                .ConfigureAppConfiguration(parseResult)
                .ConfigureLogging(parseResult)
                .ConfigureServices(parseResult);
        }

        internal static HostApplicationBuilder ConfigureLogging(this HostApplicationBuilder hostAppBuilder, ParseResult parseResult)
        {
            const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

            var isData = parseResult.GetValue(CommandLineHelper.DataOption);
# if DEBUG
            hostAppBuilder.Logging.AddDebug();
#endif
            hostAppBuilder.Logging.SetMinimumLevel(LogLevel.Error);
            hostAppBuilder.Logging.AddFilter("PackageUploader",
                isData ? LogLevel.Error :
                parseResult.GetValue(CommandLineHelper.VerboseOption) ? LogLevel.Trace : LogLevel.Information);
            hostAppBuilder.Logging.AddFilter<FileLoggerProvider>("PackageUploader", LogLevel.Trace);
            hostAppBuilder.Logging.AddSimpleFile(options =>
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
                hostAppBuilder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Error);
            }
            hostAppBuilder.Logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = LogTimestampFormat;
            });

            return hostAppBuilder;
        }

        internal static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder hostAppBuilder, ParseResult parseResult)
        {
            var isData = parseResult.GetValue(CommandLineHelper.DataOption);

            hostAppBuilder.Services.AddLogging();
            hostAppBuilder.Services.AddSingleton(new DataOutputOptions(isData));
            hostAppBuilder.Services.AddPackageUploaderService(parseResult.GetValue(CommandLineHelper.AuthenticationMethodOption));

            hostAppBuilder.Services.AddSingleton(BuildMsixvc2CommandLineContext(hostAppBuilder, parseResult));
            hostAppBuilder.Services.AddSingleton<IMsixvc2ProcessRunner, Msixvc2ProcessRunner>();
            hostAppBuilder.Services.AddSingleton<IParentProcessProvider, ParentProcessProvider>();
            hostAppBuilder.Services.AddSingleton<IMsixvc2DelegationGuard, Msixvc2DelegationGuard>();
            // TODO(GDK-release): swap Msixvc2CapabilityPlaceholder for an adapter over
            // PackageUploader.ClientApi.Tools.IMsixvc2ToolResolver once PR #<change-1> merges.
            hostAppBuilder.Services.AddSingleton<IMsixvc2UploadToolProvider, Msixvc2CapabilityPlaceholder>();

            hostAppBuilder.Services
                .AddScoped<GetProductOperation>()
                .AddSingleton<IValidateOptions<GetProductOperationConfig>, GetProductOperationValidator>()
                .AddOptions<GetProductOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<GetPackagesOperation>()
                .AddSingleton<IValidateOptions<GetPackagesOperationConfig>, GetPackagesOperationValidator>()
                .AddOptions<GetPackagesOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<UploadUwpPackageOperation>()
                .AddSingleton<IValidateOptions<UploadUwpPackageOperationConfig>, UploadUwpPackageOperationValidator>()
                .AddOptions<UploadUwpPackageOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<UploadXvcPackageOperation>()
                .AddSingleton<IValidateOptions<UploadXvcPackageOperationConfig>, UploadXvcPackageOperationValidator>()
                .AddOptions<UploadXvcPackageOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<RemovePackagesOperation>()
                .AddSingleton<IValidateOptions<RemovePackagesOperationConfig>, RemovePackagesOperationValidator>()
                .AddOptions<RemovePackagesOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<ImportPackagesOperation>()
                .AddSingleton<IValidateOptions<ImportPackagesOperationConfig>, ImportPackagesOperationValidator>()
                .AddOptions<ImportPackagesOperationConfig>().Bind(hostAppBuilder.Configuration);

            hostAppBuilder.Services
                .AddScoped<PublishPackagesOperation>()
                .AddSingleton<IValidateOptions<PublishPackagesOperationConfig>, PublishPackagesOperationValidator>()
                .AddOptions<PublishPackagesOperationConfig>().Bind(hostAppBuilder.Configuration);

            return hostAppBuilder;
        }

        /// <summary>
        /// Collects the authentication surface MakePkg.exe needs for MSIXVC2 uploads. PackageUploader spreads
        /// this across a command line option (--Authentication) and several configuration sections, and
        /// MakePkg.exe accepts the same credential material through /auth, /tenantid, /clientid,
        /// /clientsecret, /certthumbprint, /certstore, /certlocation and /resourceid — so a CI pipeline using
        /// a service principal keeps working when the upload is delegated.
        /// </summary>
        private static Msixvc2CommandLineContext BuildMsixvc2CommandLineContext(HostApplicationBuilder hostAppBuilder, ParseResult parseResult)
        {
            var configuration = hostAppBuilder.Configuration;
            var aad = configuration.GetSection(AadAuthInfo.ConfigName);
            var clientSecret = configuration.GetSection(ClientSecretAuthInfo.ConfigName);
            var clientCertificate = configuration.GetSection(ClientCertificateAuthInfo.ConfigName);

            static string First(params string[] values) =>
                values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return new Msixvc2CommandLineContext(
                parseResult.GetValue(CommandLineHelper.AuthenticationMethodOption),
                TenantId: First(
                    parseResult.GetValue(CommandLineHelper.TenantIdOption),
                    aad[nameof(AadAuthInfo.TenantId)],
                    clientSecret[nameof(ClientSecretAuthInfo.TenantId)],
                    clientCertificate[nameof(ClientCertificateAuthInfo.TenantId)]),
                ClientId: First(
                    aad[nameof(AadAuthInfo.ClientId)],
                    clientSecret[nameof(ClientSecretAuthInfo.ClientId)],
                    clientCertificate[nameof(ClientCertificateAuthInfo.ClientId)]),
                ClientSecret: First(
                    aad[nameof(AzureApplicationSecretAuthInfo.ClientSecret)],
                    clientSecret[nameof(ClientSecretAuthInfo.ClientSecret)]),
                CertificateThumbprint: aad[nameof(AzureApplicationCertificateAuthInfo.CertificateThumbprint)],
                CertificateSubject: aad[nameof(AzureApplicationCertificateAuthInfo.CertificateSubject)],
                CertificateStore: aad[nameof(AzureApplicationCertificateAuthInfo.CertificateStore)],
                CertificateLocation: aad[nameof(AzureApplicationCertificateAuthInfo.CertificateLocation)],
                CertificatePath: clientCertificate[nameof(ClientCertificateAuthInfo.CertificatePath)],
                ResourceId: aad["ResourceId"]);
        }

        internal static HostApplicationBuilder ConfigureAppConfiguration(this HostApplicationBuilder hostAppBuilder, ParseResult parseResult)
        {
            var configFile = parseResult.GetValue(CommandLineHelper.ConfigFileOption);
            var authenticationMethod = parseResult.GetValue(CommandLineHelper.AuthenticationMethodOption);

            if (configFile is not null)
            {
                hostAppBuilder.Configuration.AddJsonFile(configFile.FullName, false, false);
            }

            var switchMappings = new Dictionary<string, string>();
            CommandLineHelper.ProductIdOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(BaseOperationConfig.ProductId)}");
            CommandLineHelper.BigIdOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(BaseOperationConfig.BigId)}");
            CommandLineHelper.BranchFriendlyNameOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(PackageBranchOperationConfig.BranchFriendlyName)}");
            CommandLineHelper.FlightNameOption.AddAliasesToSwitchMappings(switchMappings, $"{nameof(PackageBranchOperationConfig.FlightName)}");
            CommandLineHelper.MarketGroupNameOption.AddAliasesToSwitchMappings(switchMappings, "MarketGroupName");
            CommandLineHelper.DestinationSandboxName.AddAliasesToSwitchMappings(switchMappings, "DestinationSandboxName");

            // Configure auth options based on the authentication method
            if (authenticationMethod is IngestionExtensions.AuthenticationMethod.AppSecret)
            {
                // Add client secret mapping for AppSecret auth (AadAuthInfo, NOT ClientSecretAuthInfo)
                CommandLineHelper.ClientSecretOption.AddAliasesToSwitchMappings(switchMappings, $"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}");
            }
            else if (authenticationMethod is IngestionExtensions.AuthenticationMethod.Browser
                                          or IngestionExtensions.AuthenticationMethod.CacheableBrowser)
            {
                // Add tenant ID mapping for browser authentication methods
                CommandLineHelper.TenantIdOption.AddAliasesToSwitchMappings(switchMappings, $"{BrowserAuthInfo.ConfigName}:{nameof(BrowserAuthInfo.TenantId)}");
            }

            var rawArgs = parseResult.Tokens.Select(t => t.Value).ToArray();
            hostAppBuilder.Configuration.AddCommandLine(rawArgs, switchMappings);

            return hostAppBuilder;
        }

        internal static void AddAliasesToSwitchMappings(this Option option, Dictionary<string, string> switchMappings, string configPath)
        {
            switchMappings[option.Name] = configPath;
            foreach (var alias in option.Aliases)
            {
                switchMappings[alias] = configPath;
            }
        }
    }
}
