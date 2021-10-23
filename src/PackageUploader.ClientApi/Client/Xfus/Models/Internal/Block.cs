// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal
{
    /// <summary>
    /// Describes a segment of binary data
    /// </summary>
    internal sealed class Block
    {
        /// <summary>
        /// Uniquely identifies this block
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The byte offset location of this block
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// The size of this block in bytes
        /// </summary>
        public long Size { get; set; }
    }
}
