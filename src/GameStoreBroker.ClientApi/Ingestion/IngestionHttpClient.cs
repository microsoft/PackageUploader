using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi
{
    internal sealed class IngestionHttpClient
    {
        public IngestionHttpClient(ILogger logger, AadAuthInfo user)
        {
            _clientRequestId = "";
            _logger = logger;
            _user = user;
        }

        public async Task<T> GetAsync<T>(string subUrl)
        {
            try
            {
                await LogRequestVerboseAsync("GET " + subUrl, _clientRequestId).ConfigureAwait(false);

                using HttpClient client = await GetClientAsync().ConfigureAwait(false);
                using HttpResponseMessage response = await client.GetAsync(subUrl).ConfigureAwait(false);
                var serverRequestId = GetRequestIdFromHeader(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    T result = JsonConvert.DeserializeObject<T>(responseString, _jsonSetting);

                    await LogResponseVerboseAsync(result, serverRequestId).ConfigureAwait(false);

                    return result;
                }

                throw new Exception($"GET '{subUrl}' failed with {response.StatusCode} [RequestId: {serverRequestId}]");
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex).ConfigureAwait(false);

                throw;
            }
        }

        private async Task<HttpClient> GetClientAsync()
        {
            var client = new HttpClient
            {
                Timeout = new TimeSpan(0, 10, 0),
                BaseAddress = _baseUri
            };
            client.DefaultRequestHeaders.Add("Client-Request-ID", _clientRequestId);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var userToken = await GetUserTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            return client;
        }

        private async Task<string> GetUserTokenAsync()
        {
            const string aadAuthorityBaseUrl = "https://login.microsoftonline.com/";
            const string aadResourceForCaller = "https://api.partner.microsoft.com";

            var aadAuthorityForCaller = aadAuthorityBaseUrl + _user.TenantId;
            var aadClientIdForCaller = _user.ClientId;
            var aadClientSecretForCaller = _user.ClientSecret;

            return await GetAadTokenAsync(aadAuthorityForCaller, aadClientIdForCaller, aadClientSecretForCaller, aadResourceForCaller).ConfigureAwait(false);
        }

        private static async Task<string> GetAadTokenAsync(string aadAuthority, string aadClientId, string aadClientSecret, string aadResource)
        {
            var authenticationContext = new AuthenticationContext(aadAuthority, false);

            var clientCredential = new ClientCredential(aadClientId, aadClientSecret);
            var result = await authenticationContext.AcquireTokenAsync(aadResource, clientCredential).ConfigureAwait(false);

            return result.AccessToken;
        }

        private static string GetRequestIdFromHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string> headerValues))
            {
                return headerValues.FirstOrDefault();
            }

            return string.Empty;
        }

        private async Task LogRequestVerboseAsync(string requestUrl, object requestBody = null)
        {
            await _logger.LogVerboseAsync(requestUrl).ConfigureAwait(false);
            await _logger.LogVerboseAsync(LogHeader).ConfigureAwait(false);
            await _logger.LogVerboseAsync($"{requestUrl} [ClientRequestId: {_clientRequestId}]").ConfigureAwait(false);
            if (requestBody != null)
            {
                await _logger.LogVerboseAsync("Request Body:").ConfigureAwait(false);
                var json = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
                await _logger.LogVerboseAsync(json).ConfigureAwait(false);
            }
            await _logger.LogVerboseAsync(string.Empty).ConfigureAwait(false);
        }

        private async Task LogResponseVerboseAsync(object obj, string serverRequestId)
        {
            await _logger.LogVerboseAsync($"Response Body: [RequestId: {serverRequestId}]").ConfigureAwait(false);
            var json = obj == null ? string.Empty : JsonConvert.SerializeObject(obj, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
            await _logger.LogVerboseAsync(json).ConfigureAwait(false);
            await _logger.LogVerboseAsync(LogHeader).ConfigureAwait(false);
        }

        private async Task LogExceptionAsync(Exception ex)
        {
            await _logger.LogErrorAsync("Exception:").ConfigureAwait(false);
            var json = JsonConvert.SerializeObject(ex, new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
            await _logger.LogErrorAsync(json).ConfigureAwait(false);
            await _logger.LogErrorAsync(LogHeader).ConfigureAwait(false);
        }

        private readonly string _clientRequestId;
        private readonly ILogger _logger;
        private readonly AadAuthInfo _user;

        private readonly JsonSerializerSettings _jsonSetting = new JsonSerializerSettings();
        private readonly Uri _baseUri = new Uri("https://api.partner.microsoft.com/v1.0/ingestion/");

        private const string LogHeader = "------------------------------------------------------------------------------------------";
    }
}
