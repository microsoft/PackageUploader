// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Exceptions
{
    public class PackageSetNotFoundException : IngestionClientException
    {
        public PackageSetNotFoundException(string errorMessage, Exception innerException = null) : base(errorMessage, innerException)
        {
        }
    }
}