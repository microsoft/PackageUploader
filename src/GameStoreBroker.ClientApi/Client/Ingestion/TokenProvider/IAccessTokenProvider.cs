// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider
{
    public interface IAccessTokenProvider
    {
        string GetAccessToken();
    }
}