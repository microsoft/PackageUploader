// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionVariantResource
    {
        /// <summary>
        /// Variant ID to be linked to submission
        /// </summary>
        public string VariantId { get; set; }

        /// <summary>
        /// Source of variant resources: [{Property, propertyInstanceId}, {Listing, listingInstanceId}, {Availability, availabilityInstanceId}, {Package, packageInstanceId}]
        /// </summary>
        public IList<TypeValuePair> Resources { get; set; }
    }
}
