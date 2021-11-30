// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal abstract class IngestionResource
    {
        /// <summary>
        /// ETag
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string ODataETag { get; set; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; set; }
    }
}