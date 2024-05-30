// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.ClientApi.Models;

public interface IUwpGameConfiguration
{
    GamePackageDate AvailabilityDate { get; set; }
    GamePackageDate MandatoryDate { get; set; }
    GameGradualRolloutInfo GradualRollout { get; set; }
}