// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Client;
using GameStoreBroker.ClientApi.Client.Ingestion.Exceptions;
using GameStoreBroker.ClientApi.Client.Ingestion.Extensions;
using GameStoreBroker.ClientApi.Client.Ingestion.Mappers;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("GameStoreBroker.ClientApi.Test")]
namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    internal sealed class IngestionHttpClient : HttpRestClient, IIngestionHttpClient
    {
        private readonly ILogger<IngestionHttpClient> _logger;

        public IngestionHttpClient(ILogger<IngestionHttpClient> logger, HttpClient httpClient) : base(logger, httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(longId))
            {
                throw new ArgumentException($"{nameof(longId)} cannot be null or empty.", nameof(longId));
            }

            try
            {
                var ingestionGameProduct = await GetAsync<IngestionGameProduct>($"products/{longId}", ct).ConfigureAwait(false);

                var gameProduct = ingestionGameProduct.Map();
                return gameProduct;
            }
            catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
            {
                throw new ProductNotFoundException($"Product with product id '{longId}' not found.", e);
            }
        }

        public async Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException($"{nameof(bigId)} cannot be null or empty.", nameof(bigId));
            }

            var ingestionGameProducts = GetAsyncEnumerable<IngestionGameProduct>($"products?externalId={bigId}", ct);
            var ingestionGameProduct = await ingestionGameProducts.FirstOrDefaultAsync(ct).ConfigureAwait(false);

            if (ingestionGameProduct is null)
            {
                throw new ProductNotFoundException($"Product with big id '{bigId}' not found.");
            }

            var gameProduct = ingestionGameProduct.Map();
            return gameProduct;
        }

        public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(branchFriendlyName))
            {
                throw new ArgumentException($"{nameof(branchFriendlyName)} cannot be null or empty.", nameof(branchFriendlyName));
            }

            var branches = GetAsyncEnumerable<IngestionBranch>($"products/{productId}/branches/getByModule(module=Package)", ct);

            var ingestionGamePackageBranch = await branches.FirstOrDefaultAsync(b => b.FriendlyName is not null && b.FriendlyName.Equals(branchFriendlyName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false);

            if (ingestionGamePackageBranch is null)
            {
                throw new PackageBranchNotFoundException($"Package branch with friendly name '{branchFriendlyName}' not found.");
            }

            var gamePackageBranch = ingestionGamePackageBranch.Map();
            return gamePackageBranch;
        }

        public async Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(string productId, string flightName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(flightName))
            {
                throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
            }

            var flights = GetAsyncEnumerable<IngestionFlight>($"products/{productId}/flights", ct);

            var selectedFlight = await flights.FirstOrDefaultAsync(f => f.Name is not null && f.Name.Equals(flightName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false);

            if (selectedFlight is null)
            {
                throw new PackageBranchNotFoundException($"Package branch with flight name '{flightName}' not found.");
            }

            var branch = await GetPackageBranchByFriendlyNameAsync(productId, selectedFlight.Id, ct).ConfigureAwait(false);
            return branch;
        }

        public async Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, string marketGroupId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"{nameof(fileName)} cannot be null or empty.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            var body = new IngestionPackageCreationRequest
            {
                PackageConfigurationId = currentDraftInstanceId,
                FileName = fileName,
                ResourceType = "PackageCreationRequest",
                MarketGroupId = marketGroupId,
            };

            var ingestionGamePackage = await PostAsync<IngestionPackageCreationRequest, IngestionGamePackage>($"products/{productId}/packages", body, ct).ConfigureAwait(false);

            if (!ingestionGamePackage.State.Equals("PendingUpload", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Package request in Partner Center is not 'PendingUpload'.");
            }

            _logger.LogInformation("Package id: {packageId}", ingestionGamePackage.Id);

            var gamePackage = ingestionGamePackage.Map();
            return gamePackage;
        }

        public async Task<GamePackage> GetPackageByIdAsync(string productId, string packageId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
            }

            var ingestionGamePackage = await GetAsync<IngestionGamePackage>($"products/{productId}/packages/{packageId}", ct).ConfigureAwait(false);

            var gamePackage = ingestionGamePackage.Map();
            return gamePackage;
        }

        public async Task<GamePackageAsset> CreatePackageAssetRequestAsync(string productId, string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType, CancellationToken ct)
        {
            var body = new IngestionGamePackageAsset
            {
                PackageId = packageId,
                Type = packageAssetType.GetGamePackageAssetType(),
                ResourceType = "PackageAsset",
                FileName = fileInfo.Name,
                BinarySizeInBytes = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                Name = fileInfo.Name,
            };

            var ingestionGamePackageAsset = await PostAsync($"products/{productId}/packages/{packageId}/packageAssets", body, ct).ConfigureAwait(false);

            var gamePackageAsset = ingestionGamePackageAsset.Map();
            return gamePackageAsset;
        }

        public async Task<GamePackage> ProcessPackageRequestAsync(string productId, GamePackage gamePackage, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (gamePackage is null)
            {
                throw new ArgumentNullException(nameof(gamePackage), $"{nameof(gamePackage)} cannot be null.");
            }

            var body = new IngestionGamePackage
            {
                Id = gamePackage.Id,
                State = "Uploaded",
                ResourceType = "GamePackage",
                ETag = gamePackage.ODataETag,
                ODataETag = gamePackage.ODataETag
            };

            var ingestionGamePackage = await PutAsync($"products/{productId}/packages/{gamePackage.Id}", body, ct);
            var newGamePackage = ingestionGamePackage.Map();
            return newGamePackage;
        }

        public async Task<GamePackageAsset> CommitPackageAssetAsync(string productId, string packageId, string packageAssetId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(packageAssetId))
            {
                throw new ArgumentException($"{nameof(packageAssetId)} cannot be null or empty.", nameof(packageAssetId));
            }

            var body = new IngestionGamePackageAsset();

            var ingestionGamePackageAsset = await PutAsync($"products/{productId}/packages/{packageId}/packageAssets/{packageAssetId}/commit", body, ct).ConfigureAwait(false);

            var gamePackageAsset = ingestionGamePackageAsset.Map();
            return gamePackageAsset;
        }

        public async Task RemovePackagesAsync(string productId, string currentDraftInstanceId, string marketGroupId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            var packageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", ct);

            var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (packageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
            }
            
            if (packageSet.MarketGroupPackages is not null && packageSet.MarketGroupPackages.Any())
            {
                foreach (var marketGroupPackage in packageSet.MarketGroupPackages)
                {
                    if (string.IsNullOrWhiteSpace(marketGroupId) || string.Equals(marketGroupPackage.MarketGroupId, marketGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Blanking all package ids for each market group package
                        marketGroupPackage.PackageIds = new List<string>();
                        marketGroupPackage.PackageAvailabilityDates = new Dictionary<string, DateTime?>();
                    }
                }
            }

            await PutPackageSetAsync(productId, packageSet, ct).ConfigureAwait(false);
        }

        public async Task SetAvailabilityDateXvcPackage(string productId, string currentDraftInstanceId, string marketGroupId, string packageId, DateTime? availabilityDate, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            var packageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", ct);

            var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (packageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
            }

            if (packageSet.MarketGroupPackages is not null && packageSet.MarketGroupPackages.Any())
            {
                foreach (var marketGroupPackage in packageSet.MarketGroupPackages)
                {
                    if (string.Equals(marketGroupPackage.MarketGroupId, marketGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Setting the availability date
                        if (marketGroupPackage.PackageAvailabilityDates is null)
                        {
                            marketGroupPackage.PackageAvailabilityDates = new Dictionary<string, DateTime?>();
                        }
                        marketGroupPackage.PackageAvailabilityDates[packageId] = availabilityDate;
                    }
                }
            }

            await PutPackageSetAsync(productId, packageSet, ct).ConfigureAwait(false);
        }

        public async Task SetAvailabilityDateUwpPackage(string productId, string currentDraftInstanceId, string marketGroupId, DateTime? availabilityDate, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            var packageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", ct);

            var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (packageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
            }

            if (packageSet.MarketGroupPackages is not null && packageSet.MarketGroupPackages.Any())
            {
                foreach (var marketGroupPackage in packageSet.MarketGroupPackages)
                {
                    if (string.Equals(marketGroupPackage.MarketGroupId, marketGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Setting the availability date
                        marketGroupPackage.AvailabilityDate = availabilityDate;
                    }
                }
            }

            await PutPackageSetAsync(productId, packageSet, ct).ConfigureAwait(false);
        }

        public async Task SetMandatoryDateUwpPackage(string productId, string currentDraftInstanceId, string marketGroupId, DateTime? mandatoryDate, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            if (string.IsNullOrWhiteSpace(marketGroupId))
            {
                throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
            }

            var packageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", ct);

            var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (packageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
            }

            if (packageSet.MarketGroupPackages is not null && packageSet.MarketGroupPackages.Any())
            {
                foreach (var marketGroupPackage in packageSet.MarketGroupPackages)
                {
                    if (string.Equals(marketGroupPackage.MarketGroupId, marketGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Setting the mandatory date
                        marketGroupPackage.MandatoryUpdateInfo = new IngestionMandatoryUpdateInfo
                        {
                            EffectiveDate = mandatoryDate,
                            IsEnabled = mandatoryDate is not null,
                        };
                    }
                }
            }

            await PutPackageSetAsync(productId, packageSet, ct).ConfigureAwait(false);
        }

        public async Task ImportPackages(string productId, string originCurrentDraftInstanceId, string destinationCurrentDraftInstanceId, string marketGroupId, bool overwrite, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(originCurrentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(originCurrentDraftInstanceId)} cannot be null or empty.", nameof(originCurrentDraftInstanceId));
            }

            if (string.IsNullOrWhiteSpace(destinationCurrentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(destinationCurrentDraftInstanceId)} cannot be null or empty.", nameof(destinationCurrentDraftInstanceId));
            }

            if (string.Equals(originCurrentDraftInstanceId, destinationCurrentDraftInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"{nameof(originCurrentDraftInstanceId)} cannot be equal to {nameof(originCurrentDraftInstanceId)}.", nameof(originCurrentDraftInstanceId));
            }

            var originPackageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={originCurrentDraftInstanceId})", ct);

            var originPackageSet = await originPackageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (originPackageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{originCurrentDraftInstanceId}' not found.");
            }

            var destinationPackageSets = GetAsyncEnumerable<IngestionPackageSet>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={destinationCurrentDraftInstanceId})", ct);

            var destinationPackageSet = await destinationPackageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (destinationPackageSet is null)
            {
                throw new PackageSetNotFoundException($"Package set for product '{productId}' and currentDraftInstanceId '{destinationCurrentDraftInstanceId}' not found.");
            }

            if (originPackageSet.MarketGroupPackages is not null && originPackageSet.MarketGroupPackages.Any())
            {
                foreach (var originMarketGroupPackage in originPackageSet.MarketGroupPackages)
                {
                    if (string.IsNullOrWhiteSpace(marketGroupId) || string.Equals(originMarketGroupPackage.MarketGroupId, marketGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        var destinationMarketGroupPackage = destinationPackageSet.MarketGroupPackages.SingleOrDefault(m => string.Equals(m.MarketGroupId, originMarketGroupPackage.MarketGroupId));
                        if (destinationMarketGroupPackage is not null)
                        {
                            if (overwrite)
                            {
                                var originalPackageAvailabilityDates = destinationMarketGroupPackage.PackageAvailabilityDates;
                                destinationMarketGroupPackage.PackageIds = originMarketGroupPackage.PackageIds;
                                destinationMarketGroupPackage.PackageAvailabilityDates = originMarketGroupPackage.PackageAvailabilityDates;
                                foreach (var (packageId, _) in destinationMarketGroupPackage.PackageAvailabilityDates)
                                {
                                    if (originalPackageAvailabilityDates.ContainsKey(packageId))
                                    {
                                        // If the package was already there, we keep the availability date
                                        originMarketGroupPackage.PackageAvailabilityDates[packageId] = originalPackageAvailabilityDates[packageId];
                                    }
                                    else
                                    {
                                        // If the package was not there, we blank the availability date
                                        originMarketGroupPackage.PackageAvailabilityDates[packageId] = null;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var packageId in originMarketGroupPackage.PackageIds.Where(packageId => !destinationMarketGroupPackage.PackageIds.Contains(packageId)))
                                {
                                    destinationMarketGroupPackage.PackageIds.Add(packageId);
                                }
                                foreach (var (packageId, _) in originMarketGroupPackage.PackageAvailabilityDates)
                                {
                                    if (!originMarketGroupPackage.PackageAvailabilityDates.ContainsKey(packageId))
                                    {
                                        originMarketGroupPackage.PackageAvailabilityDates[packageId] = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            await PutPackageSetAsync(productId, destinationPackageSet, ct).ConfigureAwait(false);
        }

        private async Task PutPackageSetAsync(string productId, IngestionPackageSet packageSet, CancellationToken ct)
        {
            packageSet.ETag = packageSet.ODataETag;

            // ODataEtag needs to be added to If-Match header on http client still.
            var customHeaders = new Dictionary<string, string>
            {
                { "If-Match", packageSet.ODataETag},
            };

            await PutAsync($"products/{productId}/packageConfigurations/{packageSet.Id}", packageSet, customHeaders, ct).ConfigureAwait(false);
        }
    }
}
