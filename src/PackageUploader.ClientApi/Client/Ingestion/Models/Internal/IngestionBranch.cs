// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionBranch
    {
        /// <summary>
        /// Resource type [Branch]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Branch type [Main, Private]
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Module type [Listing, Property, Package, Availability]
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// Branch current draft instance ID. Service set only
        /// </summary>
        public string CurrentDraftInstanceId { get; set; }
    }
}
