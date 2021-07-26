using Newtonsoft.Json;

namespace GameStoreBroker.Api
{
    public class AadAuthInfo
    {
        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "clientId")]
        public string ClientId { get; set; }

        [JsonProperty(PropertyName = "clientSecret")]
        public string ClientSecret { get; set; }
    }
}
