using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.Api;
using GameStoreBroker.ClientApi.ExternalModels;
using GameStoreBroker.ClientApi.Http;
using GameStoreBroker.ClientApi.IngestionModels;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi
{
    internal sealed class IngestionHttpClient : HttpRestClient
    {
        public bool Authorized;

        public IngestionHttpClient(ILogger<IngestionHttpClient> logger, HttpClient httpClient) : base(logger, httpClient)
        {
            httpClient.BaseAddress = new Uri("https://api.partner.microsoft.com/v1.0/ingestion/");
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            httpClient.DefaultRequestHeaders.Add("Client-Request-ID", "");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            //var userToken = GetUserTokenAsync(user).GetAwaiter().GetResult();
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        }

        public async Task Authorize(AadAuthInfo user)
        {
            var userToken = await GetUserTokenAsync(user);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            Authorized = true;
        }

        private async Task<string> GetUserTokenAsync(AadAuthInfo user)
        {
            const string aadAuthorityBaseUrl = "https://login.microsoftonline.com/";
            const string aadResourceForCaller = "https://api.partner.microsoft.com";

            var aadAuthorityForCaller = aadAuthorityBaseUrl + user.TenantId;
            var aadClientIdForCaller = user.ClientId;
            var aadClientSecretForCaller = user.ClientSecret;

            return await GetAadTokenAsync(aadAuthorityForCaller, aadClientIdForCaller, aadClientSecretForCaller, aadResourceForCaller).ConfigureAwait(false);
        }

        private static async Task<string> GetAadTokenAsync(string aadAuthority, string aadClientId, string aadClientSecret, string aadResource)
        {
            var authenticationContext = new AuthenticationContext(aadAuthority, false);

            var clientCredential = new ClientCredential(aadClientId, aadClientSecret);
            var result = await authenticationContext.AcquireTokenAsync(aadResource, clientCredential).ConfigureAwait(false);

            return result.AccessToken;
        }

        public async Task<GameProduct> GetGameProductByLongIdAsync(string longId)
        {
            var ingestionGameProduct = await GetAsync<IngestionGameProduct>($"products/{longId}");

            var gameProduct = new GameProduct
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value
            };

            return gameProduct;
        }

        public async Task<GameProduct> GetGameProductByBigIdAsync(string bigId)
        {
            var ingestionGameProducts = await GetAsync<PagedCollection<IngestionGameProduct>>($"products?externalId={bigId}");
            var ingestionGameProduct = ingestionGameProducts.Value.FirstOrDefault();

            if (ingestionGameProduct == null)
            {
                return null;
            }

            var gameProduct = new GameProduct
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds?.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value
            };

            return gameProduct;
        }

        public async Task<GamePackage> GetPackageByIdAsync(string productId, string packageId)
        {
            var result = await GetAsync<GamePackage>($"products/{productId}/packages/{packageId}");
            return result;
        }
    }
}
