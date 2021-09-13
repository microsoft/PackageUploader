// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Client
{
    internal interface IHttpRestClient
    {
        Task<T> GetAsync<T>(string subUrl, CancellationToken ct);
        Task<T> PostAsync<T>(string subUrl, T body, CancellationToken ct);
        Task<TOut> PostAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct);
        Task<T> PutAsync<T>(string subUrl, T body, CancellationToken ct);
        Task<T> PutAsync<T>(string subUrl, T body, IDictionary<string, string> customHeaders, CancellationToken ct);
        Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, CancellationToken ct);
        Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, IDictionary<string, string> customHeaders, CancellationToken ct);
    }
}