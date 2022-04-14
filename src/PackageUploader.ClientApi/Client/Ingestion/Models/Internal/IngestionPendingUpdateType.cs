// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal enum IngestionPendingUpdateType
{
    Unknown,
    Create,
    Submit,
    Promote,
    Validate,
}