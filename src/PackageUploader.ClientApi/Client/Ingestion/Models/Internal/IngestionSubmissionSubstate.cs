// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal enum IngestionSubmissionSubstate
    {
        Unknown,
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
