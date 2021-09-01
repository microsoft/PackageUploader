// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client
{
    internal abstract class HttpRestClient : IHttpRestClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();

        protected HttpRestClient(ILogger logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        protected void SetAuthorizationHeader(AuthenticationHeaderValue authenticationHeaderValue)
        {
            _httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
        }

        public async Task<T> GetAsync<T>(string subUrl, CancellationToken ct)
        {
            try
            {
                var clientRequestId = Guid.NewGuid().ToString();
                LogRequestVerbose("GET", subUrl, clientRequestId);
                var request = new HttpRequestMessage(HttpMethod.Get, subUrl);
                request.Headers.Add("Request-ID", clientRequestId);
                using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var serverRequestId = GetRequestIdFromHeader(response);
                var result = await response.Content.ReadFromJsonAsync<T>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);

                LogResponseVerbose(result, serverRequestId);

                return result;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<Tout> PostAsync<Tin, Tout>(string subUrl, Tin body, CancellationToken ct)
        {
            try
            {
                var clientRequestId = Guid.NewGuid().ToString();
                LogRequestVerbose("POST", subUrl, clientRequestId, body);
                var json = body == null ? string.Empty : JsonSerializer.Serialize(body, DefaultJsonSerializerOptions);
                using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                var request = new HttpRequestMessage(HttpMethod.Get, subUrl);
                request.Headers.Add("Request-ID", clientRequestId);

                var k = 10;
                while (k > 0)
                {
                    using var response = await _httpClient.SendAsync(request, ct);
                    var serverRequestId = GetRequestIdFromHeader(response);

                    if (response.StatusCode == HttpStatusCode.GatewayTimeout ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        response.Headers.Contains("Retry-After"))
                    {
                        await Task.Delay(new TimeSpan(0, 0, 30));
                        k--;
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<Tout>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);

                    LogResponseVerbose(result, serverRequestId);

                    return result;
                }

                throw new Exception($"POST '{subUrl}' stuck after 5 mins");
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static string GetRequestIdFromHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Request-ID", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }

            return string.Empty;
        }

        private void LogRequestVerbose(string verb, string requestUrl, string clientRequestId, object requestBody = null)
        {
            _logger.LogTrace("{verb} {requestUrl} [ClientRequestId: {clientRequestId}]", verb, requestUrl, clientRequestId);
            if (requestBody != null)
            {
                _logger.LogTrace("Request Body:");
                _logger.LogTrace(requestBody.ToJson());
            }
        }

        private void LogResponseVerbose(object obj, string serverRequestId)
        {
            _logger.LogTrace("Response Body: [RequestId: {serverRequestId}]", serverRequestId);
            _logger.LogTrace(obj.ToJson());
        }

        private void LogException(Exception ex)
        {
            _logger.LogError(ex, "Exception:");
        }
    }
}
