// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Client
{
    internal interface IHttpRestClient
    {
        Task<T> GetAsync<T>(string subUrl, CancellationToken ct);
        Task<TOut> PostAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct);
    }
}