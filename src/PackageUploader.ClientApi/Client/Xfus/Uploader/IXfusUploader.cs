// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Xfus.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader;

public interface IXfusUploader
{
    Task UploadFileToXfusAsync(FileInfo uploadFile, XfusUploadInfo xfusUploadInfo, bool deltaUpload, IProgress<ulong> bytesProgress, CancellationToken ct);
}