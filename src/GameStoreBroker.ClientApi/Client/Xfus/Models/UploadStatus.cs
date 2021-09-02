// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Xfus.Models
{
    /// <summary>
    /// Specifies the state of an asset upload's progress.
    /// </summary>
    internal enum UploadStatus
    {
        /// <summary>
        /// There are still pending blocks that the client should upload.
        /// </summary>
        ReceivingBlocks,

        /// <summary>
        /// There are no more pending blocks, however other actions are being done on the server side. The client should wait before calling to get upload progress again.
        /// </summary>
        Busy,

        /// <summary>
        /// There are no more pending blocks and no pending work to be done, upload is complete.
        /// </summary>
        Completed
    }
}
