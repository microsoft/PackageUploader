// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    /// <summary>
    /// Flight Resource
    /// </summary>
    internal sealed class IngestionFlight
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Resource type [Flight]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Flight name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flight group ids
        /// </summary>
        public IList<string> GroupIds { get; set; }

        /// <summary>
        /// Flight type [PackageFlight]
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Flight relative rank
        /// </summary>
        public int RelativeRank { get; set; }
    }
}
