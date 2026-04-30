// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Operations;
using PackageUploader.ClientApi;
using System.CommandLine;
using System.IO;

namespace PackageUploader.Application
{
    internal static class CommandLineHelper
    {
        // Options
        internal static readonly Option<bool> DataOption = new("--Data", "-d") { Description = "Do not log on console and only return data" };
        internal static readonly Option<bool> VerboseOption = new("--Verbose", "-v") { Description = "Log verbose messages such as http calls", Recursive = true };
        internal static readonly Option<FileInfo> LogFileOption = new("--LogFile", "-l") { Description = "The location of the log file", Recursive = true };
        internal static readonly Option<string> ClientSecretOption = new("--ClientSecret", "-s") { Description = "The client secret of the AAD app (only for AppSecret)" };
        internal static readonly Option<string> TenantIdOption = new("--TenantId", "-t") { Description = "The Azure tenant ID to use for authentication (primarily for Browser authentication)" };
        internal static readonly Option<FileInfo> ConfigFileOption = new("--ConfigFile", "-c") { Description = "The location of the config file", Required = true };
        internal static readonly Option<IngestionExtensions.AuthenticationMethod> AuthenticationMethodOption = new("--Authentication", "-a") { Description = "The authentication method", DefaultValueFactory = _ => IngestionExtensions.AuthenticationMethod.AppSecret };
        internal static readonly Option<string> ProductIdOption = new("--ProductId", "-p") { Description = "Product ID, replaces config value productId if present" };
        internal static readonly Option<string> BigIdOption = new("--BigId", "-b") { Description = "Big ID, replaces config value bigId if present" };
        internal static readonly Option<string> BranchFriendlyNameOption = new("--BranchFriendlyName", "-bf") { Description = "Branch Friendly Name, replaces config value branchFriendlyName if present" };
        internal static readonly Option<string> FlightNameOption = new("--FlightName", "-f") { Description = "Flight Name, replaces config value flightName if present" };
        internal static readonly Option<string> MarketGroupNameOption = new("--MarketGroupName", "-m") { Description = "Market Group Name, replaces config value marketGroupName if present" };
        internal static readonly Option<string> DestinationSandboxName = new("--DestinationSandboxName", "-ds") { Description = "Destination Sandbox Name, replaces config value destinationSandboxName if present" };

        internal static RootCommand BuildRootCommand()
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
            rootCommand.Options.Add(VerboseOption);
            rootCommand.Options.Add(LogFileOption);
            rootCommand.Description = "Application that enables game developers to upload Xbox and PC game packages to Partner Center";
            return rootCommand;
        }

        public static Command AddOperationHandler<T>(this Command command) where T : Operation
        {
            command.Action = new OperationInvoker<T>();
            return command;
        }
    }
}