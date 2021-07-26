using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.ExternalModels;
using Newtonsoft.Json;

namespace GameStoreBroker.Application.Schema
{
    public abstract class UploadPackageOperationSchema : BaseOperationSchema
    {
        [JsonProperty(PropertyName = "branchFriendlyName")]
        public string BranchFriendlyName { get; set; }

        [JsonProperty(PropertyName = "flightName")]
        public string FlightName { get; set; }

        [JsonProperty(PropertyName = "uploadConfig")]
        public UploadConfig UploadConfig { get; set; }

        [JsonProperty(PropertyName = "minutesToWaitForProcessing")]
        public int MinutesToWaitForProcessing { get; set; }
    }
}
