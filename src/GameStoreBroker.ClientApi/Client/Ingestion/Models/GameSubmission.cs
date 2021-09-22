using GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GameSubmission
    {
        public string Id { get; set; }

        public GameSubmissionState GameSubmissionState { get; set; }

        public int ReleaseNumber { get; set; }

        public string FriendlyName { get; set; }

        public DateTime? PublishedTimeInUtc { get; set; }

        public List<GameSubmissionValidationItem> SubmissionValidationItems { get; set; }
    }
}
