using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.ExternalModels
{
    public class UploadConfig
    {
        [JsonProperty(PropertyName = "httpTimeoutMs")]
        public int HttpTimeoutMs { get; set; } = 3000;

        [JsonProperty(PropertyName = "httpUploadTimeoutMs")]
        public int HttpUploadTimeoutMs { get; set; } = 300000;

        [JsonProperty(PropertyName = "maxParallelism")]
        public int MaxParallelism { get; set; } = 24;

        [JsonProperty(PropertyName = "defaultConnectionLimit")]
        public int DefaultConnectionLimit { get; set; } = 2;

        [JsonProperty(PropertyName = "expect100Continue")]
        public bool Expect100Continue { get; set; } = true;

        [JsonProperty(PropertyName = "useNagleAlgorithm")]
        public bool UseNagleAlgorithm { get; set; } = true;
    }
}
