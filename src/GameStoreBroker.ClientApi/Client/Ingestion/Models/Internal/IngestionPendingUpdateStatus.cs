// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
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
