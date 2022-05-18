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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PackageUploader.ClientApi.Client.Xfus;

internal class XfusUploader : IXfusUploader
{
    public const string HttpClientName = "xfus";

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<XfusUploader> _logger;
    private readonly UploadConfig _uploadConfig;

    public XfusUploader(IHttpClientFactory httpClientFactory, ILogger<XfusUploader> logger, IOptions<UploadConfig> uploadConfig)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uploadConfig = uploadConfig?.Value ?? throw new ArgumentNullException(nameof(uploadConfig));
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

        var uploadProgress = await InitializeAssetAsync(httpClient, xfusUploadInfo.XfusId, uploadFile, deltaUpload, ct).ConfigureAwait(false);
        long totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);
        _logger.LogInformation($"XFUS Asset Initialized. Will upload {new ByteSize(totalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");

        var blockProgressReporter = new BlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
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

                await UploadBlocksAsync(httpClient, uploadProgress.PendingBlocks, maximumBlockSize, uploadFile, xfusUploadInfo.XfusId, blockProgressReporter, ct).ConfigureAwait(false);
            }

            try
            { 
                uploadProgress = await ContinueAssetAsync(httpClient, xfusUploadInfo.XfusId, deltaUpload, ct).ConfigureAwait(false);
                _logger.LogDebug($"UploadProgress.Status after ContinueAsset: {uploadProgress.Status}");

                if (uploadProgress.Status == UploadStatus.ReceivingBlocks)
                {
                    totalBlockBytes = uploadProgress.PendingBlocks.Sum(x => x.Size);
                    totalBytesUploaded += totalBlockBytes;

                    blockProgressReporter = new BlockProgressReporter(_logger, uploadProgress.PendingBlocks.Length, totalBlockBytes);
                    _logger.LogInformation($"XFUS Asset Continuation. Will upload {new ByteSize(totalBlockBytes)} across {uploadProgress.PendingBlocks.Length} blocks.");
                }
                else if (uploadProgress.Status == UploadStatus.Busy)
                {
                    var deltaMessage = deltaUpload ? " (likely waiting for delta plan calculation from XFUS API)" : string.Empty;
                    _logger.LogInformation($"XFUS API is busy and requested we retry in: (HH:MM:SS) {uploadProgress.RequestDelay}...{deltaMessage}");
                    await Task.Delay(uploadProgress.RequestDelay, ct).ConfigureAwait(false);
                }
                else if (uploadProgress.Status == UploadStatus.Completed)
                {
                    _logger.LogTrace($"Upload complete. Total uploaded: {new ByteSize(totalBytesUploaded)}");
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
        _logger.LogInformation($"{uploadFile.Name} Upload complete in: (HH:MM:SS) {timer.Elapsed:hh\\:mm\\:ss}.");
    }

    private async Task<UploadProgress> InitializeAssetAsync(HttpClient httpClient, Guid assetId, FileInfo uploadFile, bool deltaUpload, CancellationToken ct)
    {
        var properties = new UploadProperties
        {
            FileProperties = new FileProperties
            {
                Name = uploadFile.Name,
                Size = uploadFile.Length,
            }
        };

        using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/initialize", deltaUpload, properties);
        using var cts = new CancellationTokenSource(_uploadConfig.HttpTimeoutMs);

        var response = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
        }

        _logger.LogInformation("XFUS AssetId: {assetId}", assetId);
        var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
        return uploadProgress;
    }

    private async Task UploadBlocksAsync(HttpClient httpClient, Block[] blockToBeUploaded, int maxBlockSize,
        FileInfo uploadFile, Guid assetId, BlockProgressReporter blockProgressReporter, CancellationToken ct)
    {
        var bufferPool = new BufferPool(maxBlockSize);
        var actionBlockOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _uploadConfig.MaxParallelism };
        var uploadBlock = new ActionBlock<Block>(async block =>
            {
                byte[] buffer;
                try
                {
                    buffer = bufferPool.GetBuffer(); // Take or create buffer from the pool
                    await using var stream = File.OpenRead(uploadFile.FullName);
                    stream.Seek(block.Offset, SeekOrigin.Begin);
                    var bytesRead = await stream.ReadAsync(buffer, 0, (int)block.Size, ct).ConfigureAwait(false);

                    _logger.LogTrace($"Uploading block {block.Id} with payload: {new ByteSize(bytesRead)}.");

                    // In certain scenarios like delta uploads, or the last chunk in an upload,
                    // the actual chunk size could be less than the largest chunk size.
                    // We need to make sure buffer size matches chunk size otherwise we will get an error
                    // when trying to send http request.
                    Array.Resize(ref buffer, bytesRead);

                    await UploadBlockFromPayloadAsync(httpClient, block.Size, assetId, block.Id, buffer, ct).ConfigureAwait(false);

                    blockProgressReporter.BlocksLeftToUpload--;
                    blockProgressReporter.BytesUploaded += bytesRead;
                    _logger.LogTrace($"Uploaded block {block.Id}. Total uploaded: {new ByteSize(blockProgressReporter.BytesUploaded)} / {new ByteSize(blockProgressReporter.TotalBlockBytes)}.");
                    blockProgressReporter.ReportProgress();
                }
                // Swallow exceptions so other chunk upload can proceed without ActionBlock terminating
                // from a midway-failed chunk upload. We'll re-upload failed chunks later on so this is ok.
                catch (Exception e)
                {
                    _logger.LogTrace($"Block {block.Id} failed, will retry. {e}");
                    return;
                }

                bufferPool.RecycleBuffer(buffer);
            },
            actionBlockOptions);

        foreach (var block in blockToBeUploaded)
        {
            await uploadBlock.SendAsync(block, ct).ConfigureAwait(false);
        }

        uploadBlock.Complete();
        await uploadBlock.Completion.ConfigureAwait(false);
    }

    private static async Task UploadBlockFromPayloadAsync(HttpMessageInvoker httpClient, long contentLength, Guid assetId, long blockId, byte[] payload, CancellationToken ct)
    {
        using var req = CreateStreamRequest(HttpMethod.Put, $"{assetId}/blocks/{blockId}/source/payload", payload, contentLength);

        var response = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
        }
    }

    private static async Task<UploadProgress> ContinueAssetAsync(HttpMessageInvoker httpClient, Guid assetId, bool deltaUpload, CancellationToken ct)
    {
        using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/continue", deltaUpload, string.Empty);

        var response = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
        }
            
        var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
        return uploadProgress;
    }

    private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, bool deltaUpload, T content)
    {
        var request = new HttpRequestMessage(method, url);
        request.Content = new StringContent(JsonSerializer.Serialize(content, DefaultJsonSerializerOptions));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

        if (deltaUpload)
        {
            request.Headers.Add("X-MS-EnableDeltaUploads", "True");
        }

        return request;
    }

    private static HttpRequestMessage CreateStreamRequest(HttpMethod method, string url, byte[] content, long contentLength)
    {
        var request = new HttpRequestMessage(method, url);
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentLength = contentLength;
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);
        return request;
    }

    private class BlockProgressReporter
    {
        private readonly ILogger _logger;

        public int BlocksToUpload { get; }
        public int BlocksLeftToUpload { get; set; }
        public int PercentComplete { get; private set; } = -1;
        public long BytesUploaded { get; set; }
        public long TotalBlockBytes { get; }

        public BlockProgressReporter(ILogger logger, int blocksToUpload, long totalBlockBytes)
        {
            _logger = logger;
            BlocksToUpload = blocksToUpload;
            BlocksLeftToUpload = blocksToUpload;
            TotalBlockBytes = totalBlockBytes;
        }

        public void ReportProgress()
        {
            var ratio = (float)BlocksLeftToUpload / (float)BlocksToUpload;
            var percentage = 100 - (int)Math.Round(100 * ratio);

            if (percentage > PercentComplete)
            {
                PercentComplete = percentage;
                _logger.LogInformation($"Upload {percentage}% complete.");
            }
        }
    }
}