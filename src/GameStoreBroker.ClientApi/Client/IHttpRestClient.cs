// Copyright (C) Microsoft. All rights reserved.

using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client
{
    internal interface IHttpRestClient
    {
        Task<T> GetAsync<T>(string subUrl);
    }
}