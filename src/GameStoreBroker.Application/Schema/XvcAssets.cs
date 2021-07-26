using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.Application.Schema
{
    public class XvcAssets
    {
        [JsonProperty(PropertyName = "packageFilePath")]
        public string PackageFilePath { get; set; }

        [JsonProperty(PropertyName = "ekbFilePath")]
        public string EkbFilePath { get; set; }

        [JsonProperty(PropertyName = "subvalFilePath")]
        public string SubvalFilePath { get; set; }

        [JsonProperty(PropertyName = "symbolsFilePath")]
        public string SymbolsFilePath { get; set; }

        [JsonProperty(PropertyName = "discLayoutFilePath")]
        public string DiscLayoutFilePath { get; set; }
    }
}