// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    /// <summary>
    /// Gradual rollout information
    /// </summary>
    internal sealed class IngestionGradualRolloutInfo
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