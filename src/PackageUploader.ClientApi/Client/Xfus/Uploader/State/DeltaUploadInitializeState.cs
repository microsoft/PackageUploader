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

internal class DeltaUploadInitializeState : XfusUploaderState
{
    internal DeltaUploadInitializeState(XfusApiController xfusApiController, ILogger logger) : base(xfusApiController, logger)
    {
    }

    internal override async Task<XfusUploaderState> UploadAsync(XfusUploadInfo xfusUploadInfo, FileInfo uploadFile, int httpTimeoutMs, IProgress<ulong> bytesProgress, CancellationToken ct)
    {
        var uploadProgress = await InitializeAssetAsync(xfusUploadInfo, uploadFile, true, ct).ConfigureAwait(false);
        _logger.LogInformation("XFUS Asset Initializing. Uploading Delta Plan Pre-Header {totalBlockBytes} across {pendingBlocks} blocks.", new ByteSize(_xfusBlockProgressReporter.TotalBlockBytes), uploadProgress.PendingBlocks.Length);

        uploadProgress = await StepUploadAsync(uploadProgress, xfusUploadInfo, uploadFile, true, httpTimeoutMs, bytesProgress, ct).ConfigureAwait(false);

        return new DeltaUploadHeaderState(_xfusApiController, _logger, uploadProgress, _xfusBlockProgressReporter, _totalBytesUploaded);
    }
}