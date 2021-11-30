// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.Exceptions
{
    public class IngestionClientException : Exception
    {
        public IngestionClientException()
        {
        }

        public IngestionClientException(string message) : base(message)
        {
        }

        public IngestionClientException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}