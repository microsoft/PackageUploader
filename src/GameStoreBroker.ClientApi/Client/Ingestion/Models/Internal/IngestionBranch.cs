// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionBranch
    {
        /// <summary>
        /// Branch Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        public string CurrentDraftInstanceId { get; set; }
    }
}
