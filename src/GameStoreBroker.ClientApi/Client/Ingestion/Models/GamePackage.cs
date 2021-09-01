// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GamePackage
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; internal set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonPropertyName("eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonPropertyName("@odata.etag")]
        public string ODataETag { get; set; }

        /// <summary>
        /// state of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        [JsonPropertyName("uploadInfo")]
        public XfusUploadInfo UploadInfo { get; set; }
    }
}
