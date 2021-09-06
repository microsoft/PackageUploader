// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace GameStoreBroker.ClientApi.Client.Xfus.Models.Internal
{
    /// <summary>
    /// Status information for an upload
    /// </summary>
    internal sealed class UploadProgress
    {
        /// <summary>
        /// Creates an instance of <see cref="UploadProgress"/>
        /// </summary>
        public UploadProgress(Block[] pendingBlocks, UploadStatus uploadStatus, TimeSpan requestDelay = default)
        {
            this.PendingBlocks = pendingBlocks ?? throw new ArgumentNullException(nameof(pendingBlocks));
            this.Status = uploadStatus;
            this.RequestDelay = requestDelay;
        }

        /// <summary>
        /// Blocks the service is expecting but has not receieved
        /// </summary>
        public Block[] PendingBlocks { get; set; }

        /// <summary>
        /// Status of upload progress.
        /// </summary>
        public UploadStatus Status { get; set; }

        /// <summary>
        /// Delay to wait before retrying API call
        /// </summary>
        public TimeSpan RequestDelay { get; set; }
    }
}
