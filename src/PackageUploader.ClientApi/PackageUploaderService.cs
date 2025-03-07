// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using PackageUploader.ClientApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi;

public class PackageUploaderService : IPackageUploaderService
{
    private readonly IIngestionHttpClient _ingestionHttpClient;
    private readonly IXfusUploader _xfusUploader;
    private readonly ILogger<PackageUploaderService> _logger;

    public PackageUploaderService(IIngestionHttpClient ingestionHttpClient, IXfusUploader xfusUploader, ILogger<PackageUploaderService> logger)
    {
        _ingestionHttpClient = ingestionHttpClient ?? throw new ArgumentNullException(nameof(ingestionHttpClient));
        _xfusUploader = xfusUploader ?? throw new ArgumentNullException(nameof(xfusUploader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(bigId);

        _logger.LogDebug("Requesting game product by BigId '{bigId}'.", bigId);
        return await _ingestionHttpClient.GetGameProductByBigIdAsync(bigId, ct).ConfigureAwait(false);
    }

    public async Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);

        _logger.LogDebug("Requesting game product by ProductId '{productId}'.", productId);
        return await _ingestionHttpClient.GetGameProductByLongIdAsync(productId, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<IGamePackageBranch>> GetPackageBranchesAsync(GameProduct product, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);

        _logger.LogDebug("Requesting game package branches.");
        return await _ingestionHttpClient.GetPackageBranchesAsync(product.ProductId, ct).ConfigureAwait(false);
    }

    public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        StringArgumentException.ThrowIfNullOrWhiteSpace(branchFriendlyName);

        _logger.LogDebug("Requesting game package branch by branch friendly name '{branchFriendlyName}'.", branchFriendlyName);
        return await _ingestionHttpClient.GetPackageBranchByFriendlyNameAsync(product.ProductId, branchFriendlyName, ct).ConfigureAwait(false);
    }

    public async Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        StringArgumentException.ThrowIfNullOrWhiteSpace(flightName);

        _logger.LogDebug("Requesting game package flight by flight name '{flightName}'.", flightName);
        return await _ingestionHttpClient.GetPackageFlightByFlightNameAsync(product.ProductId, flightName, ct).ConfigureAwait(false);
    }

    public async Task<GamePackageConfiguration> GetPackageConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);

        _logger.LogDebug("Requesting game package configuration by product id '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);

        var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

        if (packageConfiguration.MarketGroupPackages is null || packageConfiguration.MarketGroupPackages.Count == 0)
        {
            _logger.LogDebug("Initializing game package configuration in branch '{branchName}'.", packageConfiguration.BranchName);
            packageConfiguration.MarketGroupPackages =
            [
                new()
                {
                    MarketGroupId = "default",
                    Name = "default",
                    Markets = null,
                    PackageIds = [],
                    AvailabilityDate = null,
                    PackageAvailabilityDates = [],
                    PackageIdToMetadataMap = [],
                    MandatoryUpdateInfo = null,
                },
            ];
            packageConfiguration = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
        }

        return packageConfiguration;
    }

    public async Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(GameProduct product, GamePackageConfiguration packageConfiguration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageConfiguration);

        _logger.LogDebug("Updating game package configuration in product id '{productId}' and package configuration id '{packageConfigurationId}'.", product.ProductId, packageConfiguration.Id);

        var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
        return result;
    }

    public async IAsyncEnumerable<GamePackage> GetGamePackagesAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupName, [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        StringArgumentException.ThrowIfNullOrWhiteSpace(marketGroupName);

        var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false) 
            ?? throw new Exception($"Package Configuration not found for {packageBranch.BranchType.ToString().ToLower()} '{packageBranch.Name}'.");

        if (packageConfiguration.MarketGroupPackages is null || packageConfiguration.MarketGroupPackages.Count == 0)
        {
            throw new Exception($"{packageBranch.BranchType} '{packageBranch.Name}' does not have any Market Group Packages.");
        }

        var marketGroupPackage = packageConfiguration.MarketGroupPackages.SingleOrDefault(x => x.Name.Equals(marketGroupName))
            ?? throw new Exception($"Market Group '{marketGroupName}' (case sensitive) not found in {packageBranch.BranchType.ToString().ToLower()} '{packageBranch.Name}'.");

        foreach (var packageId in marketGroupPackage.PackageIds)
        {
            yield return await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, packageId, ct).ConfigureAwait(false);
        }
    }

    public async Task<GamePackage> UploadGamePackageAsync(GameProduct product, IGamePackageBranch packageBranch, GameMarketGroupPackage marketGroupPackage, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, bool deltaUpload, bool isXvc, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        ArgumentNullException.ThrowIfNull(marketGroupPackage);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageFilePath);

        var packageFile = new FileInfo(packageFilePath);
        if (!packageFile.Exists)
        {
            throw new FileNotFoundException("Package file not found.", packageFile.FullName);
        }

        var xvcTargetPlatform = isXvc ? ReadXvcTargetPlatformFromMetaData(packageFile) : XvcTargetPlatform.NotSpecified;

        _logger.LogDebug("Creating game package for file '{fileName}', product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFile.Name, product.ProductId, packageBranch.CurrentDraftInstanceId);
        var package = await _ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, packageFile.Name, marketGroupPackage.MarketGroupId, isXvc, xvcTargetPlatform, ct).ConfigureAwait(false);

        if (gameAssets is not null)
        {
            await UploadAssetAsync(product, package, gameAssets.EkbFilePath, GamePackageAssetType.EkbFile, ct).ConfigureAwait(false);
        }

        _logger.LogDebug("Uploading file '{fileName}'.", packageFile.Name);
        await _xfusUploader.UploadFileToXfusAsync(packageFile, package.UploadInfo, deltaUpload, ct).ConfigureAwait(false);
        _logger.LogDebug("Package file '{fileName}' uploaded.", packageFile.Name);

        package = await _ingestionHttpClient.ProcessPackageRequestAsync(product.ProductId, package, ct).ConfigureAwait(false);
        _logger.LogInformation("Package is uploaded and is in processing.");

        package = await WaitForPackageProcessingAsync(product, package, minutesToWaitForProcessing, 1, ct).ConfigureAwait(false);

        if (gameAssets is not null)
        {
            await UploadAssetAsync(product, package, gameAssets.SymbolsFilePath, GamePackageAssetType.SymbolsZip, ct).ConfigureAwait(false);
            await UploadAssetAsync(product, package, gameAssets.SubValFilePath, GamePackageAssetType.SubmissionValidatorLog, ct).ConfigureAwait(false);
            await UploadAssetAsync(product, package, gameAssets.DiscLayoutFilePath, GamePackageAssetType.DiscLayoutFile, ct).ConfigureAwait(false);
        }

        return package;
    }

    public async Task<GamePackageConfiguration> RemovePackagesAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupName, string packageFileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageFileName);

        _logger.LogDebug("Removing game packages with filename '{packageFileName}' in product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFileName, product.ProductId, packageBranch.CurrentDraftInstanceId);

        var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

        // Converting Wildcards in packageFileName to Regex
        var regex = new Regex('^' + Regex.Escape(packageFileName).
            Replace("\\*", ".*").
            Replace("\\?", ".") + '$', RegexOptions.IgnoreCase);

        var packagesRemoved = 0;

        // Finding the package with the specified filename and removing it for each market group package
        if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
            {
                _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in {branchType} '{branchName}'.", marketGroupName, packageBranch.BranchType.ToString().ToLower(), packageBranch.Name);
            }
            else
            {
                var packages = new Dictionary<string, GamePackage>();
                foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages)
                {
                    if (string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name))
                    {
                        if (packageFileName.Equals("*", StringComparison.OrdinalIgnoreCase))
                        {
                            packagesRemoved += marketGroupPackage.PackageIds.Count;
                            marketGroupPackage.PackageIds = [];
                            marketGroupPackage.PackageAvailabilityDates = [];
                            marketGroupPackage.PackageIdToMetadataMap = [];
                        }
                        else
                        {
                            var packageIdsToRemove = new List<string>();
                            foreach (var packageId in marketGroupPackage.PackageIds)
                            {
                                if (!packages.TryGetValue(packageId, out var package))
                                {
                                    package = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, packageId, ct).ConfigureAwait(false);
                                    packages.Add(packageId, package);
                                }

                                if (regex.IsMatch(package.FileName))
                                {
                                    _logger.LogDebug("Removing Package with id '{gamePackageId}', File name '{packageFileName}' from Market Group '{marketGroupName}'.", packageId, package.FileName, marketGroupName);
                                    packageIdsToRemove.Add(packageId);
                                }
                            }

                            foreach (var packageId in packageIdsToRemove)
                            {
                                packagesRemoved++;
                                marketGroupPackage.PackageIds.Remove(packageId);
                                marketGroupPackage.PackageAvailabilityDates.Remove(packageId);
                                marketGroupPackage.PackageIdToMetadataMap?.Remove(packageId);
                            }
                        }
                    }
                }
            }
        }

        if (packagesRemoved > 0)
        {
            _logger.LogInformation("Removed {packagesRemoved} packages from {branchType} '{branchName}'.", packagesRemoved, packageBranch.BranchType.ToString().ToLower(), packageBranch.Name);
            var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            return result;
        }

        _logger.LogInformation("No packages removed from {branchType} '{branchName}'.", packageBranch.BranchType.ToString().ToLower(), packageBranch.Name);
        return packageConfiguration;
    }

    public async Task<GamePackageConfiguration> SetXvcConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupName, IXvcGameConfiguration gameConfiguration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        ArgumentNullException.ThrowIfNull(gamePackage);
        ArgumentNullException.ThrowIfNull(gameConfiguration);

        _logger.LogDebug("Setting the dates to package with id '{gamePackageId}' in '{productId}' and draft id '{currentDraftInstanceID}'.", gamePackage.Id, product.ProductId, packageBranch.CurrentDraftInstanceId);

        var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);
        if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
            {
                _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in {branchType} '{branchName}'.", marketGroupName, packageBranch.BranchType.ToString().ToLower(), packageBranch.Name);
            }
            else
            {

                foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages
                             .Where(marketGroupPackage => marketGroupPackage.PackageIds.Contains(gamePackage.Id))
                             .Where(marketGroupPackage => string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name)))
                {
                    // Availability Date
                    if (gameConfiguration.AvailabilityDate.IsEnabled)
                    {
                        marketGroupPackage.PackageAvailabilityDates ??= [];
                        marketGroupPackage.PackageAvailabilityDates[gamePackage.Id] = gameConfiguration.AvailabilityDate.EffectiveDate;
                    }
                    else if (marketGroupPackage.PackageAvailabilityDates is not null)
                    {
                        marketGroupPackage.PackageAvailabilityDates[gamePackage.Id] = null;
                    }

                    // PreDownload Date
                    marketGroupPackage.PackageIdToMetadataMap ??= [];
                    marketGroupPackage.PackageIdToMetadataMap[gamePackage.Id] = CalculatePackageMetadata(gameConfiguration.PreDownloadDate);
                }
            }
        }

        var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<GamePackageConfiguration> SetUwpConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupName, IUwpGameConfiguration gameConfiguration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(packageBranch);
        ArgumentNullException.ThrowIfNull(gameConfiguration);

        _logger.LogDebug("Setting the package dates in '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);

        var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

        if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
            {
                _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in {branchType} '{branchName}'.", marketGroupName, packageBranch.BranchType.ToString().ToLower(), packageBranch.Name);
            }
            else
            {
                foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages
                             .Where(marketGroupPackage => string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name)))
                {
                    // Setting the availability date
                    if (gameConfiguration.AvailabilityDate is not null)
                    {
                        marketGroupPackage.AvailabilityDate = gameConfiguration.AvailabilityDate.IsEnabled
                            ? gameConfiguration.AvailabilityDate.EffectiveDate
                            : null;
                    }

                    // Setting the mandatory date
                    if (gameConfiguration.MandatoryDate is not null)
                    {
                        marketGroupPackage.MandatoryUpdateInfo = new GameMandatoryUpdateInfo
                        {
                            IsEnabled = gameConfiguration.MandatoryDate.IsEnabled,
                            EffectiveDate = gameConfiguration.MandatoryDate.EffectiveDate,
                        };
                    }
                }
            }
        }

        if (gameConfiguration.GradualRollout is not null)
        {
            packageConfiguration.GradualRolloutInfo = gameConfiguration.GradualRollout;
        }

        var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, IGamePackageBranch originPackageBranch, IGamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, CancellationToken ct)
    {
        return await ImportPackagesAsync(product, originPackageBranch, destinationPackageBranch, marketGroupName, overwrite, null, ct);
    }

    public async Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, IGamePackageBranch originPackageBranch, IGamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, IGameConfiguration gameConfiguration, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(originPackageBranch);
        ArgumentNullException.ThrowIfNull(destinationPackageBranch);

        if (string.Equals(originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{nameof(originPackageBranch)} cannot be equal to {nameof(destinationPackageBranch)}.", nameof(originPackageBranch));
        }

        _logger.LogDebug("Importing game packages in product id '{productId}' from draft id '{originCurrentDraftInstanceID}' to draft id '{destinationCurrentDraftInstanceID}'. Overwriting: {overwrite}.", product.ProductId, originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, overwrite);

        var originPackageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, originPackageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);
        var destinationPackageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, destinationPackageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

        // Importing packages from originMarketGroupPackage to destinationPackageConfiguration
        if (originPackageConfiguration.MarketGroupPackages is not null && originPackageConfiguration.MarketGroupPackages.Count > 0)
        {
            // initializing MarketGroupPackages if needed
            destinationPackageConfiguration.MarketGroupPackages ??= [];

            foreach (var originMarketGroupPackage in originPackageConfiguration.MarketGroupPackages)
            {
                if (string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(originMarketGroupPackage.Name))
                {
                    var destinationMarketGroupPackage = destinationPackageConfiguration.MarketGroupPackages.SingleOrDefault(m => m.Name.Equals(originMarketGroupPackage.Name, StringComparison.OrdinalIgnoreCase));

                    // If GameMarketGroupPackage does not exist in destination, we create one
                    if (destinationMarketGroupPackage is null)
                    {
                        destinationMarketGroupPackage = new GameMarketGroupPackage
                        {
                            MarketGroupId = originMarketGroupPackage.MarketGroupId,
                            Markets = originMarketGroupPackage.Markets,
                            Name = originMarketGroupPackage.Name,
                        };
                        destinationPackageConfiguration.MarketGroupPackages.Add(destinationMarketGroupPackage);
                    }

                    if (overwrite)
                    {
                        var originalPackageAvailabilityDates =
                            destinationMarketGroupPackage.PackageAvailabilityDates is null
                                ? []
                                : new Dictionary<string, DateTime?>(destinationMarketGroupPackage.PackageAvailabilityDates);

                        destinationMarketGroupPackage.PackageIds = originMarketGroupPackage.PackageIds;
                        destinationMarketGroupPackage.PackageAvailabilityDates = originMarketGroupPackage.PackageAvailabilityDates;
                        if (destinationMarketGroupPackage.PackageAvailabilityDates is not null)
                        {
                            foreach (var (packageId, _) in destinationMarketGroupPackage.PackageAvailabilityDates)
                            {
                                if (originalPackageAvailabilityDates.TryGetValue(packageId, out var availabilityDate))
                                {
                                    // If the package was already there, we keep the availability date
                                    destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = availabilityDate;
                                }
                                else
                                {
                                    // If the package was not there, we set the availability date
                                    if (gameConfiguration?.AvailabilityDate is not null)
                                    {
                                        destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = gameConfiguration.AvailabilityDate.IsEnabled
                                            ? gameConfiguration.AvailabilityDate.EffectiveDate
                                            : null;
                                    }
                                    else
                                    {
                                        destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = null;
                                    }
                                }
                            }
                        }

                        OverWritePackageMetadata(originMarketGroupPackage, destinationMarketGroupPackage, gameConfiguration.PreDownloadDate);
                    }
                    else
                    {
                        destinationMarketGroupPackage.PackageIds ??= [];
                        var packageIdsToAdd = originMarketGroupPackage.PackageIds.Where(packageId => !destinationMarketGroupPackage.PackageIds.Contains(packageId)).ToList();
                        if (packageIdsToAdd.Count > 0)
                        {
                            destinationMarketGroupPackage.PackageIds.AddRange(packageIdsToAdd);
                            if (destinationMarketGroupPackage.PackageAvailabilityDates is not null)
                            {
                                foreach (var packageId in packageIdsToAdd)
                                {
                                    if (gameConfiguration?.AvailabilityDate is not null)
                                    {
                                        destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = gameConfiguration.AvailabilityDate.IsEnabled
                                            ? gameConfiguration.AvailabilityDate.EffectiveDate
                                            : null;
                                    }
                                    else
                                    {
                                        destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = null;
                                    }
                                }
                            }

                            if(gameConfiguration.PreDownloadDate is not null)
                            {
                                AddPackageMetadata(destinationMarketGroupPackage, packageIdsToAdd, gameConfiguration.PreDownloadDate);
                            }
                        }
                    }

                    // Setting Uwp package dates
                    if (gameConfiguration is not null)
                    {
                        if (overwrite)
                        {
                            if (gameConfiguration.AvailabilityDate is not null)
                            {
                                destinationMarketGroupPackage.AvailabilityDate = gameConfiguration.AvailabilityDate.IsEnabled
                                    ? gameConfiguration.AvailabilityDate.EffectiveDate
                                    : null;
                            }
                            if (gameConfiguration.MandatoryDate is not null)
                            {
                                destinationMarketGroupPackage.MandatoryUpdateInfo = new GameMandatoryUpdateInfo
                                {
                                    IsEnabled = gameConfiguration.MandatoryDate.IsEnabled,
                                    EffectiveDate = gameConfiguration.MandatoryDate.EffectiveDate,
                                };
                            }
                        }
                        else
                        {
                            if (gameConfiguration.AvailabilityDate is not null &&
                                !destinationMarketGroupPackage.AvailabilityDate.HasValue)
                            {
                                destinationMarketGroupPackage.AvailabilityDate =
                                    gameConfiguration.AvailabilityDate.IsEnabled
                                        ? gameConfiguration.AvailabilityDate.EffectiveDate
                                        : null;
                            }

                            if (gameConfiguration.MandatoryDate is not null &&
                                (destinationMarketGroupPackage.MandatoryUpdateInfo is null ||
                                 !destinationMarketGroupPackage.MandatoryUpdateInfo.IsEnabled))
                            {
                                destinationMarketGroupPackage.MandatoryUpdateInfo = new GameMandatoryUpdateInfo
                                {
                                    IsEnabled = gameConfiguration.MandatoryDate.IsEnabled,
                                    EffectiveDate = gameConfiguration.MandatoryDate.EffectiveDate,
                                };
                            }
                        }
                    }
                }
            }

            if (gameConfiguration?.GradualRollout is not null && (overwrite || destinationPackageConfiguration.GradualRolloutInfo is null))
            {
                destinationPackageConfiguration.GradualRolloutInfo = gameConfiguration.GradualRollout;
            }
        }

        var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, destinationPackageConfiguration, ct).ConfigureAwait(false);
        return result;
    }

    public async Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, int minutesToWaitForPublishing, CancellationToken ct)
    {
        return await PublishPackagesToSandboxAsync(product, originPackageBranch, destinationSandboxName, null, minutesToWaitForPublishing, ct).ConfigureAwait(true);
    }

    public async Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(originPackageBranch);
        StringArgumentException.ThrowIfNullOrWhiteSpace(destinationSandboxName);

        var gameSubmissionOptions = gameSubmissionConfiguration?.ToGameSubmissionOptions();
        var gameSubmission = await _ingestionHttpClient.CreateSandboxSubmissionRequestAsync(product.ProductId, originPackageBranch.CurrentDraftInstanceId, destinationSandboxName, gameSubmissionOptions, ct).ConfigureAwait(false);

        gameSubmission = await WaitForPackagePublishingAsync(product.ProductId, gameSubmission, minutesToWaitForPublishing, ct).ConfigureAwait(false);

        return gameSubmission;
    }

    public async Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, int minutesToWaitForPublishing, CancellationToken ct)
    {
        return await PublishPackagesToFlightAsync(product, gamePackageFlight, null, minutesToWaitForPublishing, ct).ConfigureAwait(false);
    }

    public async Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(gamePackageFlight);

        var gameSubmissionOptions = gameSubmissionConfiguration?.ToGameSubmissionOptions();
        var gameSubmission = await _ingestionHttpClient.CreateFlightSubmissionRequestAsync(product.ProductId, gamePackageFlight.CurrentDraftInstanceId, gamePackageFlight.Id, gameSubmissionOptions, ct).ConfigureAwait(false);

        gameSubmission = await WaitForPackagePublishingAsync(product.ProductId, gameSubmission, minutesToWaitForPublishing, ct).ConfigureAwait(false);

        return gameSubmission;
    }

    private async Task UploadAssetAsync(GameProduct product, GamePackage processingPackage, string assetFilePath, GamePackageAssetType assetType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assetFilePath))
        {
            _logger.LogWarning("No {assetType} asset file path provided, will continue to upload Package on its own.", assetType);
        }
        else
        {
            var assetFile = new FileInfo(assetFilePath);
            if (assetFile.Exists)
            {
                var packageAsset = await _ingestionHttpClient.CreatePackageAssetRequestAsync(product.ProductId, processingPackage.Id, assetFile, assetType, ct).ConfigureAwait(false);
                const bool delta = false; // Assets do not need delta upload.
                await _xfusUploader.UploadFileToXfusAsync(assetFile, packageAsset.UploadInfo, delta, ct).ConfigureAwait(false);
                await _ingestionHttpClient.CommitPackageAssetAsync(product.ProductId, processingPackage.Id, packageAsset.Id, ct).ConfigureAwait(false);
            }
            else
            {
                throw new FileNotFoundException("Package asset file not found.", assetFile.FullName);
            }
        }
    }

    private async Task<GamePackage> WaitForPackageProcessingAsync(GameProduct product, GamePackage processingPackage, int minutesToWait, int checkIntervalMinutes, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
        GamePackage internalPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, processingPackage.Id, ct).ConfigureAwait(false);
            
        var checkIntervalTimeSpan = TimeSpan.FromMinutes(checkIntervalMinutes);
        _logger.LogInformation("Will wait {minutesToWait} minute(s) for package processing, checking every {checkIntervalMinutes} minute(s).", minutesToWait, checkIntervalMinutes);

        while (internalPackage.State is GamePackageState.InProcessing or GamePackageState.Uploaded or GamePackageState.Unknown && minutesToWait > 0)
        {
            _logger.LogInformation("Package still in processing, waiting another {checkIntervalMinutes} minute(s). Will wait a further {minutesToWait} minute(s) after this.", checkIntervalMinutes, minutesToWait);

            await Task.Delay(checkIntervalTimeSpan, ct).ConfigureAwait(false);

            internalPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, internalPackage.Id, ct).ConfigureAwait(false);

            minutesToWait -= checkIntervalMinutes;
        }

        _logger.LogInformation("Package state: {packageState}", internalPackage.State switch
        {
            GamePackageState.InProcessing => "In processing",
            GamePackageState.ProcessFailed => "Process failed",
            GamePackageState.PendingUpload => "Pending upload",
            _ => internalPackage.State,
        });

        if(processingPackage.Id != internalPackage.Id)
        {
            // Needed because 301 status returns what to query but not what we're currently uploading
            processingPackage.State = internalPackage.State;
        }

        return processingPackage;
    }

    private async Task<GameSubmission> WaitForPackagePublishingAsync(string productId, GameSubmission gameSubmission, int minutesToWait, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
        gameSubmission = await _ingestionHttpClient.GetGameSubmissionAsync(productId, gameSubmission.Id, ct).ConfigureAwait(false);

        while (gameSubmission.GameSubmissionState is GameSubmissionState.InProgress && minutesToWait > 0)
        {
            _logger.LogInformation("Package still in publishing, waiting another 1 minute. Will wait a further {minutesToWait} minute(s) after this.", minutesToWait);

            await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);

            gameSubmission = await _ingestionHttpClient.GetGameSubmissionAsync(productId, gameSubmission.Id, ct).ConfigureAwait(false);

            minutesToWait -= 1;
        }

        _logger.LogInformation("Submission state: {gameSubmissionState}", gameSubmission.GameSubmissionState switch
        {
            GameSubmissionState.InProgress => "In progress",
            GameSubmissionState.NotStarted => "Not started",
            _ => gameSubmission.GameSubmissionState,
        });

        return gameSubmission;
    }

    private static void AddPackageMetadata(GameMarketGroupPackage destinationMarketGroupPackage, List<string> packageIdsToAdd, GamePackageDate preDownloadDate)
    {
        destinationMarketGroupPackage.PackageIdToMetadataMap ??= [];
        foreach (var packageId in packageIdsToAdd)
        {
            destinationMarketGroupPackage.PackageIdToMetadataMap.Add(packageId, CalculatePackageMetadata(preDownloadDate));
        }
    }

    private static void OverWritePackageMetadata(GameMarketGroupPackage originMarketGroupPackage, GameMarketGroupPackage destinationMarketGroupPackage, GamePackageDate preDownloadDate)
    {
        var originalPackageIdToMetadataMap = destinationMarketGroupPackage.PackageIdToMetadataMap ?? [];
        destinationMarketGroupPackage.PackageIdToMetadataMap = originMarketGroupPackage.PackageIdToMetadataMap;

        if (destinationMarketGroupPackage.PackageIdToMetadataMap is not null)
        {
            var destinationPkgIds = destinationMarketGroupPackage.PackageIdToMetadataMap.Keys.ToList();
            foreach (var packageId in destinationPkgIds)
            {
              destinationMarketGroupPackage.PackageIdToMetadataMap[packageId] = originalPackageIdToMetadataMap.TryGetValue(packageId, out var value) ? value : CalculatePackageMetadata(preDownloadDate);
            }
        }
    }

    private static GameMarketGroupPackageMetadata CalculatePackageMetadata(GamePackageDate preDownloadDate)
    {
        return new GameMarketGroupPackageMetadata
        {
            PreDownloadDate = preDownloadDate?.IsEnabled == true ? preDownloadDate.EffectiveDate : null,
        };
    }

  private static XvcTargetPlatform ReadXvcTargetPlatformFromMetaData(FileSystemInfo packageFile)
    {
        const int headerOffsetForXvcTargetPlatform = 1137;

        byte data;

        using (var fileStream = File.OpenRead(packageFile.FullName))
        using (var reader = new BinaryReader(fileStream))
        {
            reader.BaseStream.Seek(headerOffsetForXvcTargetPlatform, SeekOrigin.Begin);
            data = reader.ReadByte();
        }

        var xvcTargetPlatform = (XvcTargetPlatform)data;

        if (xvcTargetPlatform == XvcTargetPlatform.NotSpecified)
        {
            // Not specifying equates to Gen8.
            xvcTargetPlatform = XvcTargetPlatform.ConsoleGen8;
        }

        return xvcTargetPlatform;
    }
}