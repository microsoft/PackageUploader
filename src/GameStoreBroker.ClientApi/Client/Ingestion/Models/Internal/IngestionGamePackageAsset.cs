// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionGamePackageAsset
    {
        /// <summary>
        /// The Id of the package asset.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The type of the package asset (EraSymbolFile, EraSubmissionValidatorLog, EraEkb).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The file name of the package asset.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns whether or not this asset has been committed yet.
        /// </summary>
        public bool? IsCommitted { get; set; }

        /// <summary>
        /// The containing package Id for this asset.
        /// </summary>
        public string PackageId { get; set; }

        /// <summary>
        /// The type of package this asset represents e.g. Xml, Cab etc.
        /// </summary>
        public string PackageType { get; set; }

        /// <summary>
        /// The DateTime this asset was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        /// <value>
        /// The binary size in bytes.
        /// </value>
        public long? BinarySizeInBytes { get; set; }

        /// <summary>
        /// Xfus upload info
        /// </summary>
        public IngestionXfusUploadInfo UploadInfo { get; set; }

        /// <summary>
        /// file name of the package
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Resource type of this data model
        /// </summary>
        public string ResourceType { get; set; }
    }
}
