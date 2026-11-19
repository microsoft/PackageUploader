// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using System;

namespace PackageUploader.Application.Models;

public class Package
{
    /// <summary>
    /// File name of the package
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File size of the package
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Canonical package resource identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Textual processing state of the package.
    /// </summary>
    public string State { get; set; }

    public string PackageType { get; set; }

    public string XvcTargetPlatform { get; set; }

    /// <summary>
    /// Authoritative extracted package PDUID, when available.
    /// </summary>
    public string BuildId { get; set; }

    public string ContentId { get; set; }

    /// <summary>
    /// Extracted package version text, when available.
    /// </summary>
    public string Version { get; set; }

    public Package(GamePackage gamePackage)
    {
        FileName = gamePackage.FileName;
        FileSize = gamePackage.FileSize;
        Id = gamePackage.Id;
        State = Enum.GetName(gamePackage.State) ?? nameof(GamePackageState.Unknown);
        PackageType = gamePackage.PackageType;
        XvcTargetPlatform = gamePackage.XvcTargetPlatform;
        BuildId = gamePackage.BuildId;
        ContentId = gamePackage.ContentId;
        Version = gamePackage.Version;
    }
}