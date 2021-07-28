// Copyright (C) Microsoft. All rights reserved.

namespace GameStoreBroker.ClientApi.Models
{
    public sealed class AadAuthInfo
    {
        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
