// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal enum IngestionSubmissionState
{
    Unknown,
    NotSet,
    InProgress,
    Published,
    Archived,
    Deleted,
    Cancelled,
    RolledBack,
    ModularCert,
    Shadow,
}