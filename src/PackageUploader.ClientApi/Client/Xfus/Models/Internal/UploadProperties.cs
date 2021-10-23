// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal
{
    /// <summary>
    /// Parameters for performing an upload
    /// </summary>
    internal sealed class UploadProperties
    {
        /// <summary>
        /// Describes the file being uploaded
        /// </summary>
        public FileProperties FileProperties { get; set; }
    }
}
