// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Exceptions;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader.State;

internal abstract class XfusUploaderState
{
    internal abstract Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, CancellationToken ct);

    protected XfusApiController _xfusApiController;
    protected ILogger _logger;

    protected XfusBlockProgressReporter _xfusBlockProgressReporter;
    protected long _totalBytesUploaded;

    protected XfusUploaderState(XfusApiController xfusApiController, ILogger logger)
    {
        _xfusApiController = xfusApiController;
        _logger = logger;
    }

    protected async Task<UploadProgress> InitializeAssetAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, bool deltaUpload, CancellationToken ct)
    {
        var uploadProgress = await _xfusApiController.InitializeAssetAsync(xfusUploadInfo.XfusId, uploadFile, deltaUpload, ct).ConfigureAwait(false);
        long totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);

        _xfusBlockProgressReporter = new XfusBlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
        _xfusBlockProgressReporter.ReportProgress();

        return uploadProgress;
    }

    protected async Task FullUploadAsync(UploadProgress uploadProgress, XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, bool deltaUpload, int httpTimeoutMs, CancellationToken ct)
    {
        var firstRun = true;
        while (uploadProgress.Status != UploadStatus.Completed)
        {
            await UploadBlocksAsync(uploadProgress, xfusUploadInfo, uploadFile, ct).ConfigureAwait(false);
            uploadProgress = await CheckContinuationAsync(uploadProgress, xfusUploadInfo, deltaUpload, httpTimeoutMs, ct).ConfigureAwait(false);

            if (!firstRun)
            {
                var totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);
                _logger.LogInformation($"XFUS Asset Continuation requested. Will upload {new ByteSize(totalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");
            }
            firstRun = false;
        }
    }

    protected async Task<UploadProgress> StepUploadAsync(UploadProgress uploadProgress, XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, bool deltaUpload, int httpTimeoutMs, CancellationToken ct)
    {
        var continuationBlockComplete = false;
        while (!continuationBlockComplete || uploadProgress == null)
        {
            await UploadBlocksAsync(uploadProgress, xfusUploadInfo, uploadFile, ct).ConfigureAwait(false);
            if (_xfusBlockProgressReporter.BlocksLeftToUpload <= 0)
            {
                continuationBlockComplete = true;
            }
            uploadProgress = await CheckContinuationAsync(uploadProgress, xfusUploadInfo, deltaUpload, httpTimeoutMs, ct).ConfigureAwait(false);
        }
        return uploadProgress;
    }

    private async Task UploadBlocksAsync(UploadProgress uploadProgress, XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, CancellationToken ct)
    {
        var validBlocks = uploadProgress != null && uploadProgress.PendingBlocks != null && uploadProgress.PendingBlocks.Any();
        if (validBlocks && uploadProgress.Status == UploadStatus.ReceivingBlocks)
        {
            await _xfusApiController.UploadBlocksAsync(uploadProgress.PendingBlocks, uploadFile, xfusUploadInfo.XfusId, _xfusBlockProgressReporter, ct).ConfigureAwait(false);
        }
    }

    private async Task<UploadProgress> CheckContinuationAsync(UploadProgress uploadProgress, XfusUploadInfo xfusUploadInfo, bool deltaUpload, int httpTimeoutMs, CancellationToken ct)
    {
        try
        {
            uploadProgress = await _xfusApiController.ContinueAssetAsync(xfusUploadInfo.XfusId, deltaUpload, ct).ConfigureAwait(false);

            if (uploadProgress.Status == UploadStatus.ReceivingBlocks)
            {
                var totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);

                _totalBytesUploaded += _xfusBlockProgressReporter.BytesUploaded;
                _xfusBlockProgressReporter = new XfusBlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
            }
            else if (uploadProgress.Status == UploadStatus.Busy)
            {
                _logger.LogInformation($"XFUS API is busy and requested we retry in: (HH:MM:SS) {uploadProgress.RequestDelay}...");
                await Task.Delay(uploadProgress.RequestDelay, ct).ConfigureAwait(false);
            }
        }
        catch (XfusServerException serverException)
        {
            _logger.LogDebug($"Exception: {serverException}");
            if (serverException.IsRetryable || serverException.HttpStatusCode == HttpStatusCode.ServiceUnavailable)
            {
                await Task.Delay(
                    serverException.RetryAfter.TotalMilliseconds > 0
                        ? serverException.RetryAfter
                        : new TimeSpan(httpTimeoutMs), ct).ConfigureAwait(false);
            }
            uploadProgress = null;
        }

        return uploadProgress;
    }
}
