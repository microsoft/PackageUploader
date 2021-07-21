using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Http
{
    internal abstract class HttpRestClient : IHttpRestClient, IDisposable
    {
        private string _clientRequestId;
        private ILogger _logger;
        protected HttpClient _httpClient;

        public HttpRestClient(ILogger logger)
        {
            _clientRequestId = "";
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string subUrl)
        {
            try
            {
                await LogRequestVerboseAsync("GET " + subUrl, _clientRequestId).ConfigureAwait(false);

                using HttpResponseMessage response = await _httpClient.GetAsync(subUrl).ConfigureAwait(false);
                var serverRequestId = GetRequestIdFromHeader(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    T result = JsonConvert.DeserializeObject<T>(responseString, _jsonSetting);

                    await LogResponseVerboseAsync(result, serverRequestId).ConfigureAwait(false);

                    return result;
                }

                throw new HttpRequestException($"GET '{subUrl}' failed with {response.StatusCode} [RequestId: {serverRequestId}].", null, response.StatusCode);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex).ConfigureAwait(false);

                throw;
            }
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

        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
        }

        private readonly JsonSerializerSettings _jsonSetting = new();
        private const string LogHeader = "------------------------------------------------------------------------------------------";
    }
}
