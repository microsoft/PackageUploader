// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackageFlight : GamePackageResource, IGamePackageBranch
{
    /// <summary>
    /// Flight name
    /// </summary>
    public string Name { get; internal init; }

    /// <summary>
    /// Flight group ids
    /// </summary>
    public IList<string> GroupIds { get; set; }

    /// <summary>
    /// Branch current draft instance ID.
    /// </summary>
    public string CurrentDraftInstanceId { get; internal init; }

    /// <summary>
    /// Indicates if this branch is a flight.
    /// </summary>
    public GamePackageBranchType BranchType => GamePackageBranchType.Flight;

}