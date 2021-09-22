// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionSubmission
    {
        /// <summary>
        /// Resource type [Submission]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// (readonly) State of Submission [Inprogress, Published]
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// (readonly) Substate of Submission [InDraft, Submitted, Failed, FailedInCertification, ReadyToPublish, Publishing, Published, InStore]
        /// </summary>
        public string Substate { get; set; }

        /// <summary>
        /// Target of submission [{Sandbox, sandboxID}, {Flight, flightID}, {Scope, Preview/Live}]
        /// </summary>
        public IList<TypeValuePair> Targets { get; set; }

        /// <summary>
        /// Source of Submission resources: [{Sandbox, sandboxID}] or [{Property, propertyInstanceID}, {Listing, listingInstanceID}, {Availability, availabilityInstanceID}, {Package, packageInstanceID}]
        /// </summary>
        public IList<TypeValuePair> Resources { get; set; }

        /// <summary>
        /// Source Variants of submission
        /// </summary>
        public IList<IngestionVariantResource> VariantResources { get; set; }

        /// <summary>
        /// Submission publish details
        /// </summary>
        public IngestionPublishOption PublishOption { get; set; }

        /// <summary>
        /// Published time (UTC)
        /// </summary>
        public DateTime? PublishedTimeInUtc { get; set; }

        /// <summary>
        /// Submission pending update status details
        /// </summary>
        public IngestionPendingUpdateInfo PendingUpdateInfo { get; set; }

        /// <summary>
        /// Any extra information need to pass.
        /// </summary>
        public IList<TypeValuePair> ExtendedProperties { get; set; }

        /// <summary>
        /// (readonly) Release number
        /// </summary>
        public int ReleaseNumber { get; set; }

        /// <summary>
        /// (readonly) Friendly name
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; set; }
    }
}
