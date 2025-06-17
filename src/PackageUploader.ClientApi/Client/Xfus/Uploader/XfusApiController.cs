// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Xfus.Config;
using PackageUploader.ClientApi.Client.Xfus.Exceptions;
using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader;

internal class XfusApiController
{
    private readonly ILogger<XfusUploader> _logger;
    private readonly UploadConfig _uploadConfig;
    private readonly HttpClient _httpClient;

    public XfusApiController(ILogger<XfusUploader> logger, HttpClient httpClient, UploadConfig uploadConfig)
    {
        _logger = logger;
        _uploadConfig = uploadConfig;
        _httpClient = httpClient;
    }

    internal async Task UploadBlocksAsync(UploadProgress uploadProgress, FileInfo uploadFile, Guid assetId, XfusBlockProgressReporter blockProgressReporter, CancellationToken ct)
    {
        // We want to use the biggest block size because it is most likely to be the most frequent block size
        // among different upload scenarios. In addition, by over-allocating memory, we minimize potential
        // memory usage spikes during the middle of an upload as blocks are created and reused.
        var maximumBlockSize = (int)uploadProgress.PendingBlocks.Max(x => x.Size);

        var bufferPool = new BufferPool(maximumBlockSize);
        var actionBlockOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _uploadConfig.MaxParallelism };
        var uploadBlock = new ActionBlock<Block>(async block =>
        {
            byte[] buffer;
            try
            {
                buffer = bufferPool.GetBuffer(); // Take or create buffer from the pool
                await using var stream = File.OpenRead(uploadFile.FullName);
                stream.Seek(block.Offset, SeekOrigin.Begin);
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)block.Size), ct).ConfigureAwait(false);

                _logger.LogTrace("Uploading block {blockId} with payload: {bytesRead}.", block.Id, new ByteSize(bytesRead));

                // In certain scenarios like delta uploads, or the last chunk in an upload,
                // the actual chunk size could be less than the largest chunk size.
                // We need to make sure buffer size matches chunk size otherwise we will get an error
                // when trying to send http request.
                Array.Resize(ref buffer, bytesRead);

                if (uploadProgress.DirectUploadParameters != null && uploadProgress.DirectUploadParameters.SasUri != null)
                {
                    await UploadBlockFromPayloadAsync(assetId, block, buffer, uploadProgress.DirectUploadParameters.SasUri, bytesRead, ct).ConfigureAwait(false);
                }
                else
                {
                    await UploadBlockFromPayloadAsync(bytesRead, assetId, block.Id, buffer, ct).ConfigureAwait(false);
                }

                blockProgressReporter.BlocksLeftToUpload--;
                blockProgressReporter.BytesUploaded += bytesRead;
                _logger.LogTrace("Uploaded block {blockId}. Total uploaded: {bytesUploaded} / {totalBlockBytes}.", block.Id, new ByteSize(blockProgressReporter.BytesUploaded), new ByteSize(blockProgressReporter.TotalBlockBytes));
                blockProgressReporter.ReportProgress();
            }
            // Swallow exceptions so other chunk upload can proceed without ActionBlock terminating
            // from a midway-failed chunk upload. We'll re-upload failed chunks later on so this is ok.
            catch (Exception e)
            {
                _logger.LogTrace(e, "Block {blockId} failed, will retry.", block.Id);
                LogXfusExceptionDetails(e, "XFUS block upload", assetId, block.Id);
                return;
            }

            bufferPool.RecycleBuffer(buffer);
        },
            actionBlockOptions);

        foreach (var block in uploadProgress.PendingBlocks)
        {
            await uploadBlock.SendAsync(block, ct).ConfigureAwait(false);
        }

        uploadBlock.Complete();
        await uploadBlock.Completion.ConfigureAwait(false);
    }

    internal async Task UploadBlockFromPayloadAsync(long contentLength, Guid assetId, long blockId, byte[] payload, CancellationToken ct)
    {
        try
        {
            using var req = CreateStreamRequest(HttpMethod.Put, $"{assetId}/blocks/{blockId}/source/payload", payload, contentLength);

            var response = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception exception)
        {
            LogXfusExceptionDetails(exception, "XFUS block payload upload", assetId, blockId);
            throw;
        }
    }

    internal async Task UploadBlockFromPayloadAsync(Guid assetId, Block block, byte[] payload, string sasUri, int bytesRead, CancellationToken ct)
    {
        try
        {
            BlockBlobClient blobClient = new(new Uri(sasUri));
            using var chunkStream = new MemoryStream(payload, 0, bytesRead);

            await blobClient.StageBlockAsync(block.BlockIdBase64, chunkStream, null, ct);
        }
        catch (Exception exception)
        {
            LogXfusExceptionDetails(exception, "XFUS block payload upload", assetId, block.Id);
            throw;
        }
    }

    internal async Task<UploadProgress> InitializeAssetAsync(Guid assetId, FileInfo uploadFile, bool deltaUpload, CancellationToken ct)
    {
        try {
            _logger.LogInformation("Calling XFUS with AssetId: {assetId}", assetId);

            var properties = new UploadProperties
            {
                FileProperties = new FileProperties
                {
                    Name = uploadFile.Name,
                    Size = uploadFile.Length,
                }
            };

            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/initialize", deltaUpload, properties, XfusJsonSerializerContext.Default.UploadProperties);
            using var cts = new CancellationTokenSource(_uploadConfig.HttpTimeoutMs);

            var response = await _httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }

            var uploadProgress = await response.Content.ReadFromJsonAsync(XfusJsonSerializerContext.Default.UploadProgress, ct).ConfigureAwait(false);

            _logger.LogInformation("Asset initialized as {} upload", uploadProgress.DirectUploadParameters != null ? "direct" : "proxy");
            return uploadProgress;
        }
        catch (Exception exception) {
            LogXfusExceptionDetails(exception, "XFUS asset initialization", assetId);
            throw;
        }
    }

    internal async Task<UploadProgress> ContinueAssetAsync(Guid assetId, bool deltaUpload, CancellationToken ct)
    {
        try
        {
            using var req = CreateJsonRequest(HttpMethod.Post, $"{assetId}/continue", deltaUpload, string.Empty, XfusJsonSerializerContext.Default.String);

            var response = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
            }

            var uploadProgress = await response.Content.ReadFromJsonAsync(XfusJsonSerializerContext.Default.UploadProgress, ct).ConfigureAwait(false);
            return uploadProgress;
        }
        catch (Exception exception)
        {
            LogXfusExceptionDetails(exception, "XFUS asset continuation", assetId);
            throw;
        }
    }

    private void LogXfusExceptionDetails(Exception exception, string operationType, Guid assetId, long? blockId = null)
    {
        var blockIdInfo = blockId.HasValue ? $" - Block ID: {blockId}" : "";
        
        _logger.LogError(exception, 
            "XFUS Endpoint Error: {operationType} failed for AssetId: {assetId}{blockIdInfo}.\n" +
            "To report this issue:\n" +
            "1. Save the log file\n" +
            "2. Contact the Package Uploader support team with the AssetId and error details\n" + 
            "3. Consider opening a support ticket with Partner Center referencing the XFUS failure\n",
            operationType, assetId, blockIdInfo);
    }

    private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, bool deltaUpload, T content, JsonTypeInfo<T> jsonTypeInfo)
    {
        var request = new HttpRequestMessage(method, url);
        request.Content = new StringContent(JsonSerializer.Serialize(content, jsonTypeInfo));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);

        if (deltaUpload)
        {
            request.Headers.Add("X-MS-EnableDeltaUploads", "True");
            request.Headers.Add("UploadMethod", "2"); // 2 indicates delta upload using direct upload method
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
}
