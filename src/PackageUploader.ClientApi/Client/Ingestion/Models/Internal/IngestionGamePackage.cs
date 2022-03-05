// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal class IngestionGamePackage : IngestionResource
{
    /// <summary>
    /// Resource type [GamePackage]
    /// </summary>
    public string ResourceType { get; set; }

    /// <summary>
    /// state of the package [PendingUpload, Uploaded, InProcessing, Processed, ProcessFailed]
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Check body of package to return back to caller.
    /// </summary>
    public bool? IsEmpty { get; set; }

    /// <summary>
    /// Type of the package (e.g. Appx, AppxBundle, Msix, Xvc, etc.).
    /// </summary>
    public string PackageType { get; set; }

    /// <summary>
    /// file name of the package
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Xfus upload info
    /// </summary>
    public IngestionXfusUploadInfo UploadInfo { get; set; }

    /// <summary>
    /// file size of the package
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// If the package is certified
    /// </summary>
    public bool? IsCertified { get; set; }

    /// <summary>
    /// language codes of the package
    /// </summary>
    public IReadOnlyCollection<string> LanguageCodes { get; set; }

    /// <summary>
    /// target platforms of the package
    /// </summary>
    public IReadOnlyCollection<string> TargetPlatforms { get; set; }

    /// <summary>
    /// package platform of the package
    /// </summary>
    public string PackagePlatform { get; set; }

    /// <summary>
    /// xvc target platform of the package
    /// </summary>
    public string XvcTargetPlatform { get; set; }
}