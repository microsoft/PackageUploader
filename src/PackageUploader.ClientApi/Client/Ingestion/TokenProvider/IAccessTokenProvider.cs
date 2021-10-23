// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessToken();
    }
}