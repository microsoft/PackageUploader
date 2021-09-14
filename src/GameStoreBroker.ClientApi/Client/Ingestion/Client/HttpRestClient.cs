// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;
using GameStoreBroker.ClientApi.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
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

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        protected HttpRestClient(ILogger logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
                
                var result = await response.Content.ReadFromJsonAsync<T>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
                LogResponseVerbose(result, GetRequestIdFromHeader(response));
                return result;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async IAsyncEnumerable<T> GetAsyncEnumerable<T>(string subUrl, [EnumeratorCancellation] CancellationToken ct)
        {
            var nextLink = subUrl;
            do
            {
                var response = await GetAsync<PagedCollection<T>>(nextLink, ct);

                if (response.Value == null)
                {
                    yield break;
                }

                foreach (var value in response.Value)
                {
                    if (ct.IsCancellationRequested)
                    {
                        yield break;
                    }
                    yield return value;
                }

                nextLink = response.NextLink;
            } while (!string.IsNullOrWhiteSpace(nextLink) && !ct.IsCancellationRequested);
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

                using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
                LogResponseVerbose(result, GetRequestIdFromHeader(response));
                return result;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        public async Task<T> PutAsync<T>(string subUrl, T body, CancellationToken ct)
        {
            return await PutAsync(subUrl, body, null, ct).ConfigureAwait(false);
        }

        public async Task<T> PutAsync<T>(string subUrl, T body, IDictionary<string, string> customHeaders, CancellationToken ct)
        {
            return await PutAsync<T, T>(subUrl, body, customHeaders, ct).ConfigureAwait(false);
        }

        public async Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct)
        {
            return await PutAsync<TIn, TOut>(subUrl, body, null, ct).ConfigureAwait(false);
        }

        public async Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, IDictionary<string, string> customHeaders, CancellationToken ct)
        {
            try
            {
                var clientRequestId = GenerateClientRequestId();
                LogRequestVerbose("PUT", subUrl, clientRequestId, body);

                var json = body is null ? string.Empty : JsonSerializer.Serialize(body, DefaultJsonSerializerOptions);
                using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                var request = new HttpRequestMessage(HttpMethod.Put, subUrl);
                request.Headers.Add("Request-ID", clientRequestId);

                if (customHeaders is not null && customHeaders.Any())
                {
                    foreach (var (name, value) in customHeaders)
                    {
                        request.Headers.Add(name, value);
                    }
                }

                request.Content = content;

                using var response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
                LogResponseVerbose(result, GetRequestIdFromHeader(response));
                return result;
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
        }

        private static string GenerateClientRequestId() => Guid.NewGuid().ToString();

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
