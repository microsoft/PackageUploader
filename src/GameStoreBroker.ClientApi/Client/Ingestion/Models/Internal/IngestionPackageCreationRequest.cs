// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
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