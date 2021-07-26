using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.Api.IngestionModels;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.ExternalModels
{
    public class GamePackage
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonProperty(PropertyName = "eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonProperty(PropertyName = "@odata.etag")]
        public string ODataETag { get; set; }

        /// <summary>
        /// state of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        [JsonProperty(PropertyName = "uploadInfo")]
        public XfusUploadInfo UploadInfo { get; set; }
    }
}
