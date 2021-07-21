using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi
{
    public sealed class IngestionHttpClient : HttpRestClient
    {
        public IngestionHttpClient(ILogger<IngestionHttpClient> logger, AadAuthInfo user, HttpClient httpClient) : base(logger, httpClient)
        {
            httpClient.BaseAddress = new Uri("https://api.partner.microsoft.com/v1.0/ingestion/");
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            httpClient.DefaultRequestHeaders.Add("Client-Request-ID", "");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var userToken = GetUserTokenAsync(user).GetAwaiter().GetResult();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
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
    }
}
