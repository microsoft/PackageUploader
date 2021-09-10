// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models
{
    public sealed class AadAuthInfo
    {
        [Required]
        public string TenantId { get; set; }

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }
    }
}
