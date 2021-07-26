using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.ExternalModels;
using Newtonsoft.Json;

namespace GameStoreBroker.Application.Schema
{
    public class UploadPcPackageOperationSchema : UploadPackageOperationSchema
    {
        [JsonProperty(PropertyName = "packageFilePath")]
        public string PackageFilePath { get; set; }
    }
}
