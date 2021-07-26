using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi.ExternalModels
{
    public class GamePackageAsset
    {
        /// <summary>
        /// The Id of the package asset.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The type of the package asset (EraSymbolFile, EraSubmissionValidatorLog, EraEkb).
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// The file name of the package asset.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Returns whether or not this asset has been committed yet.
        /// </summary>
        [JsonProperty(PropertyName = "isCommitted")]
        public bool? IsCommitted { get; set; }

        /// <summary>
        /// The containing package Id for this asset.
        /// </summary>
        [JsonProperty(PropertyName = "packageId")]
        public string PackageId { get; set; }

        /// <summary>
        /// The type of package this asset represents e.g. Xml, Cab etc.
        /// </summary>
        [JsonProperty(PropertyName = "packageType")]
        public string PackageType { get; set; }

        /// <summary>
        /// The DateTime this asset was created.
        /// </summary>
        [JsonProperty(PropertyName = "createdDate")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        /// <value>
        /// The binary size in bytes.
        /// </value>
        [JsonProperty(PropertyName = "binarySizeInBytes")]
        public long? BinarySizeInBytes { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        [JsonProperty(PropertyName = "uploadInfo")]
        public XfusUploadInfo UploadInfo { get; set; }

        /// <summary>
        /// file name of the package
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }
    }
}
