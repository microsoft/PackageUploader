// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class XfusUploadInfo
    {
        /// <summary>
        /// File name
        /// </summary>
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Xfus asset Id
        /// </summary>
        [JsonPropertyName("xfusId")]
        public string XfusId { get; set; }

        /// <summary>
        /// file SAS URI
        /// </summary>
        [JsonPropertyName("fileSasUri")]
        public string FileSasUri { get; set; }

        /// <summary>
        /// Xfus token
        /// </summary>
        [JsonPropertyName("token")]
        public string Token { get; set; }

        /// <summary>
        /// Xfus upload domain
        /// </summary>
        [JsonPropertyName("uploadDomain")]
        public string UploadDomain { get; set; }

        /// <summary>
        /// Xfus tenant, e.g. DCE, XICE
        /// </summary>
        [JsonPropertyName("xfusTenant")]
        public string XfusTenant { get; set; }
    }
}
