// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Models;

public interface IXvcGameConfiguration
{
    GamePackageDate AvailabilityDate { get; set; }
    GamePackageDate PreDownloadDate { get; set; }
}