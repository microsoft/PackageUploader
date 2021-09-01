// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client
{
    internal interface IHttpRestClient
    {
        Task<T> GetAsync<T>(string subUrl, CancellationToken ct);
        Task<Tout> PostAsync<Tin, Tout>(string subUrl, Tin body, CancellationToken ct);
    }
}