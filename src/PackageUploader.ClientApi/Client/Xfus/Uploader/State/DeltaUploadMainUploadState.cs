// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
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

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, CancellationToken ct)
    {
        var uploadProgress = _uploadProgress;
        _logger.LogInformation($"XFUS Delta Upload Plan calculated. Will upload {new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");

        await FullUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, true, httpTimeoutMs, ct).ConfigureAwait(false);

        _logger.LogTrace($"Upload complete. Total Uploaded: {new ByteSize(_totalBytesUploaded)} (Saving you in total {new ByteSize(uploadFile.Length - _totalBytesUploaded)} in upload bandwidth!)");
        return null;
    }
}
