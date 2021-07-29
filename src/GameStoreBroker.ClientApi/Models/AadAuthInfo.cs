// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Models
{
    public sealed class AadAuthInfo
    {
        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
