// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using PackageUploader.ClientApi.Client.Ingestion.Sanitizers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.Client;

internal abstract class HttpRestClient : IHttpRestClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly MediaTypeHeaderValue JsonMediaTypeHeaderValue = new (MediaTypeNames.Application.Json);
    private const LogLevel VerboseLogLevel = LogLevel.Trace;
    private readonly string _sdkVersion;

    protected HttpRestClient(ILogger logger, HttpClient httpClient, IngestionSdkVersion ingestionSdkVersion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sdkVersion = ingestionSdkVersion?.SdkVersion ?? "SDK-V1.0.0";
    }

    public async Task<T> GetAsync<T>(string subUrl, CancellationToken ct)
    {
        try
        {
            var request = CreateJsonRequestMessage(HttpMethod.Get, subUrl);

            await LogRequestVerboseAsync(request, ct).ConfigureAwait(false);
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            await LogResponseVerboseAsync(response, ct).ConfigureAwait(false);
    
            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently or HttpStatusCode.Found 
                or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect)
            {
                string redirectUrl = response.Headers.Location?.ToString();
                var redirectRequest = CreateJsonRequestMessage(HttpMethod.Get, redirectUrl);

                await LogRequestVerboseAsync(redirectRequest, ct).ConfigureAwait(false);
                using var redirectResponse = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                await LogResponseVerboseAsync(redirectResponse, ct).ConfigureAwait(false);
                redirectResponse.EnsureSuccessStatusCode();
                
                var redirectResult = await redirectResponse.Content.ReadFromJsonAsync<T>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
                return redirectResult;
            }
            else
            {
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<T>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
                return result;
            }
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
            var response = await GetAsync<PagedCollection<T>>(nextLink, ct).ConfigureAwait(false);

            if (response.Value is null)
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
            var request = CreateJsonRequestMessage(HttpMethod.Post, subUrl, body);

            await LogRequestVerboseAsync(request, ct).ConfigureAwait(false);
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            await LogResponseVerboseAsync(response, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
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
            var request = CreateJsonRequestMessage(HttpMethod.Put, subUrl, body, customHeaders);

            await LogRequestVerboseAsync(request, ct).ConfigureAwait(false);
            using var response = await _httpClient.SendAsync(request, ct);
            await LogResponseVerboseAsync(response, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TOut>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    private HttpRequestMessage CreateJsonRequestMessage(HttpMethod method, string requestUri) =>
        CreateJsonRequestMessage(method, requestUri, (object) null, null);

    private HttpRequestMessage CreateJsonRequestMessage<T>(HttpMethod method, string requestUri, T inputValue) =>
        CreateJsonRequestMessage(method, requestUri, inputValue, null);

    private HttpRequestMessage CreateJsonRequestMessage<T>(HttpMethod method, string requestUri, T inputValue, IDictionary<string, string> customHeaders)
    {
        var request = new HttpRequestMessage(method, requestUri);
        if (inputValue is not null)
        {
            request.Content = JsonContent.Create(inputValue, JsonMediaTypeHeaderValue, DefaultJsonSerializerOptions);
        }
        request.Headers.Add("Request-ID", Guid.NewGuid().ToString());
        request.Headers.Add("MethodOfAccess", _sdkVersion);

        if (customHeaders is not null && customHeaders.Any())
        {
            foreach (var (name, value) in customHeaders)
            {
                request.Headers.Add(name, value);
            }
        }

        return request;
    }

    private static string GetRequestIdFromHeaders(HttpHeaders headers) =>
        headers.TryGetValues("Request-ID", out var headerValues)
            ? headerValues.FirstOrDefault()
            : string.Empty;

    private async Task LogRequestVerboseAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!_logger.IsEnabled(VerboseLogLevel))
            return;

        var clientRequestId = GetRequestIdFromHeaders(request.Headers);
        _logger.Log(VerboseLogLevel, "Request {verb} {requestUrl} [ClientRequestId: {clientRequestId}]", request.Method, request.RequestUri, clientRequestId);
        if (request.Content is not null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.Log(VerboseLogLevel, "Request Body: {requestBody}", requestBody);
        }
    }

    private async Task LogResponseVerboseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!_logger.IsEnabled(VerboseLogLevel))
            return;

        var serverRequestId = GetRequestIdFromHeaders(response.Headers);
        _logger.Log(VerboseLogLevel, "Response {statusCodeInt} {statusCode}: {reasonPhrase} [ServerRequestId: {serverRequestId}]", (int)response.StatusCode, response.StatusCode, response.ReasonPhrase ?? "", serverRequestId);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        responseBody = LogSanitizer.SanitizeJsonResponse(responseBody);

        _logger.Log(VerboseLogLevel, "Response Body: {responseBody}", responseBody);
    }

    private void LogException(Exception ex)
    {
        _logger.LogError(ex, "Exception:");
    }
}