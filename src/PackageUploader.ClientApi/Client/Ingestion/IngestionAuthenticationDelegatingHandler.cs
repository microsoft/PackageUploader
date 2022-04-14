// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion;

internal class IngestionAuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<IngestionAuthenticationDelegatingHandler> _logger;
    private IngestionAccessToken _token;

    public IngestionAuthenticationDelegatingHandler(IAccessTokenProvider accessTokenProvider, ILogger<IngestionAuthenticationDelegatingHandler> logger)
    {
        _accessTokenProvider = accessTokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        _token ??= await _accessTokenProvider.GetTokenAsync(ct).ConfigureAwait(false);
        var response = await SetAuthHeaderAndSendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogDebug("Response status code was {statusCode}. Retrying with new token.", response.StatusCode);
            _token = await _accessTokenProvider.GetTokenAsync(ct).ConfigureAwait(false);
            response = await SetAuthHeaderAndSendAsync(request, ct).ConfigureAwait(false);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SetAuthHeaderAndSendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }
}