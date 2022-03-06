// Copyright (C) Microsoft. All rights reserved.

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public interface IGamePackageBranch
{
    /// <summary>
    /// Branch name
    /// </summary>
    string BranchFriendlyName { get; }

    /// <summary>
    /// Branch current draft instance ID.
    /// </summary>
    string CurrentDraftInstanceId { get; }

    /// <summary>
    /// Indicates if this branch is a flight.
    /// </summary>
    bool IsFlight { get; }
}