// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Xfus.Models.Internal
{
    /// <summary>
    /// Parameters for ingesting a file
    /// </summary>
    internal sealed class FileProperties
    {
        /// <summary>
        /// The file name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The file size in bytes
        /// </summary>
        public long Size { get; set; }
    }
}
