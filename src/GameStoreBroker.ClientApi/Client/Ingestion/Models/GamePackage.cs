// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Xfus.Models;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class GamePackage
    {
        public string Id { get; internal init; }

        public GamePackageState State { get; internal init; }

        public XfusUploadInfo UploadInfo { get; internal init; }

        public string ODataETag { get; internal init; }
    }
}
