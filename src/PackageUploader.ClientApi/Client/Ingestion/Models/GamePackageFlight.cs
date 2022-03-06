// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackageFlight : GamePackageResource, IGamePackageBranch
{
    /// <summary>
    /// Flight name
    /// </summary>
    public string FlightName { get; internal init; }

    /// <summary>
    /// Flight group ids
    /// </summary>
    public IList<string> GroupIds { get; set; }

    /// <summary>
    /// Branch name
    /// </summary>
    public string BranchFriendlyName { get; internal init; }

    /// <summary>
    /// Branch current draft instance ID.
    /// </summary>
    public string CurrentDraftInstanceId { get; internal init; }

    /// <summary>
    /// Indicates if this branch is a flight.
    /// </summary>
    public bool IsFlight { get; internal init; }

}