// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    /// <summary>
    /// Flight Resource
    /// </summary>
    public sealed class IngestionFlight
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Resource type [Flight]
        /// </summary>
        [JsonPropertyName("resourceType")]
        public string ResourceType { get; set; }

        /// <summary>
        /// Flight name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Flight group ids
        /// </summary>
        [JsonPropertyName("groupIds")]
        public IList<string> GroupIds { get; set; }

        /// <summary>
        /// Flight type [PackageFlight]
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Flight relative rank
        /// </summary>
        [JsonPropertyName("relativeRank")]
        public int RelativeRank { get; set; }
    }
}
