// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;

namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    internal class IngestionAuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _accessTokenProvider;
        private IngestionAccessToken _token;

        public IngestionAuthenticationDelegatingHandler(IAccessTokenProvider accessTokenProvider)
        {
            _accessTokenProvider = accessTokenProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            _token ??= await _accessTokenProvider.GetTokenAsync(ct);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            var response = await base.SendAsync(request, ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _token = await _accessTokenProvider.GetTokenAsync(ct);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                response = await base.SendAsync(request, ct);
            }

            return response;
        }
    }
}
