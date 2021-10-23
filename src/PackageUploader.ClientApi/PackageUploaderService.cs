﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus;
using PackageUploader.ClientApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi
{
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
            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException($"{nameof(bigId)} cannot be null or empty.", nameof(bigId));
            }

            _logger.LogDebug("Requesting game product by BigId '{bigId}'.", bigId);
            return await _ingestionHttpClient.GetGameProductByBigIdAsync(bigId, ct).ConfigureAwait(false);
        }

        public async Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            _logger.LogDebug("Requesting game product by ProductId '{productId}'.", productId);
            return await _ingestionHttpClient.GetGameProductByLongIdAsync(productId, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(flightName))
            {
                throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
            }

            _logger.LogDebug("Requesting game package branch by flight name '{flightName}'.", flightName);
            return await _ingestionHttpClient.GetPackageBranchByFlightNameAsync(product.ProductId, flightName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(branchFriendlyName))
            {
                throw new ArgumentException($"{nameof(branchFriendlyName)} cannot be null or empty.", nameof(branchFriendlyName));
            }

            _logger.LogDebug("Requesting game package branch by branch friendly name '{branchFriendlyName}'.", branchFriendlyName);
            return await _ingestionHttpClient.GetPackageBranchByFriendlyNameAsync(product.ProductId, branchFriendlyName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(flightName))
            {
                throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
            }

            _logger.LogDebug("Requesting game package flight by flight name '{flightName}'.", flightName);
            return await _ingestionHttpClient.GetPackageFlightByFlightNameAsync(product.ProductId, flightName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageConfiguration> GetPackageConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageBranch ?? throw new ArgumentNullException(nameof(packageBranch));

            _logger.LogDebug("Requesting game package configuration by product id '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);

            var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

            if (packageConfiguration.MarketGroupPackages is null || !packageConfiguration.MarketGroupPackages.Any())
            {
                _logger.LogDebug("Initializing game package configuration in branch '{branchName}'.", packageConfiguration.BranchName);
                packageConfiguration.MarketGroupPackages = new List<GameMarketGroupPackage>
                {
                    new()
                    {
                        MarketGroupId = "default",
                        Name = "default",
                        Markets = null,
                        PackageIds = new List<string>(),
                        AvailabilityDate = null,
                        PackageAvailabilityDates = new Dictionary<string, DateTime?>(),
                        MandatoryUpdateInfo = null,
                    },
                };
                packageConfiguration = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            }

            return packageConfiguration;
        }

        public async Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(GameProduct product, GamePackageConfiguration packageConfiguration, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageConfiguration ?? throw new ArgumentNullException(nameof(packageConfiguration));

            _logger.LogDebug("Updating game package configuration in product id '{productId}' and package configuration id '{packageConfigurationId}'.", product.ProductId, packageConfiguration.Id);

            var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            return result;
        }

        public async Task<GamePackage> UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, GameMarketGroupPackage marketGroupPackage, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageBranch ?? throw new ArgumentNullException(nameof(packageBranch));
            _ = marketGroupPackage ?? throw new ArgumentNullException(nameof(marketGroupPackage));

            if (string.IsNullOrWhiteSpace(packageFilePath))
            {
                throw new ArgumentException($"{nameof(packageFilePath)} cannot be null or empty.", nameof(packageFilePath));
            }

            var packageFile = new FileInfo(packageFilePath);
            if (!packageFile.Exists)
            {
                throw new FileNotFoundException("Package file not found.", packageFile.FullName);
            }

            _logger.LogDebug("Creating game package for file '{fileName}', product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFile.Name, product.ProductId, packageBranch.CurrentDraftInstanceId);
            var package = await _ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, packageFile.Name, marketGroupPackage.MarketGroupId, ct).ConfigureAwait(false);

            _logger.LogDebug("Uploading file '{fileName}'.", packageFile.Name);
            await _xfusUploader.UploadFileToXfusAsync(packageFile, package.UploadInfo, ct).ConfigureAwait(false);
            _logger.LogDebug("Package file '{fileName}' uploaded.", packageFile.Name);

            package = await _ingestionHttpClient.ProcessPackageRequestAsync(product.ProductId, package, ct).ConfigureAwait(false);
            _logger.LogInformation("Package is uploaded and is in processing.");

            package = await WaitForPackageProcessingAsync(product, package, minutesToWaitForProcessing, 1, ct).ConfigureAwait(false);

            if (gameAssets is not null)
            {
                await UploadAssetAsync(product, package, gameAssets.EkbFilePath, GamePackageAssetType.EkbFile, ct).ConfigureAwait(false);
                await UploadAssetAsync(product, package, gameAssets.SymbolsFilePath, GamePackageAssetType.SymbolsZip, ct).ConfigureAwait(false);
                await UploadAssetAsync(product, package, gameAssets.SubValFilePath, GamePackageAssetType.SubmissionValidatorLog, ct).ConfigureAwait(false);
                await UploadAssetAsync(product, package, gameAssets.DiscLayoutFilePath, GamePackageAssetType.DiscLayoutFile, ct).ConfigureAwait(false);
            }

            return package;
        }

        public async Task<GamePackageConfiguration> RemovePackagesAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupName, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageBranch ?? throw new ArgumentNullException(nameof(packageBranch));

            _logger.LogDebug("Removing game packages in product id '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);

            var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

            // Blanking all package ids for each market group package
            if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Any())
            {
                if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
                {
                    _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in branch '{branchName}'.", marketGroupName, packageBranch.Name);
                }
                else
                {
                    foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages)
                    {
                        if (string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name))
                        {
                            marketGroupPackage.PackageIds = new List<string>();
                            marketGroupPackage.PackageAvailabilityDates = new Dictionary<string, DateTime?>();
                        }
                    }
                }
            }

            var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            return result;
        }

        public async Task<GamePackageConfiguration> SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupName, GamePackageDate availabilityDate, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageBranch ?? throw new ArgumentNullException(nameof(packageBranch));
            _ = gamePackage ?? throw new ArgumentNullException(nameof(gamePackage));
            _ = availabilityDate ?? throw new ArgumentNullException(nameof(availabilityDate));

            _logger.LogDebug("Setting the availability date to package with id '{gamePackageId}' in '{productId}' and draft id '{currentDraftInstanceID}'.", gamePackage.Id, product.ProductId, packageBranch.CurrentDraftInstanceId);

            var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

            // Setting the availability date
            if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Any())
            {
                if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
                {
                    _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in branch '{branchName}'.", marketGroupName, packageBranch.Name);
                }
                else
                {
                    foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages)
                    {
                        if (marketGroupPackage.PackageIds.Contains(gamePackage.Id))
                        {
                            if (string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name))
                            {
                                if (availabilityDate.IsEnabled)
                                {
                                    if (marketGroupPackage.PackageAvailabilityDates is null)
                                    {
                                        marketGroupPackage.PackageAvailabilityDates = new Dictionary<string, DateTime?>();
                                    }
                                    marketGroupPackage.PackageAvailabilityDates[gamePackage.Id] = availabilityDate.EffectiveDate;
                                }
                                else if (marketGroupPackage.PackageAvailabilityDates is not null)
                                {
                                    marketGroupPackage.PackageAvailabilityDates[gamePackage.Id] = null;
                                }
                            }
                        }
                    }
                }
            }

            var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            return result;
        }

        public async Task<GamePackageConfiguration> SetUwpConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupName, IGameConfiguration gameConfiguration, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = packageBranch ?? throw new ArgumentNullException(nameof(packageBranch));
            _ = gameConfiguration ?? throw new ArgumentNullException(nameof(gameConfiguration));

            _logger.LogDebug("Setting the package dates in '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);

            var packageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

            if (packageConfiguration.MarketGroupPackages is not null && packageConfiguration.MarketGroupPackages.Any())
            {
                if (!string.IsNullOrWhiteSpace(marketGroupName) && !packageConfiguration.MarketGroupPackages.Any(x => x.Name.Equals(marketGroupName)))
                {
                    _logger.LogWarning("Market Group '{marketGroupName}' (case sensitive) not found in branch '{branchName}'.", marketGroupName, packageBranch.Name);
                }
                else
                {
                    foreach (var marketGroupPackage in packageConfiguration.MarketGroupPackages)
                    {
                        if (string.IsNullOrWhiteSpace(marketGroupName) || marketGroupName.Equals(marketGroupPackage.Name))
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
            }

            if (gameConfiguration.GradualRollout is not null)
            {
                packageConfiguration.GradualRolloutInfo = gameConfiguration.GradualRollout;
            }

            var result = await _ingestionHttpClient.UpdatePackageConfigurationAsync(product.ProductId, packageConfiguration, ct).ConfigureAwait(false);
            return result;
        }

        public async Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, CancellationToken ct)
        {
            return await ImportPackagesAsync(product, originPackageBranch, destinationPackageBranch, marketGroupName, overwrite, null, ct);
        }

        public async Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, IGameConfiguration gameConfiguration, CancellationToken ct)
        {
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = originPackageBranch ?? throw new ArgumentNullException(nameof(originPackageBranch));
            _ = destinationPackageBranch ?? throw new ArgumentNullException(nameof(destinationPackageBranch));

            if (string.Equals(originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{nameof(originPackageBranch)} cannot be equal to {nameof(destinationPackageBranch)}.", nameof(originPackageBranch));
            }

            _logger.LogDebug("Importing game packages in product id '{productId}' from draft id '{originCurrentDraftInstanceID}' to draft id '{destinationCurrentDraftInstanceID}'. Overwriting: {overwrite}.", product.ProductId, originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, overwrite);

            var originPackageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, originPackageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);
            var destinationPackageConfiguration = await _ingestionHttpClient.GetPackageConfigurationAsync(product.ProductId, destinationPackageBranch.CurrentDraftInstanceId, ct).ConfigureAwait(false);

            // Importing packages from originMarketGroupPackage to destinationPackageConfiguration
            if (originPackageConfiguration.MarketGroupPackages is not null && originPackageConfiguration.MarketGroupPackages.Any())
            {
                // initializing MarketGroupPackages if needed
                destinationPackageConfiguration.MarketGroupPackages ??= new List<GameMarketGroupPackage>();

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
                                    ? new Dictionary<string, DateTime?>()
                                    : new Dictionary<string, DateTime?>(destinationMarketGroupPackage.PackageAvailabilityDates);

                            destinationMarketGroupPackage.PackageIds = originMarketGroupPackage.PackageIds;
                            destinationMarketGroupPackage.PackageAvailabilityDates = originMarketGroupPackage.PackageAvailabilityDates;
                            if (destinationMarketGroupPackage.PackageAvailabilityDates is not null)
                            {
                                foreach (var (packageId, _) in destinationMarketGroupPackage.PackageAvailabilityDates)
                                {
                                    if (originalPackageAvailabilityDates.ContainsKey(packageId))
                                    {
                                        // If the package was already there, we keep the availability date
                                        destinationMarketGroupPackage.PackageAvailabilityDates[packageId] = originalPackageAvailabilityDates[packageId];
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
                        }
                        else
                        {
                            var packageIdsToAdd = originMarketGroupPackage.PackageIds.Where(packageId => !destinationMarketGroupPackage.PackageIds.Contains(packageId)).ToList();
                            if (packageIdsToAdd.Any())
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
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = originPackageBranch ?? throw new ArgumentNullException(nameof(originPackageBranch));

            if (string.IsNullOrWhiteSpace(destinationSandboxName))
            {
                throw new ArgumentException($"{nameof(destinationSandboxName)} cannot be null or empty.", nameof(destinationSandboxName));
            }

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
            _ = product ?? throw new ArgumentNullException(nameof(product));
            _ = gamePackageFlight ?? throw new ArgumentNullException(nameof(gamePackageFlight));

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
                    await _xfusUploader.UploadFileToXfusAsync(assetFile, packageAsset.UploadInfo, ct).ConfigureAwait(false);
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
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            processingPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, processingPackage.Id, ct).ConfigureAwait(false);
            
            var checkIntervalTimeSpan = TimeSpan.FromMinutes(checkIntervalMinutes);
            _logger.LogInformation("Will wait {minutesToWait} minute(s) for package processing, checking every {checkIntervalMinutes} minute(s).", minutesToWait, checkIntervalMinutes);

            while (processingPackage.State is GamePackageState.InProcessing or GamePackageState.Uploaded or GamePackageState.Unknown && minutesToWait > 0)
            {
                _logger.LogInformation("Package still in processing, waiting another {checkIntervalMinutes} minute(s). Will wait a further {minutesToWait} minute(s) after this.", checkIntervalMinutes, minutesToWait);

                await Task.Delay(checkIntervalTimeSpan, ct).ConfigureAwait(false);

                processingPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, processingPackage.Id, ct).ConfigureAwait(false);

                minutesToWait -= checkIntervalMinutes;
            }

            _logger.LogInformation(processingPackage.State switch
            {
                GamePackageState.InProcessing => "Package still in processing.",
                GamePackageState.Processed => "Package processed.",
                _ => $"Package state: {processingPackage.State}",
            });

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

            _logger.LogInformation(gameSubmission.GameSubmissionState switch
            {
                GameSubmissionState.Failed => "Failed to publish.",
                GameSubmissionState.Published => "Game published.",
                GameSubmissionState.InProgress => "Publish still in progress.",
                _ => $"Submission state: {gameSubmission.GameSubmissionState}",
            });

            return gameSubmission;
        }
    }
}
