// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Exceptions;
using GameStoreBroker.ClientApi.Mappers;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public async Task Authorize(IAccessTokenProvider accessTokenProvider, CancellationToken ct)
        {
            var accessToken = await accessTokenProvider.GetAccessToken(ct);
            SetAuthorizationHeader(new AuthenticationHeaderValue("Bearer", accessToken));
        }

        public async Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct)
        {
            if (longId == null)
            {
                throw new ArgumentNullException(nameof(longId));
            }

            if (string.IsNullOrWhiteSpace(longId))
            {
                throw new ArgumentException(null, nameof(longId));
            }

            try
            {
                var ingestionGameProduct = await GetAsync<IngestionGameProduct>($"products/{longId}", ct).ConfigureAwait(false);

                var gameProduct = ingestionGameProduct.Map();
                return gameProduct;
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ProductNotFoundException($"Product with product id '{longId}' not found.", e);
            }
        }

        public async Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct)
        {
            if (bigId == null)
            {
                throw new ArgumentNullException(nameof(bigId));
            }

            if (string.IsNullOrWhiteSpace(bigId))
            {
                throw new ArgumentException(null, nameof(bigId));
            }

            var ingestionGameProducts = await GetAsync<PagedCollection<IngestionGameProduct>>($"products?externalId={bigId}", ct).ConfigureAwait(false);
            var ingestionGameProduct = ingestionGameProducts.Value.FirstOrDefault();

            if (ingestionGameProduct == null)
            {
                throw new ProductNotFoundException($"Product with big id {bigId} not found.");
            }

            var gameProduct = ingestionGameProduct.Map();
            return gameProduct;
        }

        public async Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, CancellationToken ct)
        {
            var body = new IngestionPackageCreationRequest
            {
                PackageConfigurationId = currentDraftInstanceId,
                FileName = fileName,
                ResourceType = "PackageCreationRequest",
                MarketGroupId = "default",
            };

            var gamePackage = await PostAsync<IngestionPackageCreationRequest, GamePackage>($"products/{productId}/packages", body, ct);

            if (!gamePackage.State.Equals("PendingUpload", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Package request in Partner Center is not 'PendingUpload'.");
            }

            _logger.LogInformation("Package id: {packageId}", gamePackage.Id);
            return gamePackage;
        }
    }
}
