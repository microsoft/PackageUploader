// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Builders
{
    internal interface IBuilder<out T>
    {
        public T Build();
    }
}