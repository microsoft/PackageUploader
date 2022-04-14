// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public interface IGamePackageBranch
{
    /// <summary>
    /// Branch friendly name or flight name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Branch current draft instance ID.
    /// </summary>
    string CurrentDraftInstanceId { get; }

    /// <summary>
    /// Indicates if this branch is a flight.
    /// </summary>
    GamePackageBranchType BranchType { get; }
}