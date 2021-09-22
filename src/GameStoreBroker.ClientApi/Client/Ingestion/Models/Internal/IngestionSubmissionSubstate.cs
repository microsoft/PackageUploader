// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal enum IngestionSubmissionSubstate
    {
        InDraft,
        Submitted,
        Cancelled,
        Failed,
        FailedInCertification,
        ReadyToPublish,
        Publishing,
        Published,
        InStore,
    }
}
