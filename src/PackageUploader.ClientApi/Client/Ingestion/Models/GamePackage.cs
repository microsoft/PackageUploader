// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Xfus.Models;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackage : GamePackageResource
{
    /// <summary>
    /// State of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
    /// </summary>
    public GamePackageState State { get; internal set; }

    /// <summary>
    /// Xfus upload info
    /// </summary>
    public XfusUploadInfo UploadInfo { get; internal init; }

    /// <summary>
    /// If the package is certified
    /// </summary>
    public bool? IsCertified { get; set; }

    /// <summary>
    /// File name of the package
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File size of the package
    /// </summary>
    public long? FileSize { get; set; }
}