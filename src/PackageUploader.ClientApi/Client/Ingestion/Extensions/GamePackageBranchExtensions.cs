// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.ClientApi.Client.Ingestion.Extensions;

public static class GamePackageBranchExtensions
{
    public static string GetName(this IGamePackageBranch gamePackageBranch) =>
        gamePackageBranch.IsFlight
            ? ((GamePackageFlight)gamePackageBranch).FlightName
            : gamePackageBranch.BranchFriendlyName;

    public static string GetBranchType(this IGamePackageBranch gamePackageBranch) =>
        gamePackageBranch.IsFlight ? "Flight" : "Branch";

}