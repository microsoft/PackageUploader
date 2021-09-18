// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    /// <summary>
    /// GameProduct Model
    /// </summary>
    public sealed class GamePackageBranch
    {
        /// <summary>
        /// LongId
        /// </summary>
        public string Id { get; internal init; }

        /// <summary>
        /// Collection of external Id
        /// Type: AzurePublisherId, AzureOfferId, ExternalAzureProductId
        /// </summary>
        public string Name { get; internal init; }

        public string CurrentDraftInstanceId { get; internal init; }
    }
}