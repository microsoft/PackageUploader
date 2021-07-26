using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    internal sealed class IngestionGameProduct
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "isModularPublishing")]
        public bool? IsModularPublishing { get; set; }

        [JsonProperty(PropertyName = "externalIDs")]
        public IList<TypeValuePair> ExternalIds { get; set; }
    }
}