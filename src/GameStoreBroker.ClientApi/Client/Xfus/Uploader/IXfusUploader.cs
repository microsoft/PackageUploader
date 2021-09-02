// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.ClientApi.Client.Xfus.Uploader
{
    public interface IXfusUploader
    {
        Task UploadFileToXfusAsync(string filePath, XfusUploadInfo xfusUploadInfo);
    }
}
