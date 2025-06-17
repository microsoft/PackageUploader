// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal;

/// <summary>
/// Status information for an upload
/// </summary>
internal sealed class UploadProgress
{
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

    /// <summary>
    /// Direct upload parameters
    /// </summary>
    public DirectUploadParameters DirectUploadParameters { get; set; }
}