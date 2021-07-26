using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameStoreBroker.Api;
using Newtonsoft.Json;

namespace GameStoreBroker.Application.Schema
{
    public abstract class BaseOperationSchema
    {
        [JsonProperty(PropertyName = "operationName")]
        public string OperationName { get; set; }

        [JsonProperty(PropertyName = "productId")]
        public string ProductId { get; set; }

        [JsonProperty(PropertyName = "bigId")]
        public string BigId { get; set; }

        [JsonProperty(PropertyName = "aadAuthInfo")]
        public AadAuthInfo AadAuthInfo { get; set; }
    }
}
