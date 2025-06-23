// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Models;

public struct PackageUploadingProgress
{
    public int Percentage { get; set; }

    public PackageUploadingProgressStage Stage { get; set; }

    public readonly bool Equals(PackageUploadingProgress other)
    {
        return Percentage == other.Percentage && Stage == other.Stage;
    }
}

public enum PackageUploadingProgressStage
{
    NotStarted,
    ComputingDeltas,
    UploadingPackage,
    ProcessingPackage,
    UploadingSupplementalFiles,
    Done
}