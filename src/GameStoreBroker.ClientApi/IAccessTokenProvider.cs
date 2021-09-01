// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessToken(CancellationToken ct);
    }
}