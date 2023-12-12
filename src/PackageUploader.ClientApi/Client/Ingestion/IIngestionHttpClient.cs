// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion;

public interface IIngestionHttpClient
{
    Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct);
    Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct);
    Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct);
    Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(string productId, string flightName, CancellationToken ct);
    Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, string marketGroupId, bool isXvc, XvcTargetPlatform xvcTargetPlatform, CancellationToken ct);
    Task<GamePackage> GetPackageByIdAsync(string productId, string packageId, CancellationToken ct);
    Task<GamePackageAsset> CreatePackageAssetRequestAsync(string productId, string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType, CancellationToken ct);
    Task<GamePackage> ProcessPackageRequestAsync(string productId, GamePackage gamePackage, CancellationToken ct);
    Task<GamePackageAsset> CommitPackageAssetAsync(string productId, string packageId, string packageAssetId, CancellationToken ct);
    Task<GamePackageConfiguration> GetPackageConfigurationAsync(string productId, string currentDraftInstanceId, CancellationToken ct);
    Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(string productId, GamePackageConfiguration gamePackageConfiguration, CancellationToken ct);
    Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, CancellationToken ct);
    Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, GameSubmissionOptions gameSubmissionOptions, CancellationToken ct);
    Task<GameSubmission> CreateFlightSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationFlightId, CancellationToken ct);
    Task<GameSubmission> CreateFlightSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationFlightId, GameSubmissionOptions gameSubmissionOptions, CancellationToken ct);
    Task<GameSubmission> GetGameSubmissionAsync(string productId, string submissionId, CancellationToken ct);
    Task<IReadOnlyCollection<IGamePackageBranch>> GetPackageBranchesAsync(string productId, CancellationToken ct);
}