// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Xfus.Config
{
    public class UploadConfig
    {
        public int HttpTimeoutMs { get; set; } = 3000;

        public int HttpUploadTimeoutMs { get; set; } = 300000;

        public int MaxParallelism { get; set; } = 24;

        public int DefaultConnectionLimit { get; set; } = 2;

        public bool Expect100Continue { get; set; } = true;

        public bool UseNagleAlgorithm { get; set; } = true;
    }
}
