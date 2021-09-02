// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Models
{
    /// <summary>
    /// GameProduct Model
    /// </summary>
    public sealed class GamePackageBranch
    {
        /// <summary>
        /// LongId
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Collection of external Id
        /// Type: AzurePublisherId, AzureOfferId, ExternalAzureProductId
        /// </summary>
        public string Name { get; set; }

        public string CurrentDraftInstanceID { get; set; }
    }
}