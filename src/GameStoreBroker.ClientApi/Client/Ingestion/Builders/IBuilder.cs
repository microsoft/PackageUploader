// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Builders
{
    internal interface IBuilder<out T>
    {
        public T Build();
    }
}