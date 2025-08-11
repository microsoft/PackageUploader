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

internal class DeltaUploadMainUploadState : XfusUploaderState
{
    private readonly UploadProgress _uploadProgress;

    internal DeltaUploadMainUploadState(XfusApiController xfusApiController, ILogger logger, UploadProgress uploadProgress, XfusBlockProgressReporter xfusBlockProgressReporter, long bytesSoFar) : base(xfusApiController, logger)
    {
        _uploadProgress = uploadProgress;
        _xfusBlockProgressReporter = xfusBlockProgressReporter;
        _totalBytesUploaded = bytesSoFar;
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, IProgress<ulong> bytesProgress, CancellationToken ct)
    {
        var uploadProgress = _uploadProgress;
        _logger.LogInformation("XFUS Delta Upload Plan calculated. Will upload {totalBlockBytes} across {pendingBlocks} blocks.", new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes), uploadProgress.PendingBlocks.Length);

        await FullUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, true, httpTimeoutMs, bytesProgress, ct).ConfigureAwait(false);

        if (uploadFile.Length > _totalBytesUploaded)
        {
            ulong bytesSaved = (ulong)(uploadFile.Length - _totalBytesUploaded);
            _logger.LogInformation("Upload complete. Total Uploaded: {totalBytesUploaded} (Saving you in total {totalBytesSaved} in upload bandwidth!)", new ByteSize(_totalBytesUploaded), new ByteSize(bytesSaved));

            // Update progress to account for the bytes we saved.
            bytesProgress?.Report(bytesSaved);
        }
        else
        {
            _logger.LogInformation("Upload complete. Total Uploaded: {totalBytesUploaded}", new ByteSize(_totalBytesUploaded));
        }
        return null;
    }
}
