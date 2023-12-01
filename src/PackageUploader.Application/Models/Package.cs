// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.Application.Models;

public class Package
{
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

    public Package(GamePackage gamePackage)
    {
        IsCertified = gamePackage.IsCertified;
        FileName = gamePackage.FileName;
        FileSize = gamePackage.FileSize;
    }
}