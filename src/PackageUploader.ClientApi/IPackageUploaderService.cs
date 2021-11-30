// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi
{
    public interface IPackageUploaderService
    {
        Task<GameProduct> GetProductByBigIdAsync(string bigId, CancellationToken ct);
        Task<GameProduct> GetProductByProductIdAsync(string productId, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(GameProduct product, string branchFriendlyName, CancellationToken ct);
        Task<GamePackageBranch> GetPackageBranchByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct);
        Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(GameProduct product, string flightName, CancellationToken ct);
        Task<GamePackageConfiguration> GetPackageConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, CancellationToken ct);
        Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(GameProduct product, GamePackageConfiguration packageConfiguration, CancellationToken ct);
        Task<GamePackage> UploadGamePackageAsync(GameProduct product, GamePackageBranch packageBranch, GameMarketGroupPackage marketGroupPackage, string packageFilePath, GameAssets gameAssets, int minutesToWaitForProcessing, CancellationToken ct);
        Task<GamePackageConfiguration> RemovePackagesAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupName, CancellationToken ct);
        Task<GamePackageConfiguration> SetXvcAvailabilityDateAsync(GameProduct product, GamePackageBranch packageBranch, GamePackage gamePackage, string marketGroupName, GamePackageDate availabilityDate, CancellationToken ct);
        Task<GamePackageConfiguration> SetUwpConfigurationAsync(GameProduct product, GamePackageBranch packageBranch, string marketGroupId, IGameConfiguration gameConfiguration, CancellationToken ct);
        Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, CancellationToken ct);
        Task<GamePackageConfiguration> ImportPackagesAsync(GameProduct product, GamePackageBranch originPackageBranch, GamePackageBranch destinationPackageBranch, string marketGroupName, bool overwrite, IGameConfiguration gameConfiguration, CancellationToken ct);
        Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, int minutesToWaitForPublishing, CancellationToken ct);
        Task<GameSubmission> PublishPackagesToSandboxAsync(GameProduct product, GamePackageBranch originPackageBranch, string destinationSandboxName, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct);
        Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, int minutesToWaitForPublishing, CancellationToken ct);
        Task<GameSubmission> PublishPackagesToFlightAsync(GameProduct product, GamePackageFlight gamePackageFlight, GamePublishConfiguration gameSubmissionConfiguration, int minutesToWaitForPublishing, CancellationToken ct);
    }
}
