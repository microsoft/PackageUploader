// Copyright (C) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GameStoreBroker.Application.Schema
{
    internal class AadAuthInfoSchema
    {
        [Required(ErrorMessage = "aadAuthInfo__tenantId is required")]
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "aadAuthInfo__clientId is required")]
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }
    }
}
