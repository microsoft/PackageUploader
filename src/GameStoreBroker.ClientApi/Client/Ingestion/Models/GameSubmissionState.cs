using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public enum GameSubmissionState
    {
        NotStarted,
        InProgress,
        Published,
        Failed,
    }
}
