// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Xfus.Models
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
