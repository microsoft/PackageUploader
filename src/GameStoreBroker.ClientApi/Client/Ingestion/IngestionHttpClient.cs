// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Builders;
using GameStoreBroker.ClientApi.Client.Ingestion.Client;
using GameStoreBroker.ClientApi.Client.Ingestion.Exceptions;
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

            var body = new IngestionPackageCreationRequestBuilder(currentDraftInstanceId, fileName, marketGroupId).Build();

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
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
            }

            if (fileInfo is null)
            {
                throw new ArgumentNullException(nameof(fileInfo), $"{nameof(fileInfo)} cannot be null.");
            }

            var body = new IngestionGamePackageAssetBuilder(packageId, fileInfo, packageAssetType).Build();

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

            gamePackage.State = GamePackageState.Uploaded;
            var body = gamePackage.Map();

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

        public async Task<GamePackageConfiguration> GetPackageConfigurationAsync(string productId, string currentDraftInstanceId, CancellationToken ct)
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
                throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
            }

            var gamePackageConfiguration = packageSet.Map();
            return gamePackageConfiguration;
        }

        public async Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(string productId, GamePackageConfiguration gamePackageConfiguration, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (gamePackageConfiguration is null)
            {
                throw new ArgumentNullException(nameof(gamePackageConfiguration), $"{nameof(gamePackageConfiguration)} cannot be null.");
            }

            var packageSet = await GetAsync<IngestionPackageSet>($"products/{productId}/packageConfigurations/{gamePackageConfiguration.Id}", ct);
            
            if (packageSet is null)
            {
                throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and packageConfigurationId '{gamePackageConfiguration.Id}' not found.");
            }

            var newPackageSet = packageSet.Merge(gamePackageConfiguration);

            // ODataEtag needs to be added to If-Match header on http client still.
            var customHeaders = new Dictionary<string, string>
            {
                { "If-Match", packageSet.ODataETag},
            };

            var result = await PutAsync($"products/{productId}/packageConfigurations/{newPackageSet.Id}", packageSet, customHeaders, ct).ConfigureAwait(false);
            return result.Map();
        }
    }
}
