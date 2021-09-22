// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmissionCreationRequest
    {
        public string ResourceType { get; set; }
        
        /// <summary>
        /// Target of submission [{Sandbox, sandboxId}, {Flight, flightId}, {Scope, Preview/Live}]
        /// </summary>
        public IList<TypeValuePair> Targets { get; set; }

        /// <summary>
        /// Source of submission: [{Sandbox, sandboxId}]
        /// </summary>
        public IList<TypeValuePair> Resources { get; set; }
    }
}
