// Copyright (C) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    internal sealed class IngestionGameProduct
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isModularPublishing")]
        public bool? IsModularPublishing { get; set; }

        [JsonPropertyName("externalIDs")]
        public IList<TypeValuePair> ExternalIds { get; set; }
    }
}