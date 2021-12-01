// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config
{
    public class AccessTokenProviderConfig
    {
        public string AadAuthorityBaseUrl { get; set; } = "https://login.microsoftonline.com/";
        public string AadResourceForCaller { get; set; } = "https://api.partner.microsoft.com";
    }
}