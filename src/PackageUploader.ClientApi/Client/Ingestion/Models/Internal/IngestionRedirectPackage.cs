using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionRedirectPackage : IngestionResource
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// ToId
        /// </summary>
        public string ToId { get; set; }
        /// <summary>
        /// Uid
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// ProcessingState
        /// </summary>
        public string ProcessingState { get; set; }
        /// <summary>
        /// ValidationItems
        /// </summary>
        public IReadOnlyCollection<GameSubmissionValidationItem> ValidationItems { get; set; }
    }
}
