// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Models
{
    public class GamePackage
    {
        public string Id { get; internal set; }

        public string State { get; set; }

        public XfusUploadInfo UploadInfo { get; set; }
    }
}
