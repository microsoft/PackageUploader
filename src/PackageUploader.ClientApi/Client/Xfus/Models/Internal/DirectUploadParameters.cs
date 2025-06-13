// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal
{
    /// <summary>
    /// Represents the parameters required for performing a direct upload operation.
    /// </summary>
    internal class DirectUploadParameters
    {
        /// <summary>
        /// Gets or sets the SAS (Shared Access Signature) URI used for uploading.
        /// </summary>
        public string SasUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of each block to be uploaded, in bytes.
        /// </summary>
        public long BlockSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the upload is a delta upload.
        /// </summary>
        public bool IsDelta { get; set; } = false;
    }
}
