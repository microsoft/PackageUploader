// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionPackageSet
    {
        public string ProductId { get; set; }

        /// <summary>
        /// List of market groups
        /// </summary>
        public List<IngestionMarketGroupPackage> MarketGroupPackages { get; set; }

        /// <summary>
        /// Friendly Name to facilitate Manage By Release feature primarily for COFE (Commercial Office For Enterprise)
        /// </summary>
        public string ManageByReleaseFriendlyName { get; set; }
        
        /// <summary>
        /// Release Url with additional info to facilitate Manage By Release feature primarily for COFE (Commercial Office For Enterprise)
        /// </summary>
        public string ManageByReleaseUrl { get; set; }

        /// <summary>
        /// Resource type [PackageConfiguration, AzureVmPackageConfiguration, AzureContainerImagePackageConfiguration]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Revision Token (to be obsolete)
        /// </summary>
        public string RevisionToken { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string ODataETag { get; set; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Display name of the branch this PackageSet belongs to
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// ID of the branch this PackageSet belongs to
        /// </summary>
        public string BranchId { get; set; }

        /// <summary>
        /// The BranchType for the branch this PackageSet belongs to.
        /// </summary>
        public string BranchType { get; set; }

        /// <summary>
        /// Created date time
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// ModifiedDate date time
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }
}
