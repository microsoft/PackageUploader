// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    internal class IngestionBranch
    {
        /// <summary>
        /// Branch Id
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        [JsonPropertyName("friendlyName")]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Friendly Name
        /// </summary>
        [JsonPropertyName("currentDraftInstanceID")]
        public string CurrentDraftInstanceID { get; set; }
    }
}
