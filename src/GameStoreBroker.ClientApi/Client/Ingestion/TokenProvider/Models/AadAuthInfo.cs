// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.ClientApi.Client.Ingestion.TokenProvider.Models
{
    public sealed class AadAuthInfo
    {
        [Required(ErrorMessage = "AadAuthInfo:TenantId is required")]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "AadAuthInfo:ClientId is required")]
        public string ClientId { get; set; }

        [Required(ErrorMessage = "AadAuthInfo:ClientSecret is required")]
        public string ClientSecret { get; set; }
    }
}
