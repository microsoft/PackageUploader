// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Xfus.Config;
using PackageUploader.ClientApi.Client.Xfus.Exceptions;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus;

internal class XfusUploader : IXfusUploader
{
    public const string HttpClientName = "xfus";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<XfusUploader> _logger;
    private readonly UploadConfig _uploadConfig;
    private readonly XfusApiController _xfusApiController;

    public XfusUploader(IHttpClientFactory httpClientFactory, ILogger<XfusUploader> logger, IOptions<UploadConfig> uploadConfig)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uploadConfig = uploadConfig?.Value ?? throw new ArgumentNullException(nameof(uploadConfig));
        _xfusApiController = new XfusApiController(_logger, _uploadConfig);
    }

    public async Task UploadFileToXfusAsync(FileInfo uploadFile, XfusUploadInfo xfusUploadInfo, bool deltaUpload, CancellationToken ct)
    {
        if (!uploadFile.Exists)
        {
            throw new FileNotFoundException("Upload file not found.", uploadFile.FullName);
        }

        var timer = new Stopwatch();
        timer.Start();

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(xfusUploadInfo.UploadDomain + "/api/v2/assets/");
        httpClient.DefaultRequestHeaders.Add("Tenant", xfusUploadInfo.XfusTenant);

        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xfusUploadInfo.Token));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authToken);

        var uploadProgress = await _xfusApiController.InitializeAssetAsync(httpClient, xfusUploadInfo.XfusId, uploadFile, deltaUpload, ct).ConfigureAwait(false);
        long totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);
        _logger.LogInformation($"XFUS Asset Initialized. Will upload {new ByteSize(totalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");

        var blockProgressReporter = new XfusBlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
        blockProgressReporter.ReportProgress();
        long totalBytesUploaded = totalBlockBytes;

        while (uploadProgress.Status != UploadStatus.Completed)
        {
            var validBlocks = uploadProgress.PendingBlocks != null && uploadProgress.PendingBlocks.Any();
            if (uploadProgress.Status == UploadStatus.ReceivingBlocks && validBlocks)
            {
                // We want to use the biggest block size because it is most likely to be the most frequent block size
                // among different upload scenarios. In addition, by over-allocating memory, we minimize potential
                // memory usage spikes during the middle of an upload as blocks are created and reused.
                var maximumBlockSize = (int)uploadProgress.PendingBlocks.Max(x => x.Size);

                await _xfusApiController.UploadBlocksAsync(httpClient, uploadProgress.PendingBlocks, maximumBlockSize, uploadFile, xfusUploadInfo.XfusId, blockProgressReporter, ct).ConfigureAwait(false);
            }

            try
            {
                uploadProgress = await _xfusApiController.ContinueAssetAsync(httpClient, xfusUploadInfo.XfusId, deltaUpload, ct).ConfigureAwait(false);

                if (uploadProgress.Status == UploadStatus.ReceivingBlocks)
                {
                    totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);
                    totalBytesUploaded += totalBlockBytes;

                    blockProgressReporter = new XfusBlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
                    _logger.LogInformation($"XFUS Asset Continuation. Will upload {new ByteSize(totalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");
                }
                else if (uploadProgress.Status == UploadStatus.Busy)
                {
                    var deltaMessage = deltaUpload ? " (likely waiting for delta plan calculation from XFUS API)" : string.Empty;
                    _logger.LogInformation($"XFUS API is busy and requested we retry in: (HH:MM:SS) {uploadProgress.RequestDelay}...{deltaMessage}");
                    await Task.Delay(uploadProgress.RequestDelay, ct).ConfigureAwait(false);
                }
            }
            catch (XfusServerException serverException)
            {
                if (serverException.IsRetryable || serverException.HttpStatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(
                        serverException.RetryAfter.TotalMilliseconds > 0
                            ? serverException.RetryAfter
                            : new TimeSpan(_uploadConfig.HttpTimeoutMs), ct).ConfigureAwait(false);
                }
            }
        }

        timer.Stop();
        _logger.LogTrace($"Upload complete. Total uploaded: {new ByteSize(totalBytesUploaded)}");
        _logger.LogInformation($"{uploadFile.Name} Upload complete in: (HH:MM:SS) {timer.Elapsed:hh\\:mm\\:ss}.");
    }

    private enum DeltaState
    {
        NoDelta,
        Initialize,
        Header,
        DeltaPlan,
        Continuation,
        Complete
    }
}