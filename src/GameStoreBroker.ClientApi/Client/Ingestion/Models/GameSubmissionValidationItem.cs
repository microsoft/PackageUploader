using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class GameSubmissionValidationItem
    {
        public string ErrorCode { get; set; }

        /// <summary>
        /// Severity for validation [Informational, Warning, Error]
        /// </summary>
        public string Severity { get; set; }

        public string Message { get; set; }

        public string Resource { get; set; }
    }
}
