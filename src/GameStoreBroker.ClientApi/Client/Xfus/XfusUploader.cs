// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Xfus.Exceptions;
using GameStoreBroker.ClientApi.Client.Xfus.Models;
using GameStoreBroker.ClientApi.Models;
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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameStoreBroker.ClientApi.Client.Xfus
{
    internal class XfusUploader : IXfusUploader
    {
        public const string HttpClientName = "xfus";

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<XfusUploader> _logger;
        private readonly UploadConfig _uploadConfig;

        private int _blocksToUpload;
        private int _blocksLeftToUpload;
        private int _percentComplete = -1;

        public XfusUploader(IHttpClientFactory httpClientFactory, ILogger<XfusUploader> logger, IOptions<UploadConfig> uploadConfig)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _uploadConfig = uploadConfig.Value;
        }

        public async Task UploadFileToXfusAsync(FileInfo uploadFile, XfusUploadInfo xfusUploadInfo, CancellationToken ct)
        {
            var properties = new UploadProperties
            {
                FileProperties = new FileProperties
                {
                    Name = uploadFile.Name,
                    Size = uploadFile.Length,
                }
            };
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xfusUploadInfo.Token));
            var assetGuid = new Guid(xfusUploadInfo.XfusId);

            var timer = new Stopwatch();
            timer.Start();

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            httpClient.BaseAddress = new Uri(xfusUploadInfo.UploadDomain + "/api/v2/assets/");

            var uploadProgress = await InitializeAssetAsync(httpClient, authToken, xfusUploadInfo.XfusTenant, assetGuid, properties, ct).ConfigureAwait(false);
            _logger.LogDebug($"XFUS Asset Initialized. Will upload {new ByteSize(uploadFile.Length)} across {uploadProgress.PendingBlocks.Length} blocks.");
            _logger.LogInformation($"XFUS Asset Initialized. Will upload {uploadFile.Name} at size of {new ByteSize(uploadFile.Length)}.");

            _blocksToUpload = uploadProgress.PendingBlocks.Length;
            _blocksLeftToUpload = _blocksToUpload;
            ReportProgress();
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

                    bytesUploaded = await UploadBlocksAsync(httpClient, uploadProgress.PendingBlocks, maximumBlockSize, uploadFile, assetGuid, authToken, xfusUploadInfo.XfusTenant, bytesUploaded, ct).ConfigureAwait(false);
                }

                try
                {
                    uploadProgress = await ContinueAssetAsync(httpClient, authToken, xfusUploadInfo.XfusTenant, assetGuid, ct).ConfigureAwait(false);
                    _blocksLeftToUpload = uploadProgress.PendingBlocks?.Length ?? 0;
                    ReportProgress();

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
                        await Task.Delay(serverException.RetryAfter.TotalMilliseconds > 0 ? serverException.RetryAfter : new TimeSpan(_uploadConfig.HttpTimeoutMs), ct).ConfigureAwait(false);
                    }
                }
            }

            timer.Stop();

            var hours = timer.Elapsed.Hours < 10 ? $"0{timer.Elapsed.Hours}" : $"{timer.Elapsed.Hours}";
            var minutes = timer.Elapsed.Minutes < 10 ? $"0{timer.Elapsed.Minutes}" : $"{timer.Elapsed.Minutes}";
            var seconds = timer.Elapsed.Seconds < 10 ? $"0{timer.Elapsed.Seconds}" : $"{timer.Elapsed.Seconds}";

            _logger.LogInformation($"{uploadFile.Name} Upload complete in: (HH:MM:SS) {hours}:{minutes}:{seconds}.");
        }

        private async Task<UploadProgress> InitializeAssetAsync(HttpClient httpClient, string authToken, string tenant, Guid assetId, UploadProperties properties, CancellationToken ct)
        {
            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/initialize", authToken, tenant, properties);
            using var cts = new CancellationTokenSource(_uploadConfig.HttpTimeoutMs);

            var response = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }

            var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct);
            return uploadProgress;
        }

        private async Task<long> UploadBlocksAsync(HttpClient httpClient, Block[] blockToBeUploaded, int maxBlockSize,
            FileInfo uploadFile, Guid assetId, string authToken, string tenant, long bytesUploaded, CancellationToken ct)
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

                        await UploadBlockFromPayloadAsync(httpClient, authToken, tenant, block.Size, assetId, block.Id, buffer, ct).ConfigureAwait(false);

                        _blocksLeftToUpload--;
                        bytesUploaded += bytesRead;
                        _logger.LogTrace($"Uploaded block {block.Id}. Total uploaded: {new ByteSize(bytesUploaded)} / {new ByteSize(uploadFile.Length)}.");
                        ReportProgress();
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

        private static async Task UploadBlockFromPayloadAsync(HttpClient httpClient, string uploadToken, string tenant, long contentLength, Guid assetId, long blockId, byte[] payload, CancellationToken ct)
        {
            using var req = CreateStreamRequest(HttpMethod.Put, $"{assetId}/blocks/{blockId}/source/payload", uploadToken, tenant, payload, contentLength);

            var response = await httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }
        }

        private static async Task<UploadProgress> ContinueAssetAsync(HttpClient httpClient, string uploadToken, string tenant, Guid assetId, CancellationToken ct)
        {
            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/continue", uploadToken, tenant, string.Empty);

            var response = await httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }
            
            var uploadProgress = await response.Content.ReadFromJsonAsync<UploadProgress>(DefaultJsonSerializerOptions, ct);
            return uploadProgress;
        }

        private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, string token, string tenant, T content)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", token);
            request.Headers.Add("Tenant", tenant);
            request.Content = new StringContent(JsonSerializer.Serialize(content, DefaultJsonSerializerOptions));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            return request;
        }

        private static HttpRequestMessage CreateStreamRequest(HttpMethod method, string url, string token, string tenant, byte[] content, long contentLength)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", token);
            request.Headers.Add("Tenant", tenant);
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentLength = contentLength;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);
            return request;
        }

        private void ReportProgress()
        {
            var ratio = (float)_blocksLeftToUpload / (float)_blocksToUpload;
            var percentage = 100 - (int)Math.Round(100 * ratio);

            if (percentage > _percentComplete)
            {
                _percentComplete = percentage;
                _logger.LogInformation($"Upload {percentage}% complete.");
            }
        }
    }
}
