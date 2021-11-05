// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public interface IAccessTokenProvider
    {
        Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct);
    }
}
