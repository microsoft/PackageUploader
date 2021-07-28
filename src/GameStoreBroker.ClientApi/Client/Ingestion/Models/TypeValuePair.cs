// Copyright (C) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    internal sealed class TypeValuePair
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
