// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.Exceptions;

public class PackageBranchNotFoundException : IngestionClientException
{
    public PackageBranchNotFoundException(string errorMessage, Exception innerException = null) : base(errorMessage, innerException)
    {
    }
}