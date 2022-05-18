// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader.State;

internal class DeltaUploadPlanState : XfusUploaderState
{
    private readonly UploadProgress _uploadProgress;

    internal DeltaUploadPlanState(XfusApiController xfusApiController, ILogger logger, UploadProgress uploadProgress, XfusBlockProgressReporter xfusBlockProgressReporter, long bytesSoFar) : base(xfusApiController, logger)
    {
        _uploadProgress = uploadProgress;
        _xfusBlockProgressReporter = xfusBlockProgressReporter;
        _totalBytesUploaded = bytesSoFar;
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, CancellationToken ct)
    {
        var uploadProgress = _uploadProgress;
        _logger.LogInformation($"XFUS Asset Header uploaded. Please wait while the API calculates the Delta Upload Plan.");

        while (uploadProgress.Status == UploadStatus.Busy)
        {
            _logger.LogInformation($"XFUS Asset Header uploaded. Delta Upload Plan still calculating, retrying in (HH:MM:SS) {uploadProgress.RequestDelay}.");
            uploadProgress = await StepUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, true, httpTimeoutMs, ct).ConfigureAwait(false);
        }

        return new DeltaUploadMainUploadState(_xfusApiController, _logger, uploadProgress, _xfusBlockProgressReporter, _totalBytesUploaded);
    }
}
