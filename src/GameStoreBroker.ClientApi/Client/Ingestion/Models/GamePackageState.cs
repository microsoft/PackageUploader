// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public enum GamePackageState
    {
        Unknown,
        PendingUpload,
        Uploaded,
        InProcessing,
        Processed,
        ProcessFailed,
    }
}