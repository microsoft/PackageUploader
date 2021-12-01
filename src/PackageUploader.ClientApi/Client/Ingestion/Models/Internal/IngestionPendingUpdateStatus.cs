// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal enum IngestionPendingUpdateStatus
    {
        Unknown,
        Running,
        Failed,
        Completed,
        Cancelled,
        PendingConfirmation,
    }
}
