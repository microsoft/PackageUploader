// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;

namespace GameStoreBroker.ClientApi.Models
{
    public class GameConfiguration
    {
        /// <summary>
        /// Availability date (Uwp/Xvc packages)
        /// </summary>
        public GamePackageDate AvailabilityDate { get; set; }

        /// <summary>
        /// MandatoryDate (Uwp packages)
        /// </summary>
        public GamePackageDate MandatoryDate { get; set; }

        /// <summary>
        /// Gradual rollout information (Uwp packages)
        /// </summary>
        public GameGradualRolloutInfo GradualRolloutInfo { get; set; }
    }
}
