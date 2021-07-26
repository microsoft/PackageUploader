// Copyright (C) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("GameStoreBroker.ClientApi.Test")]
namespace GameStoreBroker.ClientApi.Client.Ingestion
{
    internal interface IIngestionHttpClient
    {
        Task Authorize(AadAuthInfo user);
        Task<GameProduct> GetGameProductByLongIdAsync(string longId);
        Task<GameProduct> GetGameProductByBigIdAsync(string bigId);
    }
}