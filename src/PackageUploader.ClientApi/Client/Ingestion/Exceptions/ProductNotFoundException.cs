// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.Exceptions;

public class ProductNotFoundException : IngestionClientException
{
    public ProductNotFoundException(string errorMessage, Exception innerException = null) : base(errorMessage, innerException)
    {
    }
}