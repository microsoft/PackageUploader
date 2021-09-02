// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    /// <summary>
    /// GamePackage resource
    /// </summary>
    internal class IngestionGamePackage
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; internal set; }

        /// <summary>
        /// Revision Token (to be obsolete)
        /// </summary>
        [JsonPropertyName("revisionToken")]
        public string RevisionToken { get; set; }

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
        /// Check body of package to return back to caller.
        /// </summary>
        [JsonPropertyName("isEmpty")]
        public bool? IsEmpty { get; set; }

        /// <summary>
        /// Resource type
        /// </summary>
        [JsonPropertyName("resourceType")]
        public string ResourceType { get; set; }

        /// <summary>
        /// Type of the package (e.g. Appx, AppxBundle, Msix, Xvc, etc.).
        /// </summary>
        [JsonPropertyName("packageType")]
        public string PackageType { get; set; }

        /// <summary>
        /// file name of the package
        /// </summary>
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        [JsonPropertyName("uploadInfo")]
        public IngestionXfusUploadInfo UploadInfo { get; set; }

        /// <summary>
        /// file size of the package
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        /// <summary>
        /// If the package is certified
        /// </summary>
        [JsonPropertyName("isCertified")]
        public bool? IsCertified { get; set; }

        /// <summary>
        /// language codes of the package
        /// </summary>
        [JsonPropertyName("languageCodes")]
        public IReadOnlyCollection<string> LanguageCodes { get; set; }

        /// <summary>
        /// target platforms of the package
        /// </summary>
        [JsonPropertyName("targetPlatforms")]
        public IReadOnlyCollection<string> TargetPlatforms { get; set; }

        /// <summary>
        /// package platform of the package
        /// </summary>
        [JsonPropertyName("packagePlatform")]
        public string PackagePlatform { get; set; }

        /// <summary>
        /// xvc target platform of the package
        /// </summary>
        [JsonPropertyName("xvcTargetPlatform")]
        public string XvcTargetPlatform { get; set; }
    }
}