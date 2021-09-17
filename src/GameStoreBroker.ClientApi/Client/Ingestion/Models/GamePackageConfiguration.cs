// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GamePackageConfiguration
    {
        /// <summary>
        /// List of market groups
        /// </summary>
        public List<GameMarketGroupPackage> MarketGroupPackages { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        public string ODataETag { get; set; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the branch this PackageSet belongs to
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// ID of the branch this PackageSet belongs to
        /// </summary>
        public string BranchId { get; set; }

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
