// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using PackageUploader.ClientApi.Client.Xfus.Models;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackageAsset : GamePackageResource
{
    /// <summary>
    /// The type of the package asset (EraSymbolFile, EraSubmissionValidatorLog, EraEkb).
    /// </summary>
    public string Type { get; internal init; }

    /// <summary>
    /// The file name of the package asset.
    /// </summary>
    public string Name { get; internal init; }

    /// <summary>
    /// Returns whether or not this asset has been committed yet.
    /// </summary>
    public bool? IsCommitted { get; internal init; }

    /// <summary>
    /// The containing package Id for this asset.
    /// </summary>
    public string PackageId { get; internal init; }

    /// <summary>
    /// The type of package this asset represents e.g. Xml, Cab etc.
    /// </summary>
    public string PackageType { get; internal init; }

    /// <summary>
    /// The DateTime this asset was created.
    /// </summary>
    public DateTime CreatedDate { get; internal init; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    /// <value>
    /// The binary size in bytes.
    /// </value>
    public long? BinarySizeInBytes { get; internal init; }

    /// <summary>
    /// Xfus upload info
    /// </summary>
    public XfusUploadInfo UploadInfo { get; internal init; }

    /// <summary>
    /// file name of the package
    /// </summary>
    public string FileName { get; internal init; }
}