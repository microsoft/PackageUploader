// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal sealed class IngestionPendingUpdateInfo
{

    /// <summary>
    /// Pending Update Type
    /// </summary>
    public string UpdateType { get; set; }

    /// <summary>
    /// [Running, Failed, Create]
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Href to get next data
    /// </summary>
    public string Href { get; set; }

    /// <summary>
    ///  If pending status is failed, the reason explaining failure
    /// </summary>
    public string FailureReason { get; set; }
}