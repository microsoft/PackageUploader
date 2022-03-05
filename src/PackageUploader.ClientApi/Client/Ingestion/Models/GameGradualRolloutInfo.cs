// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GameGradualRolloutInfo
{
    /// <summary>
    /// Configure gradual rollout for your UWP packages
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Percentage to start rollout with
    /// </summary>
    public float? Percentage { get; set; }

    /// <summary>
    /// Always provide the newest packages when customers manually check for updates
    /// </summary>
    public bool? IsSeekEnabled { get; set; }
}