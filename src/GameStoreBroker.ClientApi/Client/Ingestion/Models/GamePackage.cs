// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Xfus.Models;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GamePackage
    {
        public string Id { get; internal set; }

        public string State { get; set; }

        public XfusUploadInfo UploadInfo { get; set; }
    }
}
