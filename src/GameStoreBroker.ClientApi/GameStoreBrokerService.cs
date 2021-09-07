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
            _ingestionHttpClient = ingestionHttpClient;
            _xfusUploader = xfusUploader;
            _logger = logger;
        }

        public async Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException($"{nameof(bigId)} cannot be null or empty.", nameof(bigId));
            }

            _logger.LogDebug("Requesting game product by BigId");
            return await _ingestionHttpClient.GetGameProductByBigIdAsync(bigId, ct).ConfigureAwait(false);
        }

        public async Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            _logger.LogDebug("Requesting game product by ProductId");
            return await _ingestionHttpClient.GetGameProductByLongIdAsync(productId, ct).ConfigureAwait(false);
        }

        public async Task<GamePackageBranch> GetPackageBranchByFlightName(GameProduct product, string flightName, CancellationToken ct)
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
            return await _ingestionHttpClient.GetPackageBranchByFlightName(product.ProductId, flightName, ct).ConfigureAwait(false);
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

        public async Task UploadUwpPackageAsync(GameProduct product, GamePackageBranch packageBranch, FileInfo packageFile, int minutesToWaitForProcessing, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (packageFile is null)
            {
                throw new ArgumentNullException(nameof(packageFile), $"{nameof(packageFile)} cannot be null.");
            }

            if (!packageFile.Exists)
            {
                throw new FileNotFoundException("Package file not found.", packageFile.FullName);
            }

            _logger.LogDebug("Creating game package for file '{fileName}', product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFile.Name, product.ProductId, packageBranch.CurrentDraftInstanceId);
            var package = await _ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, packageFile.Name, ct).ConfigureAwait(false);

            _logger.LogDebug("Uploading file '{fileName}'.", packageFile.Name);
            await _xfusUploader.UploadFileToXfusAsync(packageFile, package.UploadInfo, ct).ConfigureAwait(false);
            _logger.LogInformation("Package uploaded.");

            await _ingestionHttpClient.ProcessPackageRequestAsync(product.ProductId, package, ct).ConfigureAwait(false);
            _logger.LogInformation("Package processing.");

            await WaitForPackageProcessingAsync(product, package, minutesToWaitForProcessing, ct).ConfigureAwait(false);
        }

        public async Task UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct)
        {
            if (product is null)
            {
                throw new ArgumentNullException(nameof(product), $"{nameof(product)} cannot be null.");
            }

            if (packageBranch is null)
            {
                throw new ArgumentNullException(nameof(packageBranch), $"{nameof(packageBranch)} cannot be null.");
            }

            if (gameAssets is null)
            {
                throw new ArgumentNullException(nameof(gameAssets), $"{nameof(gameAssets)} cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(gameAssets.PackageFilePath))
            {
                throw new ArgumentException($"{nameof(gameAssets.PackageFilePath)} cannot be null or empty.", nameof(gameAssets));
            }

            var packageFile = new FileInfo(gameAssets.PackageFilePath);
            if (!packageFile.Exists)
            {
                throw new FileNotFoundException("Package file not found.", packageFile.FullName);
            }

            _logger.LogDebug("Creating game package for file '{fileName}', product id '{productId}' and draft id '{currentDraftInstanceID}'.", packageFile.Name, product.ProductId, packageBranch.CurrentDraftInstanceId);
            var package = await _ingestionHttpClient.CreatePackageRequestAsync(product.ProductId, packageBranch.CurrentDraftInstanceId, packageFile.Name, ct).ConfigureAwait(false);

            _logger.LogDebug("Uploading file '{fileName}'.", packageFile.Name);
            await _xfusUploader.UploadFileToXfusAsync(packageFile, package.UploadInfo, ct).ConfigureAwait(false);
            _logger.LogInformation("Package uploaded.");

            await _ingestionHttpClient.ProcessPackageRequestAsync(product.ProductId, package, ct).ConfigureAwait(false);
            _logger.LogInformation("Package processing.");

            await WaitForPackageProcessingAsync(product, package, minutesToWaitForProcessing, ct).ConfigureAwait(false);

            await UploadAsset(product, package, gameAssets.EkbFilePath, GamePackageAssetType.EkbFile, ct).ConfigureAwait(false);
            await UploadAsset(product, package, gameAssets.SymbolsFilePath, GamePackageAssetType.SymbolsZip, ct).ConfigureAwait(false);
            await UploadAsset(product, package, gameAssets.SubValFilePath, GamePackageAssetType.SubmissionValidatorLog, ct).ConfigureAwait(false);
            await UploadAsset(product, package, gameAssets.DiscLayoutFilePath, GamePackageAssetType.DiscLayoutFile, ct).ConfigureAwait(false);
        }

        private async Task UploadAsset(GameProduct product, GamePackage processingPackage, string assetFilePath, GamePackageAssetType assetType, CancellationToken ct)
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
                    throw new FileNotFoundException("Package file not found.", assetFile.FullName);
                }
            }
        }

        private async Task WaitForPackageProcessingAsync(GameProduct product, GamePackage processingPackage, int minutesToWait, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            processingPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, processingPackage.Id, ct).ConfigureAwait(false);

            _logger.LogInformation("Will wait {minutesToWait} minutes for package processing, checking every minute.", minutesToWait);

            while (!processingPackage.State.Equals("Processed", StringComparison.OrdinalIgnoreCase) && !processingPackage.State.Equals("ProcessFailed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Still processing, waiting another minute. Will wait a further {minutesToWait} minutes after this.", minutesToWait);

                await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);

                processingPackage = await _ingestionHttpClient.GetPackageByIdAsync(product.ProductId, processingPackage.Id, ct).ConfigureAwait(false);

                if (minutesToWait <= 0)
                {
                    break;
                }
                minutesToWait--;
            }
            
            if (processingPackage.State.Equals("Processing", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Upload complete but still processing in Partner Center.");
            }
            else
            {
                _logger.LogInformation("Finished uploading and processing.");
                _logger.LogInformation($"Processed state: {processingPackage.State}");
            }
        }
    }
}