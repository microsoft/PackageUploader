// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models
{
    public class IngestionAccessToken
    {
        public string AccessToken { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
    }
}
