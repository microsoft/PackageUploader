// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Xfus.Config;
using PackageUploader.ClientApi.Client.Xfus.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader.State;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader;

internal class XfusUploader : IXfusUploader
{
    public const string HttpClientName = "xfus";

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

        var httpClient = SetupHttpClient(xfusUploadInfo);
        var xfusApiController = new XfusApiController(_logger, httpClient, _uploadConfig);

        XfusUploaderState xfusUploaderState = deltaUpload ? new DeltaUploadInitializeState(xfusApiController, _logger) : new NoDeltaUploadState(xfusApiController, _logger);

        while (xfusUploaderState != null)
        {
            xfusUploaderState = await xfusUploaderState.UploadAsync(xfusUploadInfo, uploadFile, _uploadConfig.HttpTimeoutMs, ct).ConfigureAwait(false);
        }

        timer.Stop();
        _logger.LogInformation("{uploadFileName} Upload complete in: (HH:MM:SS) {timerElapsed}.", uploadFile.Name, timer.Elapsed.ToString("hh:mm:ss"));
    }

    private HttpClient SetupHttpClient(XfusUploadInfo xfusUploadInfo)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(xfusUploadInfo.UploadDomain + "/api/v2/assets/");
        httpClient.DefaultRequestHeaders.Add("Tenant", xfusUploadInfo.XfusTenant);

        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xfusUploadInfo.Token));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authToken);

        return httpClient;
    }
}