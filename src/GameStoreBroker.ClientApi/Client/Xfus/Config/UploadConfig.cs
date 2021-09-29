// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Xfus.Config
{
    public class UploadConfig
    {
        public int HttpTimeoutMs { get; set; } = 5000;

        public int HttpUploadTimeoutMs { get; set; } = 300000;

        public int MaxParallelism { get; set; } = 24;

        public int DefaultConnectionLimit { get; set; } = -1;

        public bool Expect100Continue { get; set; } = false;

        public bool UseNagleAlgorithm { get; set; } = false;
    }
}
