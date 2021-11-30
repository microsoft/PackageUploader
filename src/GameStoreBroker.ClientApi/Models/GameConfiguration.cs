// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.ClientApi.Models
{
    public class GameConfiguration : IGameConfiguration
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
        public GameGradualRolloutInfo GradualRollout { get; set; }
    }
}
