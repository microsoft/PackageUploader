// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.Client;

internal interface IHttpRestClient
{
    Task<T> GetAsync<T>(string subUrl, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct);
    IAsyncEnumerable<T> GetAsyncEnumerable<T>(string subUrl, JsonTypeInfo<PagedCollection<T>> jsonTypeInfo, CancellationToken ct);
    Task<T> PostAsync<T>(string subUrl, T body, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct);
    Task<TOut> PostAsync<TIn, TOut>(string subUrl, TIn body, JsonTypeInfo<TIn> jsonTInInfo, JsonTypeInfo<TOut> jsonTOutInfo, CancellationToken ct);
    Task<T> PutAsync<T>(string subUrl, T body, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct);
    Task<T> PutAsync<T>(string subUrl, T body, JsonTypeInfo<T> jsonTypeInfo, IDictionary<string, string> customHeaders, CancellationToken ct);
    Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, JsonTypeInfo<TIn> jsonTInInfo, JsonTypeInfo<TOut> jsonTOutInfo, CancellationToken ct);
    Task<TOut> PutAsync<TIn, TOut>(string subUrl, TIn body, JsonTypeInfo<TIn> jsonTInInfo, JsonTypeInfo<TOut> jsonTOutInfo, IDictionary<string, string> customHeaders, CancellationToken ct);
}