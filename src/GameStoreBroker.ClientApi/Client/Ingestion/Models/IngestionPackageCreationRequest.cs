// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    internal class IngestionPackageCreationRequest : IngestionGamePackage
    {
        /// <summary>
        /// The market group ID to put the package in
        /// </summary>
        public string MarketGroupId { get; set; }

        /// <summary>
        /// The package configuration Id
        /// </summary>
        public string PackageConfigurationId { get; set; }
    }
}