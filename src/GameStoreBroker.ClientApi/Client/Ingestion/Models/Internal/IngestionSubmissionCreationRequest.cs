using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmissionCreationRequest
    {
        public string ResourceType { get; set; }

        //
        // Summary:
        //     Target of submission [{Sandbox, sandboxId}, {Flight, flightId}, {Scope, Preview/Live}]
        public IList<TypeValuePair> Targets { get; set; }

        //
        // Summary:
        //     Source of submission: [{Sandbox, sandboxId}] // TODO: [jinjma] TBD or [{Property,
        //     propertyInstanceId}, {Listing, listingInstanceId}, {Availability, availabilityInstanceId},
        //     {Package, packageInstanceId}]
        public IList<TypeValuePair> Resources { get; set; }
    }
}
