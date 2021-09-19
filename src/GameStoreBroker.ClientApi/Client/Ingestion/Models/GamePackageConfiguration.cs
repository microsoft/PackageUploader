// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class GamePackageConfiguration : GamePackageResource
    {

        /// <summary>
        /// Display name of the branch this PackageSet belongs to
        /// </summary>
        public string BranchName { get; internal init; }

        /// <summary>
        /// ID of the branch this PackageSet belongs to
        /// </summary>
        public string BranchId { get; internal init; }

        /// <summary>
        /// Created date time
        /// </summary>
        public DateTime CreatedDate { get; internal init; }

        /// <summary>
        /// ModifiedDate date time
        /// </summary>
        public DateTime ModifiedDate { get; internal init; }

        /// <summary>
        /// Gradual Rollout information
        /// </summary>
        public GameGradualRolloutInfo GradualRolloutInfo { get; set; }

        /// <summary>
        /// List of market groups
        /// </summary>
        public List<GameMarketGroupPackage> MarketGroupPackages { get; internal init; }
    }
}
