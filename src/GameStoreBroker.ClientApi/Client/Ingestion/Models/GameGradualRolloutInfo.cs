// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class GameGradualRolloutInfo
    {
        /// <summary>
        /// Is enabled
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Percentage
        /// </summary>
        public float? Percentage { get; set; }

        /// <summary>
        /// Is Seek enabled
        /// </summary>
        public bool? IsSeekEnabled { get; set; }
    }
}