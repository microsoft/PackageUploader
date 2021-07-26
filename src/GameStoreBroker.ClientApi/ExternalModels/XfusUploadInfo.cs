using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.ExternalModels
{
    public class XfusUploadInfo
    {
        /// <summary>
        /// File name
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Xfus asset Id
        /// </summary>
        [JsonProperty(PropertyName = "xfusId")]
        public string XfusId { get; set; }

        /// <summary>
        /// file SAS URI
        /// </summary>
        [JsonProperty(PropertyName = "fileSasUri")]
        public string FileSasUri { get; set; }

        /// <summary>
        /// Xfus token
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Xfus upload domain
        /// </summary>
        [JsonProperty(PropertyName = "uploadDomain")]
        public string UploadDomain { get; set; }

        /// <summary>
        /// Xfus tenant, e.g. DCE, XICE
        /// </summary>
        [JsonProperty(PropertyName = "xfusTenant")]
        public string XfusTenant { get; set; }
    }
}
