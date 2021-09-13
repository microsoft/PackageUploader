// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionMarketGroupPackage
    {
        /// <summary>
        /// Id of market group
        /// </summary>
        public string MarketGroupId { get; set; }
        
        /// <summary>
        /// Name of market group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of markets
        /// </summary>
        public List<string> Markets { get; set; }

        /// <summary>
        /// List of packages
        /// </summary>
        public List<string> PackageIds { get; set; }

        /// <summary>
        /// Dictionary of per region, per package scheduled release dates for XVC and MSIXVC packages
        /// </summary>
        public Dictionary<string, DateTime?> PackageAvailabilityDates { get; set; }
    }
}
