// Copyright (C) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace GameStoreBroker.Application.Schema
{
    internal class AadAuthInfoSchema
    {
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }
    }
}
