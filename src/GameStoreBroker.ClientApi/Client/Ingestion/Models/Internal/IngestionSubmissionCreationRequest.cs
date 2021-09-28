// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmissionCreationRequest
    {
        /// <summary>
        /// Resource type [SubmissionCreationRequest]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Target of submission [{Sandbox, sandboxId}, {Flight, flightId}, {Scope, Preview/Live}]
        /// </summary>
        public IList<TypeValuePair> Targets { get; set; }

        /// <summary>
        /// Source of submission: [{Sandbox, sandboxId}] or [{Property, propertyInstanceId}, {Listing, listingInstanceId}, {Availability, availabilityInstanceId}, {Package, packageInstanceId}, {AgeRating, ageRatingInstanceId}]
        /// </summary>
        public IList<TypeValuePair> Resources { get; set; }

        /// <summary>
        /// Source variants of submission
        /// </summary>
        public IList<IngestionVariantResource> VariantResources { get; set; }

        /// <summary>
        /// Submission publish options
        /// </summary>
        public IngestionPublishOption PublishOption { get; set; }

        /// <summary>
        /// Any extra information need to pass.
        /// </summary>
        public IList<TypeValuePair> ExtendedProperties { get; set; }
    }
}
