// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="GamePackage.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace GameStoreBroker.Api.IngestionModels
{
    using System.Collections.Generic;
    using GameStoreBroker.ClientApi.ExternalModels;
    using Newtonsoft.Json;

    /// <summary>
    /// GamePackage resource
    /// </summary>
    internal class IngestionGamePackage
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }



        /// <summary>
        /// Revision Token (to be obsolete)
        /// </summary>
        [JsonProperty(PropertyName = "revisionToken")]
        public string RevisionToken { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonProperty(PropertyName = "eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// ETag
        /// </summary>
        [JsonProperty(PropertyName = "@odata.etag")]
        public string ODataETag { get; set; }




        /// <summary>
        /// state of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Check body of package to return back to caller.
        /// </summary>
        [JsonProperty(PropertyName = "isEmpty")]
        public bool? IsEmpty { get; set; }



        /// <summary>
        /// Resource type
        /// </summary>
        [JsonProperty(PropertyName = "resourceType")]
        public string ResourceType { get; set; }

        /// <summary>
        /// Type of the package (e.g. Appx, AppxBundle, Msix, Xvc, etc.).
        /// </summary>
        [JsonProperty(PropertyName = "packageType")]
        public string PackageType { get; set; }

        /// <summary>
        /// file name of the package
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        [JsonProperty(PropertyName = "uploadInfo")]
        public XfusUploadInfo UploadInfo { get; set; }

        /// <summary>
        /// file size of the package
        /// </summary>
        [JsonProperty(PropertyName = "fileSize")]
        public long? FileSize { get; set; }

        /// <summary>
        /// If the package is certified
        /// </summary>
        [JsonProperty(PropertyName = "isCertified")]
        public bool? IsCertified { get; set; }

        /// <summary>
        /// language codes of the package
        /// </summary>
        [JsonProperty(PropertyName = "languageCodes")]
        public IReadOnlyCollection<string> LanguageCodes { get; set; }

        /// <summary>
        /// target platforms of the package
        /// </summary>
        [JsonProperty(PropertyName = "targetPlatforms")]
        public IReadOnlyCollection<string> TargetPlatforms { get; set; }

        /// <summary>
        /// package platform of the package
        /// </summary>
        [JsonProperty(PropertyName = "packagePlatform")]
        public string PackagePlatform { get; set; }

        /// <summary>
        /// xvc target platform of the package
        /// </summary>
        [JsonProperty(PropertyName = "xvcTargetPlatform")]
        public string XvcTargetPlatform { get; set; }
    }
}