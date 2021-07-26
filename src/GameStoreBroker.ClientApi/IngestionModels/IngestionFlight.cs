using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.IngestionModels
{
    /// <summary>
    /// Flight Resource
    /// </summary>
    public sealed class IngestionFlight
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Resource type [Flight]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Flight name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flight group ids
        /// </summary>
        public IList<string> GroupIds { get; set; }

        /// <summary>
        /// Flight type [PackageFlight]
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Flight relative rank
        /// </summary>
        public int RelativeRank { get; set; }
    }
}
