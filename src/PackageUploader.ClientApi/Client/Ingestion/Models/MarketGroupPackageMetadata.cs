// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public class MarketGroupPackageMetadata
{
    /// <summary>
    /// Properties with no matching class member.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> JsonExtensionData { get; set; }
}