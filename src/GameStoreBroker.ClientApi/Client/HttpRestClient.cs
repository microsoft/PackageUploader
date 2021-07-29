// Copyright (C) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.ClientApi.Client
{
    internal abstract class HttpRestClient : IHttpRestClient
    {
        private readonly string _clientRequestId;
        private readonly ILogger _logger;
        protected readonly HttpClient HttpClient;

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();

        protected HttpRestClient(ILogger logger, HttpClient httpClient)
        {
            _clientRequestId = "";
            _logger = logger;
            HttpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string subUrl, CancellationToken ct)
        {
            try
            {
                LogRequestVerbose("GET " + subUrl, _clientRequestId);

                using var response = await HttpClient.GetAsync(subUrl, ct).ConfigureAwait(false);
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

        private static string GetRequestIdFromHeader(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Request-ID", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }

            return string.Empty;
        }

        private void LogRequestVerbose(string requestUrl, string clientRequestId, object requestBody = null)
        {
            _logger.LogTrace("{requestUrl} [ClientRequestId: {_clientRequestId}]", requestUrl, clientRequestId);
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
