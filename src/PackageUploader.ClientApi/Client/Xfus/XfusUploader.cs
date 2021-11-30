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

namespace PackageUploader.ClientApi.Client.Xfus
{
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

        public async Task UploadFileToXfusAsync(FileInfo uploadFile, XfusUploadInfo xfusUploadInfo, CancellationToken ct)
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

            var uploadProgress = await InitializeAssetAsync(httpClient, xfusUploadInfo.XfusId, uploadFile, ct).ConfigureAwait(false);
            _logger.LogDebug($"XFUS Asset Initialized. Will upload {new ByteSize(uploadFile.Length)} across {uploadProgress.PendingBlocks.Length} blocks.");
            _logger.LogInformation($"XFUS Asset Initialized. Will upload {uploadFile.Name} at size of {new ByteSize(uploadFile.Length)}.");

            var progress = new Progress(_logger, uploadProgress.PendingBlocks.Length);
            progress.ReportProgress();
            long bytesUploaded = 0;
            while (uploadProgress.Status != UploadStatus.Completed)
            {
                if (uploadProgress.Status == UploadStatus.Busy)
                {
                    await Task.Delay(uploadProgress.RequestDelay, ct).ConfigureAwait(false);
                }
                else if (uploadProgress.Status == UploadStatus.ReceivingBlocks &&
                    uploadProgress.PendingBlocks != null && uploadProgress.PendingBlocks.Any())
                {
                    // We want to use the biggest block size because it is most likely to be the most frequent block size
                    // among different upload scenarios. In addition, by over-allocating memory, we minimize potential
                    // memory usage spikes during the middle of an upload as blocks are created and reused.
                    var maximumBlockSize = (int)uploadProgress.PendingBlocks.Max(x => x.Size);

                    bytesUploaded = await UploadBlocksAsync(httpClient, uploadProgress.PendingBlocks, maximumBlockSize,
                            uploadFile, xfusUploadInfo.XfusId, bytesUploaded, progress, ct)
                        .ConfigureAwait(false);
                }

                try
                {
                    uploadProgress = await ContinueAssetAsync(httpClient, xfusUploadInfo.XfusId, ct).ConfigureAwait(false);
                    progress.BlocksLeftToUpload = uploadProgress.PendingBlocks?.Length ?? 0;
                    progress.ReportProgress();

                    if (uploadProgress.Status == UploadStatus.Busy)
                    {
                        await Task.Delay(uploadProgress.RequestDelay, ct).ConfigureAwait(false);
                    }
                    else if (uploadProgress.Status == UploadStatus.Completed)
                    {
                        _logger.LogTrace($"Upload complete. Bytes: {bytesUploaded}");
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

            var hours = timer.Elapsed.Hours < 10 ? $"0{timer.Elapsed.Hours}" : $"{timer.Elapsed.Hours}";
            var minutes = timer.Elapsed.Minutes < 10 ? $"0{timer.Elapsed.Minutes}" : $"{timer.Elapsed.Minutes}";
            var seconds = timer.Elapsed.Seconds < 10 ? $"0{timer.Elapsed.Seconds}" : $"{timer.Elapsed.Seconds}";

            _logger.LogInformation($"{uploadFile.Name} Upload complete in: (HH:MM:SS) {hours}:{minutes}:{seconds}.");
        }

        private async Task<UploadProgress> InitializeAssetAsync(HttpClient httpClient, Guid assetId, FileInfo uploadFile, CancellationToken ct)
        {
            var properties = new UploadProperties
            {
                FileProperties = new FileProperties
                {
                    Name = uploadFile.Name,
                    Size = uploadFile.Length,
                }
            };

            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/initialize", properties);
            using var cts = new CancellationTokenSource(_uploadConfig.HttpTimeoutMs);

            var response = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }

            var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
            return uploadProgress;
        }

        private async Task<long> UploadBlocksAsync(HttpClient httpClient, Block[] blockToBeUploaded, int maxBlockSize,
            FileInfo uploadFile, Guid assetId, long bytesUploaded, Progress progress, CancellationToken ct)
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

                        progress.BlocksLeftToUpload--;
                        bytesUploaded += bytesRead;
                        _logger.LogTrace($"Uploaded block {block.Id}. Total uploaded: {new ByteSize(bytesUploaded)} / {new ByteSize(uploadFile.Length)}.");
                        progress.ReportProgress();
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
            return bytesUploaded;
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

        private static async Task<UploadProgress> ContinueAssetAsync(HttpMessageInvoker httpClient, Guid assetId, CancellationToken ct)
        {
            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/continue", string.Empty);

            var response = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }
            
            var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
            return uploadProgress;
        }

        private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, T content)
        {
            var request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(JsonSerializer.Serialize(content, DefaultJsonSerializerOptions));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
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

        private class Progress
        {
            private readonly ILogger _logger;

            public int BlocksToUpload { get; }
            public int BlocksLeftToUpload { get; set; }
            public int PercentComplete { get; private set; }

            public Progress(ILogger logger, int blocksToUpload)
            {
                _logger = logger;
                BlocksToUpload = blocksToUpload;
                BlocksLeftToUpload = blocksToUpload;
                PercentComplete = -1;
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
}
