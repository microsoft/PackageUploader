// Copyright (C) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.Client
{
    internal abstract class HttpRestClient : IHttpRestClient
    {
        private readonly string _clientRequestId;
        private readonly ILogger _logger;
        protected readonly HttpClient HttpClient;

        private readonly JsonSerializerSettings _jsonSetting = new JsonSerializerSettings();
        private readonly JsonSerializerSettings _logJsonSettings = new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore };

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
                var serverRequestId = GetRequestIdFromHeader(response);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var result = JsonConvert.DeserializeObject<T>(responseString, _jsonSetting);

                    LogResponseVerbose(result, serverRequestId);

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
                _logger.LogTrace(requestBody.ToJson(settings: _logJsonSettings));
            }
        }

        private void LogResponseVerbose(object obj, string serverRequestId)
        {
            _logger.LogTrace("Response Body: [RequestId: {serverRequestId}]", serverRequestId);
            _logger.LogTrace(obj.ToJson(settings: _logJsonSettings));
        }

        private void LogException(Exception ex)
        {
            _logger.LogError(ex, "Exception:");
        }
    }
}
