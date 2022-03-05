// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public enum GamePackageState
{
    Unknown,
    PendingUpload,
    Uploaded,
    InProcessing,
    Processed,
    ProcessFailed,
}