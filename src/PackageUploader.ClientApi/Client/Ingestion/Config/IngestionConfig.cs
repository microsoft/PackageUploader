﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Config
{
    public class IngestionConfig
    {
        public string BaseAddress { get; set; } = "https://api.partner.microsoft.com/v1.0/ingestion/";
        public int HttpTimeoutMs { get; set; } = 600000;
    }
}