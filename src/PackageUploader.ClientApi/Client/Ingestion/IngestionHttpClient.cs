// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Client.Ingestion.Builders;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Mappers;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("PackageUploader.ClientApi.Test")]
namespace PackageUploader.ClientApi.Client.Ingestion;

internal sealed class IngestionHttpClient : HttpRestClient, IIngestionHttpClient
{
    private readonly ILogger<IngestionHttpClient> _logger;

    public IngestionHttpClient(ILogger<IngestionHttpClient> logger, HttpClient httpClient) : base(logger, httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
        
    public async Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(longId))
        {
            throw new ArgumentException($"{nameof(longId)} cannot be null or empty.", nameof(longId));
        }

        try
        {
            var ingestionGameProduct = await GetAsync<IngestionGameProduct>($"products/{longId}", ct).ConfigureAwait(false);

            var gameProduct = ingestionGameProduct.Map();
            return gameProduct;
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            throw new ProductNotFoundException($"Product with product id '{longId}' not found.", e);
        }
    }

    public async Task<GameProduct> GetGameProductByBigIdAsync(string bigId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bigId))
        {
            throw new ArgumentException($"{nameof(bigId)} cannot be null or empty.", nameof(bigId));
        }

        var ingestionGameProducts = GetAsyncEnumerable<IngestionGameProduct>($"products?externalId={bigId}", ct);
        var ingestionGameProduct = await ingestionGameProducts.FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (ingestionGameProduct is null)
        {
            throw new ProductNotFoundException($"Product with big id '{bigId}' not found.");
        }

        var gameProduct = ingestionGameProduct.Map();
        return gameProduct;
    }

    public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(branchFriendlyName))
        {
            throw new ArgumentException($"{nameof(branchFriendlyName)} cannot be null or empty.", nameof(branchFriendlyName));
        }

        var branches = GetAsyncEnumerable<IngestionBranch>($"products/{productId}/branches/getByModule(module=Package)", ct);

        var ingestionGamePackageBranch = await branches.FirstOrDefaultAsync(b => b.FriendlyName is not null && b.FriendlyName.Equals(branchFriendlyName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false);

        if (ingestionGamePackageBranch is null)
        {
            throw new PackageBranchNotFoundException($"Package branch with friendly name '{branchFriendlyName}' not found.");
        }

        var gamePackageBranch = ingestionGamePackageBranch.Map();
        return gamePackageBranch;
    }

    public async Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(string productId, string flightName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(flightName))
        {
            throw new ArgumentException($"{nameof(flightName)} cannot be null or empty.", nameof(flightName));
        }

        var flights = GetAsyncEnumerable<IngestionFlight>($"products/{productId}/flights", ct);

        var selectedFlight = await flights.FirstOrDefaultAsync(f => f.Name is not null && f.Name.Equals(flightName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false);

        if (selectedFlight is null)
        {
            throw new PackageBranchNotFoundException($"Package branch with flight name '{flightName}' not found.");
        }

        var branch = await GetPackageBranchByFriendlyNameAsync(productId, selectedFlight.Id, ct).ConfigureAwait(false);
        return selectedFlight.Map(branch);
    }

    public async Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, string marketGroupId, bool deltaUpload, XvcTargetPlatform xvcTargetPlatform, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
        {
            throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException($"{nameof(fileName)} cannot be null or empty.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(marketGroupId))
        {
            throw new ArgumentException($"{nameof(marketGroupId)} cannot be null or empty.", nameof(marketGroupId));
        }

        var body = new IngestionPackageCreationRequestBuilder(currentDraftInstanceId, fileName, marketGroupId, deltaUpload, xvcTargetPlatform).Build();

        var ingestionGamePackage = await PostAsync<IngestionPackageCreationRequest, IngestionGamePackage>($"products/{productId}/packages", body, ct).ConfigureAwait(false);

        if (!ingestionGamePackage.State.Equals("PendingUpload", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Package request in Partner Center is not 'PendingUpload'.");
        }

        _logger.LogInformation("Package id: {packageId}", ingestionGamePackage.Id);

        var gamePackage = ingestionGamePackage.Map();
        return gamePackage;
    }

    public async Task<GamePackage> GetPackageByIdAsync(string productId, string packageId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
        }

        var ingestionGamePackage = await GetAsync<IngestionGamePackage>($"products/{productId}/packages/{packageId}", ct).ConfigureAwait(false);

        var gamePackage = ingestionGamePackage.Map();
        return gamePackage;
    }

    public async Task<GamePackageAsset> CreatePackageAssetRequestAsync(string productId, string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
        }

        ArgumentNullException.ThrowIfNull(fileInfo);

        var body = new IngestionGamePackageAssetBuilder(packageId, fileInfo, packageAssetType).Build();

        var ingestionGamePackageAsset = await PostAsync($"products/{productId}/packages/{packageId}/packageAssets", body, ct).ConfigureAwait(false);

        var gamePackageAsset = ingestionGamePackageAsset.Map();
        return gamePackageAsset;
    }

    public async Task<GamePackage> ProcessPackageRequestAsync(string productId, GamePackage gamePackage, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        ArgumentNullException.ThrowIfNull(gamePackage);

        gamePackage.State = GamePackageState.Uploaded;
        var body = gamePackage.Map();

        var ingestionGamePackage = await PutAsync($"products/{productId}/packages/{gamePackage.Id}", body, ct);
        var newGamePackage = ingestionGamePackage.Map();
        return newGamePackage;
    }

    public async Task<GamePackageAsset> CommitPackageAssetAsync(string productId, string packageId, string packageAssetId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException($"{nameof(packageId)} cannot be null or empty.", nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(packageAssetId))
        {
            throw new ArgumentException($"{nameof(packageAssetId)} cannot be null or empty.", nameof(packageAssetId));
        }

        var body = new IngestionGamePackageAsset();

        var ingestionGamePackageAsset = await PutAsync($"products/{productId}/packages/{packageId}/packageAssets/{packageAssetId}/commit", body, ct).ConfigureAwait(false);

        var gamePackageAsset = ingestionGamePackageAsset.Map();
        return gamePackageAsset;
    }

    public async Task<GamePackageConfiguration> GetPackageConfigurationAsync(string productId, string currentDraftInstanceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
        {
            throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
        }

        var packageSets = GetAsyncEnumerable<IngestionGamePackageConfiguration>($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", ct);

        var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (packageSet is null)
        {
            throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");
        }

        var gamePackageConfiguration = packageSet.Map();
        return gamePackageConfiguration;
    }

    public async Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(string productId, GamePackageConfiguration gamePackageConfiguration, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        ArgumentNullException.ThrowIfNull(gamePackageConfiguration);

        var packageSet = await GetAsync<IngestionGamePackageConfiguration>($"products/{productId}/packageConfigurations/{gamePackageConfiguration.Id}", ct);
            
        if (packageSet is null)
        {
            throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and packageConfigurationId '{gamePackageConfiguration.Id}' not found.");
        }

        var newPackageSet = packageSet.Merge(gamePackageConfiguration);

        // ODataEtag needs to be added to If-Match header on http client still.
        var customHeaders = new Dictionary<string, string>
        {
            { "If-Match", packageSet.ODataETag},
        };

        var result = await PutAsync($"products/{productId}/packageConfigurations/{newPackageSet.Id}", packageSet, customHeaders, ct).ConfigureAwait(false);
        return result.Map();
    }

    public async Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, CancellationToken ct)
    {
        return await CreateSandboxSubmissionRequestAsync(productId, currentDraftInstanceId, destinationSandboxName, null, ct).ConfigureAwait(false);
    }

    public async Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, GameSubmissionOptions gameSubmissionOptions, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
        {
            throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
        }

        if (string.IsNullOrWhiteSpace(destinationSandboxName))
        {
            throw new ArgumentException($"{nameof(destinationSandboxName)} cannot be null or empty.", nameof(destinationSandboxName));
        }

        var body = new IngestionSubmissionCreationRequestBuilder(currentDraftInstanceId, destinationSandboxName, IngestionSubmissionTargetType.Sandbox, gameSubmissionOptions).Build();

        var submission = await PostAsync<IngestionSubmissionCreationRequest, IngestionSubmission>($"products/{productId}/submissions", body, ct).ConfigureAwait(false);

        var gameSubmission = submission.Map();

        if (gameSubmission.GameSubmissionState is GameSubmissionState.Failed) 
        {
            gameSubmission.SubmissionValidationItems = await GetGameSubmissionValidationItemsFromFailureAsync(productId, submission.Id, ct);
        }

        return gameSubmission;
    }

    public async Task<GameSubmission> CreateFlightSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationFlightId, CancellationToken ct)
    {
        return await CreateFlightSubmissionRequestAsync(productId, currentDraftInstanceId, destinationFlightId, null, ct).ConfigureAwait(false);
    }

    public async Task<GameSubmission> CreateFlightSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationFlightId, GameSubmissionOptions gameSubmissionOptions, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(currentDraftInstanceId))
        {
            throw new ArgumentException($"{nameof(currentDraftInstanceId)} cannot be null or empty.", nameof(currentDraftInstanceId));
        }

        if (string.IsNullOrWhiteSpace(destinationFlightId))
        {
            throw new ArgumentException($"{nameof(destinationFlightId)} cannot be null or empty.", nameof(destinationFlightId));
        }

        var body = new IngestionSubmissionCreationRequestBuilder(currentDraftInstanceId, destinationFlightId, IngestionSubmissionTargetType.Flight, gameSubmissionOptions).Build();

        var submission = await PostAsync<IngestionSubmissionCreationRequest, IngestionSubmission>($"products/{productId}/submissions", body, ct).ConfigureAwait(false);

        var gameSubmission = submission.Map();

        if (gameSubmission.GameSubmissionState is GameSubmissionState.Failed)
        {
            gameSubmission.SubmissionValidationItems = await GetGameSubmissionValidationItemsFromFailureAsync(productId, submission.Id, ct);
        }

        return gameSubmission;
    }

    public async Task<GameSubmission> GetGameSubmissionAsync(string productId, string submissionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(submissionId))
        {
            throw new ArgumentException($"{nameof(submissionId)} cannot be null or empty.", nameof(submissionId));
        }

        var submission = await GetAsync<IngestionSubmission>($"products/{productId}/submissions/{submissionId}", ct).ConfigureAwait(false);
        if (submission is null)
        {
            throw new SubmissionNotFoundException($"Submission for product '{productId}' and submissionId '{submissionId}' not found.");
        }

        var gameSubmission = submission.Map();

        if (gameSubmission.GameSubmissionState is GameSubmissionState.Failed)
        {
            gameSubmission.SubmissionValidationItems = await GetGameSubmissionValidationItemsFromFailureAsync(productId, submission.Id, ct);
        }

        return gameSubmission;
    }

    public async Task<IReadOnlyCollection<IGamePackageBranch>> GetPackageBranchesAsync(string productId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException($"{nameof(productId)} cannot be null or empty.", nameof(productId));
        }

        var flights = await GetAsyncEnumerable<IngestionFlight>($"products/{productId}/flights", ct).ToListAsync(ct).ConfigureAwait(false);
        var branches = await GetAsyncEnumerable<IngestionBranch>($"products/{productId}/branches/getByModule(module=Package)", ct).ToListAsync(ct).ConfigureAwait(false);

        var gamePackageBranches = new List<IGamePackageBranch>();
        foreach (var flight in flights)
        {
            var branch = branches.SingleOrDefault(b => b.FriendlyName.Equals(flight.Id));

            if (branch is null)
            {
                throw new PackageBranchNotFoundException($"Package branch with flight name '{flight.Name}' not found.");
            }

            gamePackageBranches.Add(flight.Map(branch));
            branches.Remove(branch);
        }

        gamePackageBranches.AddRange(branches.Select(b => b.Map()));
        return gamePackageBranches;
    }

    private async Task<List<GameSubmissionValidationItem>> GetGameSubmissionValidationItemsFromFailureAsync(string productId, string submissionId, CancellationToken ct)
    {
        var validations = GetAsyncEnumerable<IngestionSubmissionValidationItem>($"products/{productId}/submissions/{submissionId}/validations", ct);

        var items = await validations.Select(x => x.Map()).ToListAsync(ct);
        return items;
    }
}