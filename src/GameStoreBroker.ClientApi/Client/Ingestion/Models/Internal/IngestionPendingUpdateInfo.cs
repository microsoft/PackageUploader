using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionPendingUpdateInfo
    {
        public string UpdateType { get; set; }

        /// <summary>
        /// [Running, Failed, Create]
        /// </summary>
        public string Status { get; set; }

        public string Href { get; set; }

        public string FailureReason { get; set; }
    }
}
