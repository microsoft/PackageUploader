// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

    internal async Task UploadBlocksAsync(Block[] blockToBeUploaded, FileInfo uploadFile, Guid assetId, XfusBlockProgressReporter blockProgressReporter, IProgress<ulong> bytesProgress, CancellationToken ct)
    {
        // We want to use the biggest block size because it is most likely to be the most frequent block size
        // among different upload scenarios. In addition, by over-allocating memory, we minimize potential
        // memory usage spikes during the middle of an upload as blocks are created and reused.
        var maximumBlockSize = (int)blockToBeUploaded.Max(x => x.Size);

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

                await UploadBlockFromPayloadAsync(block.Size, assetId, block.Id, buffer, ct).ConfigureAwait(false);

                blockProgressReporter.BlocksLeftToUpload--;
                blockProgressReporter.BytesUploaded += bytesRead;
                _logger.LogTrace("Uploaded block {blockId}. Total uploaded: {bytesUploaded} / {totalBlockBytes}.", block.Id, new ByteSize(blockProgressReporter.BytesUploaded), new ByteSize(blockProgressReporter.TotalBlockBytes));
                blockProgressReporter.ReportProgress();
                bytesProgress?.Report((ulong)bytesRead);
            }
            // Swallow exceptions so other chunk upload can proceed without ActionBlock terminating
            // from a midway-failed chunk upload. We'll re-upload failed chunks later on so this is ok.
            catch (Exception e)
            {
                _logger.LogTrace(e, "Block {blockId} failed, will retry.", block.Id);
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

    internal async Task UploadBlockFromPayloadAsync(long contentLength, Guid assetId, long blockId, byte[] payload, CancellationToken ct)
    {
        using var req = CreateStreamRequest(HttpMethod.Put, $"{assetId}/blocks/{blockId}/source/payload", payload, contentLength);

        var response = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new XfusServerException(response.StatusCode, response.ReasonPhrase);
        }
    }

    internal async Task<UploadProgress> InitializeAssetAsync(Guid assetId, FileInfo uploadFile, bool deltaUpload, CancellationToken ct)
    {
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

        _logger.LogInformation("XFUS AssetId: {assetId}", assetId);
        var uploadProgress = await response.Content.ReadFromJsonAsync(XfusJsonSerializerContext.Default.UploadProgress, ct).ConfigureAwait(false);
        return uploadProgress;
    }

    internal async Task<UploadProgress> ContinueAssetAsync(Guid assetId, bool deltaUpload, CancellationToken ct)
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

    private static HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string url, bool deltaUpload, T content, JsonTypeInfo<T> jsonTypeInfo)
    {
        var request = new HttpRequestMessage(method, url);
        request.Content = new StringContent(JsonSerializer.Serialize(content, jsonTypeInfo));
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
}
