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
    /// File name of the package
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File size of the package
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Package type reported by the service, when available.
    /// </summary>
    public string PackageType { get; internal init; }

    /// <summary>
    /// XVC target platform reported by the service, when available.
    /// </summary>
    public string XvcTargetPlatform { get; internal init; }
}