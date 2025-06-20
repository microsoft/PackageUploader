// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

public class BrowserAuthInfo
{
    public const string ConfigName = nameof(BrowserAuthInfo);

    /// <summary>
    /// Optional tenant ID to use for browser authentication
    /// </summary>
    public string TenantId { get; set; }
}