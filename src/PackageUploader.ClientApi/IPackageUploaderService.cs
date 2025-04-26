// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace PackageUploader.ClientApi;

public interface IPackageUploaderService
{
    Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
    Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
    Task<IReadOnlyCollection<IGamePackageBranch>> GetPackageBranchesAsync(GameProduct product, CancellationToken ct);
    Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
    Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct);
    Task<GamePackageConfiguration> GetPackageConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, CancellationToken ct);
    IAsyncEnumerable<GamePackage> GetGamePackagesAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupName, CancellationToken ct);
    Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(GameProduct product, GamePackageConfiguration packageConfiguration, CancellationToken ct);
    Task<GamePackage> UploadGamePackageAsync(GameProduct product, IGamePackageBranch packageBranch, GameMarketGroupPackage marketGroupPackage, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, bool deltaUpload, bool isXvc, CancellationToken ct);
    Task<GamePackage> UploadGamePackageAsync(GameProduct product, IGamePackageBranch packageBranch, GameMarketGroupPackage marketGroupPackage, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, bool deltaUpload, bool isXvc, IProgress<PackageUploadingProgressStages> progress, CancellationToken ct);
    Task<GamePackageConfiguration> RemovePackagesAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupName, string packageFileName, CancellationToken ct);
    Task<GamePackageConfiguration> SetXvcConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupName, IXvcGameConfiguration gameConfiguration, CancellationToken ct);
    Task<GamePackageConfiguration> SetUwpConfigurationAsync(GameProduct product, IGamePackageBranch packageBranch, string marketGroupId, IUwpGameConfiguration gameConfiguration, CancellationToken ct);
    Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, IGamePackageBranch originPackageBranch, IGamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, CancellationToken ct);
    Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, IGamePackageBranch originPackageBranch, IGamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, IGameConfiguration gameConfiguration, CancellationToken ct);
    Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, int minutesToWaitForPublishing, CancellationToken ct);
    Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct);
    Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, int minutesToWaitForPublishing, CancellationToken ct);
    Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct);
}