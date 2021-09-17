// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Client.Xfus;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public class GameStoreBrokerService : IGameStoreBrokerService
    {
        private readonly IIngestionHttpClient _ingestionHttpClient;
        private readonly IXfusUploader _xfusUploader;
        private readonly ILogger<GameStoreBrokerService> _logger;

        public GameStoreBrokerService(IIngestionHttpClient ingestionHttpClient, IXfusUploader xfusUploader, ILogger<GameStoreBrokerService> logger)
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
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(flightName))
            {
                throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
            }

            _logger.LogDebug("Requesting game package branch by flight name '{flightName}'.", flightName);
            return await _ingestionHttpClient.GetPackageBranchByFlightNameAsync(product.ProductId, flightName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(branchFriendlyName))
            {
                throw new ArgumentException($"{nameof(branchFriendlyName)} cannot be null or empty.", nameof(branchFriendlyName));
            }

            _logger.LogDebug("Requesting game package branch by branch friendly name '{branchFriendlyName}'.", branchFriendlyName);
            return await _ingestionHttpClient.GetPackageBranchByFriendlyNameAsync(product.ProductId, branchFriendlyName, ct).ConfigureAwait(false);
        }

        public async Task<GamePackage> UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

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
            var package = await _ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, packageFile.Name, marketGroupId, ct).ConfigureAwait(false);

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

        public async Task RemovePackagesAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            _logger.LogDebug("Removing game packages in product id '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);
            await _ingestionHttpClient.RemovePackagesAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, marketGroupId, ct).ConfigureAwait(false);
        }

        public async Task SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupId, DateTime? availabilityDate, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (gamePackage is null)
            {
                throw new ArgumentNullException(nameof(gamePackage), $"{nameof(gamePackage)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            _logger.LogDebug("Setting the availability date to package with id '{gamePackageId}' in '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId, gamePackage.Id);
            await _ingestionHttpClient.SetAvailabilityDateXvcPackage(product.ProductId, packageBranch.CurrentDraftInstanceId, marketGroupId, gamePackage.Id, availabilityDate, ct).ConfigureAwait(false);
        }

        public async Task SetUwpAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, DateTime? availabilityDate, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            _logger.LogDebug("Setting the availability date in '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);
            await _ingestionHttpClient.SetAvailabilityDateUwpPackage(product.ProductId, packageBranch.CurrentDraftInstanceId, marketGroupId, availabilityDate, ct).ConfigureAwait(false);
        }

        public async Task SetUwpMandatoryDateAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, DateTime? mandatoryDate, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            _logger.LogDebug("Setting the mandatory date in '{productId}' and draft id '{currentDraftInstanceID}'.", product.ProductId, packageBranch.CurrentDraftInstanceId);
            await _ingestionHttpClient.SetMandatoryDateUwpPackage(product.ProductId, packageBranch.CurrentDraftInstanceId, marketGroupId, mandatoryDate, ct).ConfigureAwait(false);
        }

        public async Task ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupId, bool overwrite, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (originPackageBranch is null)
            {
                throw new ArgumentNullException(nameof(originPackageBranch), $"{nameof(originPackageBranch)} cannot be null.");
            }

            if (destinationPackageBranch is null)
            {
                throw new ArgumentNullException(nameof(destinationPackageBranch), $"{nameof(destinationPackageBranch)} cannot be null.");
            }

            if (string.Equals(originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{nameof(originPackageBranch)} cannot be equal to {nameof(destinationPackageBranch)}.", nameof(originPackageBranch));
            }

            _logger.LogDebug("Importing game packages in product id '{productId}' from draft id '{originCurrentDraftInstanceID}' to draft id '{destinationCurrentDraftInstanceID}'. Overwriting: {overwrite}.", product.ProductId, originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, overwrite);
            await _ingestionHttpClient.ImportPackages(product.ProductId, originPackageBranch.CurrentDraftInstanceId, destinationPackageBranch.CurrentDraftInstanceId, marketGroupId, overwrite, ct).ConfigureAwait(false);
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
    }
}