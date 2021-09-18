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
        /// Branch name
        /// </summary>
        public string Name { get; internal init; }

        /// <summary>
        /// Branch current draft instance ID.
        /// </summary>
        public string CurrentDraftInstanceId { get; internal init; }
    }
}