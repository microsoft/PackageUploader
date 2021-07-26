using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.Api.IngestionModels
{
    internal class IngestionBranch
    {
        /// <summary>
        /// Branch Id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        [JsonProperty(PropertyName = "friendlyName")]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        [JsonProperty(PropertyName = "currentDraftInstanceID")]
        public string CurrentDraftInstanceID { get; set; }
    }
}
