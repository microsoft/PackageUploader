// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackageBranch : IGamePackageBranch
{
    /// <summary>
    /// Branch friendly name
    /// </summary>
    public string Name { get; internal init; }

    /// <summary>
    /// Branch current draft instance ID.
    /// </summary>
    public string CurrentDraftInstanceId { get; internal init; }

    /// <summary>
    /// Indicates if this branch is a flight.
    /// </summary>
    public GamePackageBranchType BranchType => GamePackageBranchType.Branch;
}