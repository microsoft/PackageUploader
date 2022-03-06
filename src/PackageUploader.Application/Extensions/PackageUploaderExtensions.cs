// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Extensions;

internal static class PackageUploaderExtensions
{
    public static async Task<GameProduct> GetProductAsync(this IPackageUploaderService storeBroker, BaseOperationConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(storeBroker);
        ArgumentNullException.ThrowIfNull(config);

        if (!string.IsNullOrWhiteSpace(config.BigId))
        {
            return await storeBroker.GetProductByBigIdAsync(config.BigId, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(config.ProductId))
        {
            return await storeBroker.GetProductByProductIdAsync(config.ProductId, ct).ConfigureAwait(false);
        }

        throw new Exception("BigId or ProductId needed.");
    }

    public static async Task<IGamePackageBranch> GetGamePackageBranch(this IPackageUploaderService storeBroker, GameProduct product, PackageBranchOperationConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(storeBroker);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(config);

        if (!string.IsNullOrWhiteSpace(config.BranchFriendlyName))
        {
            return await storeBroker.GetPackageBranchByFriendlyNameAsync(product, config.BranchFriendlyName, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(config.FlightName))
        {
            return await storeBroker.GetPackageFlightByFlightNameAsync(product, config.FlightName, ct).ConfigureAwait(false);
        }

        throw new Exception("BranchFriendlyName or FlightName needed.");
    }

    public static async Task<GameMarketGroupPackage> GetGameMarketGroupPackage(this IPackageUploaderService storeBroker, GameProduct product, IGamePackageBranch packageBranch, UploadPackageOperationConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(storeBroker);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        ArgumentNullException.ThrowIfNull(config);

        var packageConfiguration = await storeBroker.GetPackageConfigurationAsync(product, packageBranch, ct).ConfigureAwait(false);

        if (packageConfiguration is null)
        {
            throw new Exception($"Package Configuration not found for branch '{packageBranch.BranchFriendlyName}'.");
        }

        if (packageConfiguration.MarketGroupPackages is null || !packageConfiguration.MarketGroupPackages.Any())
        {
            throw new Exception($"Branch '{packageBranch.BranchFriendlyName}' does not have any Market Group Packages.");
        }
            
        var marketGroupPackage = packageConfiguration.MarketGroupPackages.SingleOrDefault(x => x.Name.Equals(config.MarketGroupName));

        if (marketGroupPackage is null)
        {
            throw new Exception($"Market Group '{config.MarketGroupName}' (case sensitive) not found in branch '{packageBranch.BranchFriendlyName}'.");
        }
        return marketGroupPackage;
    }

    public static async Task<IGamePackageBranch> GetDestinationGamePackageBranch(this IPackageUploaderService storeBroker, GameProduct product, ImportPackagesOperationConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(storeBroker);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(config);

        if (!string.IsNullOrWhiteSpace(config.DestinationBranchFriendlyName))
        {
            return await storeBroker.GetPackageBranchByFriendlyNameAsync(product, config.DestinationBranchFriendlyName, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(config.DestinationFlightName))
        {
            return await storeBroker.GetPackageFlightByFlightNameAsync(product, config.DestinationFlightName, ct).ConfigureAwait(false);
        }

        throw new Exception("DestinationBranchFriendlyName or DestinationFlightName needed.");
    }
}