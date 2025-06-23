// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

public class AzureTenant
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("tenantType")]
    public string TenantType { get; set; }

    public override string ToString()
    {
        return DisplayName;
    }
}

public class AzureTenantList
{
    [JsonPropertyName("value")]
    public List<AzureTenant> Value { get; set; } = new List<AzureTenant>();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}