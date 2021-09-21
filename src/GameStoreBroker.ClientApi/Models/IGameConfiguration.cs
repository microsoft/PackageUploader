// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;

namespace GameStoreBroker.ClientApi.Models
{
    public interface IGameConfiguration
    {
        GamePackageDate AvailabilityDate { get; set; }
        GamePackageDate MandatoryDate { get; set; }
        GameGradualRolloutInfo GradualRollout { get; set; }
    }
}
