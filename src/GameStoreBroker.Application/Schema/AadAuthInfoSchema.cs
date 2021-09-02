// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal class AadAuthInfoSchema
    {
        [Required(ErrorMessage = "aadAuthInfo__tenantId is required")]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "aadAuthInfo__clientId is required")]
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
