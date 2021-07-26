using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.Api.IngestionModels
{
    public class IngestionUploadPcPackage
    {
        /// <summary>
        /// Resource ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }

        /// <summary>
        /// Resource type [Package, AzureVmPackage, AzureAppPackage, AzureContainerImagePackage]
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// state of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Check body of package to return back to caller.
        /// </summary>
        public bool? IsEmpty { get; set; }

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
        /// file name of the package
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// file SAS URI of the package
        /// </summary>
        public string FileSasUri { get; set; }

        /// <summary>
        /// file size of the package
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Package full name
        /// </summary>
        public string PackageFullName { get; set; }

        /// <summary>
        /// package format [AppxBundle, Appx, AppxSym, AppxUpload, Xap, Cab, Xvc, EAppxBundle, EAppx]
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// package architecture [X86, X64, Arm, Neutral]
        /// </summary>
        public string Architecture { get; set; }

        ///// <summary>
        ///// A list of TargetPlatforms. An universal package can have multiple target platform dependencies
        ///// and they may be scoped down via an optionally included StoreManifest.
        ///// </summary>
        ///// <value>
        ///// The target platforms.
        ///// </value>
        //public IList<TargetPlatform> TargetPlatforms { get; set; }

        /// <summary>
        /// capabilities of the package
        /// </summary>
        public IList<string> Capabilities { get; set; }

        /// <summary>
        /// supported languages of the package
        /// </summary>
        public IList<string> Languages { get; set; }

        ///// <summary>
        ///// list of bundle contents
        ///// Note: for pre-RS3 appxbundle, each of the PackageBundleContent represents an appx inside the bundle
        ///// For flat bundle, each PackageBundleContent represents a reference to a separate Package
        ///// </summary>
        //public IList<BundleContent> BundleContents { get; set; }

        /// <summary>
        /// package version
        /// </summary>
        public string Version { get; set; }
    }
}
