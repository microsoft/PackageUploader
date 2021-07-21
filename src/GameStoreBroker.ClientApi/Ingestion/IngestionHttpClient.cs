using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi
{
    internal sealed class IngestionHttpClient : HttpRestClient
    {
        public IngestionHttpClient(ILogger logger, AadAuthInfo user) : base(logger)
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10),
                BaseAddress = _baseUri
            };
            client.DefaultRequestHeaders.Add("Client-Request-ID", "");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var userToken = GetUserTokenAsync(user).GetAwaiter().GetResult();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            _httpClient = client;
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

        private readonly Uri _baseUri = new Uri("https://api.partner.microsoft.com/v1.0/ingestion/");
    }
}
