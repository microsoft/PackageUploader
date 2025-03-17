// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader.State;

internal class NoDeltaUploadState : XfusUploaderState
{
    internal NoDeltaUploadState(XfusApiController xfusApiController, ILogger logger) : base(xfusApiController, logger)
    {
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, IProgress<ulong> bytesProgress, CancellationToken ct)
    {
        var uploadProgress = await InitializeAssetAsync(xfusUploadInfo, uploadFile, false, ct).ConfigureAwait(false);
        _logger.LogInformation("XFUS Asset Initialized. Will upload {totalBlockBytes} across {pendingBlocks} blocks.", new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes), uploadProgress.PendingBlocks.Length);

        await FullUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, false, httpTimeoutMs, bytesProgress, ct).ConfigureAwait(false);

        _logger.LogTrace("Upload complete. Total Uploaded: {totalBytesUploaded}", new ByteSize(_totalBytesUploaded));
        return null;
    }
}
