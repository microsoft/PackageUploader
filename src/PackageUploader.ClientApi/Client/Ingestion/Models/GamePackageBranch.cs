// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GamePackageBranch : IGamePackageBranch
{
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