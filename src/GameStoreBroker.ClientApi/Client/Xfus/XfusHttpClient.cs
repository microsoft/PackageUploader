// Copyright (C) Microsoft. All rights reserved.

using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.ClientApi.Client.Xfus
{
    internal sealed class XfusHttpClient : HttpRestClient, IXfusHttpClient
    {
        public XfusHttpClient(ILogger<XfusHttpClient> logger, HttpClient httpClient) : base(logger, httpClient)
        {
        }
    }
}
