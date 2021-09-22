using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmission
    {
        public string Id { get; set; }

        /// <summary>
        /// (readonly) State of Submission [InProgress, Published]
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// (readonly) Substate of Submission [InDraft, Submitted, Failed, FailedInCertification, ReadyToPublish, Publishing, Published, InStore]
        /// </summary>
        public string Substate { get; set; }

        public IList<TypeValuePair> Targets { get; set; }

        public IList<TypeValuePair> Resources { get; set; }

        public DateTime? PublishedTimeInUtc { get; set; }

        public IngestionPendingUpdateInfo PendingUpdateInfo { get; set; }

        public int ReleaseNumber { get; set; }

        public string FriendlyName { get; set; }
    }
}
