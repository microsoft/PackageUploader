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
            _logger = logger;
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

            var ingestionGameProducts = await GetAsync<PagedCollection<IngestionGameProduct>>($"products?externalId={bigId}", ct).ConfigureAwait(false);
            var ingestionGameProduct = ingestionGameProducts.Value.FirstOrDefault();

            if (ingestionGameProduct is null)
            {
                throw new ProductNotFoundException($"Product with big id {bigId} not found.");
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

            var branches = await GetAsync<PagedCollection<IngestionBranch>>($"products/{productId}/branches/getByModule(module=Package)", ct).ConfigureAwait(false);

            var ingestionGamePackageBranch = branches.Value.FirstOrDefault(b => b.FriendlyName is not null && b.FriendlyName.Equals(branchFriendlyName, StringComparison.OrdinalIgnoreCase));

            if (ingestionGamePackageBranch is null)
            {
                throw new PackageBranchNotFoundException($"404 branch not found: {branchFriendlyName}");
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

            var flights = await GetAsync<PagedCollection<IngestionFlight>>($"products/{productId}/flights", ct);

            var selectedFlight = flights.Value.FirstOrDefault(f => f.Name is not null && f.Name.Equals(flightName, StringComparison.OrdinalIgnoreCase));

            if (selectedFlight is null)
            {
                throw new PackageBranchNotFoundException($"404 flight not found: {flightName}");
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

            var package = await GetAsync<GamePackage>($"products/{productId}/packages/{packageId}", ct).ConfigureAwait(false);
            return package;
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

        public async Task RemovePackagesAsync(string productId, string currentDraftInstanceId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
            }

            if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
            {
                throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
            }

            throw new NotImplementedException();
        }
    }
}
