﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;

namespace PackageUploader.Application
{
    internal class ParameterHelper
    {
        // Options
        internal static readonly Option<bool> DataOption = new(["-d", "--Data"], "Do not log on console and only return data");
        internal static readonly Option<bool> VerboseOption = new(["-v", "--Verbose"], "Log verbose messages such as http calls");
        internal static readonly Option<FileInfo> LogFileOption = new(["-l", "--LogFile"], "The location of the log file");
        internal static readonly Option<string> ClientSecretOption = new(["-s", "--ClientSecret"], "The client secret of the AAD app (only for AppSecret)");
        internal static readonly Option<string> TenantIdOption = new(["-t", "--TenantId"], "The Azure tenant ID to use for authentication (primarily for Browser authentication)");
        internal static readonly Option<FileInfo> ConfigFileOption = new Option<FileInfo>(["-c", "--ConfigFile"], "The location of the config file").Required();
        internal static readonly Option<IngestionExtensions.AuthenticationMethod> AuthenticationMethodOption = new(["-a", "--Authentication"], () => IngestionExtensions.AuthenticationMethod.AppSecret, "The authentication method");
        internal static readonly Option<string> ProductIdOption = new(["-p", "--ProductId"], "Product ID, replaces config value productId if present");
        internal static readonly Option<string> BigIdOption = new(["-b", "--BigId"], "Big ID, replaces config value bigId if present");
        internal static readonly Option<string> BranchFriendlyNameOption = new(["-bf", "--BranchFriendlyName"], "Branch Friendly Name, replaces config value branchFriendlyName if present");
        internal static readonly Option<string> FlightNameOption = new(["-f", "--FlightName"], "Flight Name, replaces config value flightName if present");
        internal static readonly Option<string> MarketGroupNameOption = new(["-m", "--MarketGroupName"], "Market Group Name, replaces config value marketGroupName if present");
        internal static readonly Option<string> DestinationSandboxName = new(["-ds", "--DestinationSandboxName"], "Destination Sandbox Name, replaces config value destinationSandboxName if present");

        internal static CommandLineBuilder BuildCommandLine()
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

        internal static void ConfigureParameters(FileInfo configFile, IngestionExtensions.AuthenticationMethod authenticationMethod, IConfigurationBuilder builder, string[] args)
        {
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

            // Configure auth options based on the authentication method
            if (authenticationMethod is IngestionExtensions.AuthenticationMethod.AppSecret)
            {
                // Add client secret mapping for AppSecret auth (AadAuthInfo, NOT ClientSecretAuthInfo)
                ClientSecretOption.AddAliasesToSwitchMappings(switchMappings, $"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}");
            }

            // Add tenant ID mapping for browser authentication methods
            if (authenticationMethod is IngestionExtensions.AuthenticationMethod.Browser or
                                        IngestionExtensions.AuthenticationMethod.CacheableBrowser)
            {
                TenantIdOption.AddAliasesToSwitchMappings(switchMappings, $"{BrowserAuthInfo.ConfigName}:{nameof(BrowserAuthInfo.TenantId)}");
            }

            builder.AddCommandLine(args, switchMappings);
        }
    }
}