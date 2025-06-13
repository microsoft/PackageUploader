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

internal class DeltaUploadHeaderState : XfusUploaderState
{
    private readonly UploadProgress _uploadProgress;

    internal DeltaUploadHeaderState(XfusApiController xfusApiController, ILogger logger, UploadProgress uploadProgress, XfusBlockProgressReporter xfusBlockProgressReporter, long bytesSoFar) : base(xfusApiController, logger)
    {
        _uploadProgress = uploadProgress;
        _xfusBlockProgressReporter = xfusBlockProgressReporter;
        _totalBytesUploaded = bytesSoFar;
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, IProgress<ulong> bytesProgress, CancellationToken ct)
    {
        var uploadProgress = _uploadProgress;
        _logger.LogInformation("XFUS Asset Initialized. Uploading Delta Plan Main Header {totalBlockBytes} across {pendingBlocks} blocks.", new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes), uploadProgress.PendingBlocks.Length);

        uploadProgress = await StepUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, true, httpTimeoutMs, bytesProgress, ct).ConfigureAwait(false);

        return new DeltaUploadPlanState(_xfusApiController, _logger, uploadProgress, _xfusBlockProgressReporter, _totalBytesUploaded);
    }
}
