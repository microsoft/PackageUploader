// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Client
{
    internal abstract class HttpRestClient : IHttpRestClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private const int RetryDefaultSeconds = 30;
        private const int RetryDefaultTimes = 10;

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        protected HttpRestClient(ILogger logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string subUrl, CancellationToken ct)
        {
            try
            {
                var clientRequestId = GenerateClientRequestId();
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
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<T> PostAsync<T>(string subUrl, T body, CancellationToken ct)
        {
            return await PostAsync<T, T>(subUrl, body, ct);
        }

        public async Task<TOut> PostAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct)
        {
            try
            {
                var clientRequestId = GenerateClientRequestId();
                LogRequestVerbose("POST", subUrl, clientRequestId, body);
                var json = body is null ? string.Empty : JsonSerializer.Serialize(body, DefaultJsonSerializerOptions);
                using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                var request = new HttpRequestMessage(HttpMethod.Post, subUrl);
                request.Headers.Add("Request-ID", clientRequestId);
                request.Content = content;

                var retryCount = RetryDefaultTimes;
                while (retryCount > 0)
                {
                    using var response = await _httpClient.SendAsync(request, ct);
                    var serverRequestId = GetRequestIdFromHeader(response);

                    if (response.StatusCode is HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable || response.Headers.Contains("Retry-After"))
                    {
                        await Task.Delay(GetRetryDelay(response), ct);
                        retryCount--;
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);

                    LogResponseVerbose(result, serverRequestId);

                    return result;
                }

                throw new Exception($"POST '{subUrl}' stuck after {RetryDefaultSeconds * RetryDefaultTimes} seconds");
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<T> PutAsync<T>(string subUrl, T body, CancellationToken ct)
        {
            return await PutAsync<T, T>(subUrl, body, ct).ConfigureAwait(false);
        }

        public async Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct)
        {
            try
            {
                var clientRequestId = GenerateClientRequestId();
                LogRequestVerbose("PUT", subUrl, clientRequestId, body);
                var json = body is null ? string.Empty : JsonSerializer.Serialize(body, DefaultJsonSerializerOptions);
                using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                var request = new HttpRequestMessage(HttpMethod.Put, subUrl);
                request.Headers.Add("Request-ID", clientRequestId);
                request.Content = content;

                var retryCount = RetryDefaultTimes;
                while (retryCount > 0)
                {
                    using var response = await _httpClient.SendAsync(request, ct);
                    var serverRequestId = GetRequestIdFromHeader(response);

                    if (response.StatusCode is HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable || response.Headers.Contains("Retry-After"))
                    {
                        await Task.Delay(GetRetryDelay(response), ct);
                        retryCount--;
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);

                    LogResponseVerbose(result, serverRequestId);

                    return result;
                }

                throw new Exception($"PUT '{subUrl}' stuck after {RetryDefaultSeconds * RetryDefaultTimes} seconds");
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static string GenerateClientRequestId() => Guid.NewGuid().ToString();

        private static TimeSpan GetRetryDelay(HttpResponseMessage response)
        {
            var delay = TimeSpan.FromSeconds(RetryDefaultSeconds);
            const string retryAfterHeader = "Retry-After";
            if (response.Headers.Contains(retryAfterHeader) &&
                response.Headers.TryGetValues(retryAfterHeader, out var headerValues))
            {
                var retryAfter = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(retryAfter))
                {
                    if (int.TryParse(retryAfter, out var retryAfterSeconds))
                    {
                        delay = TimeSpan.FromSeconds(retryAfterSeconds);
                    }
                    else if (DateTime.TryParse(retryAfter, out var retryAfterDateTime))
                    {
                        var now = DateTime.Now;
                        if (retryAfterDateTime > now)
                        {
                            delay = retryAfterDateTime - DateTime.Now;
                        }
                        else
                        {
                            delay = TimeSpan.Zero;
                        }
                    }
                }
            }
            return delay;
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
            if (requestBody is not null)
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
