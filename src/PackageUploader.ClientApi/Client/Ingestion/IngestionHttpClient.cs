﻿// Copyright (c) Microsoft Corporation.
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

    public IngestionHttpClient(ILogger<IngestionHttpClient> logger, HttpClient httpClient, IngestionSdkVersion ingestionSdkVersion) : base(logger, httpClient, ingestionSdkVersion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
        
    public async Task<GameProduct> GetGameProductByLongIdAsync(string longId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(longId);

        try
        {
            var ingestionGameProduct = await GetAsync($"products/{longId}", IngestionJsonSerializerContext.Default.IngestionGameProduct, ct).ConfigureAwait(false);

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
        StringArgumentException.ThrowIfNullOrWhiteSpace(bigId);

        var ingestionGameProducts = GetAsyncEnumerable($"products?externalId={bigId}", IngestionJsonSerializerContext.Default.PagedCollectionIngestionGameProduct, ct);
        var ingestionGameProduct = await ingestionGameProducts.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new ProductNotFoundException($"Product with big id '{bigId}' not found.");

        var gameProduct = ingestionGameProduct.Map();
        return gameProduct;
    }

    public async Task<GamePackageBranch> GetPackageBranchByFriendlyNameAsync(string productId, string branchFriendlyName, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(branchFriendlyName);

        var branches = GetAsyncEnumerable($"products/{productId}/branches/getByModule(module=Package)", IngestionJsonSerializerContext.Default.PagedCollectionIngestionBranch, ct);

        var ingestionGamePackageBranch = await branches.FirstOrDefaultAsync(b => b.FriendlyName is not null && b.FriendlyName.Equals(branchFriendlyName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false)
            ?? throw new PackageBranchNotFoundException($"Package branch with friendly name '{branchFriendlyName}' not found.");

        var gamePackageBranch = ingestionGamePackageBranch.Map();
        return gamePackageBranch;
    }

    public async Task<GamePackageFlight> GetPackageFlightByFlightNameAsync(string productId, string flightName, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(flightName);

        var flights = GetAsyncEnumerable($"products/{productId}/flights", IngestionJsonSerializerContext.Default.PagedCollectionIngestionFlight, ct);

        var selectedFlight = await flights.FirstOrDefaultAsync(f => f.Name is not null && f.Name.Equals(flightName, StringComparison.OrdinalIgnoreCase), ct).ConfigureAwait(false) ?? throw new PackageBranchNotFoundException($"Package branch with flight name '{flightName}' not found.");
        var branch = await GetPackageBranchByFriendlyNameAsync(productId, selectedFlight.Id, ct).ConfigureAwait(false);
        return selectedFlight.Map(branch);
    }

    public async Task<GamePackage> CreatePackageRequestAsync(string productId, string currentDraftInstanceId, string fileName, string marketGroupId, bool isXvc, XvcTargetPlatform xvcTargetPlatform, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(currentDraftInstanceId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        StringArgumentException.ThrowIfNullOrWhiteSpace(marketGroupId);

        var body = new IngestionPackageCreationRequestBuilder(currentDraftInstanceId, fileName, marketGroupId, isXvc, xvcTargetPlatform).Build();

        var ingestionGamePackage = await PostAsync($"products/{productId}/packages", body, IngestionJsonSerializerContext.Default.IngestionPackageCreationRequest, IngestionJsonSerializerContext.Default.IngestionGamePackage, ct).ConfigureAwait(false);

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
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        IngestionGamePackage ingestionGamePackage;

        try
        {
            ingestionGamePackage = await GetAsync($"products/{productId}/packages/{packageId}", IngestionJsonSerializerContext.Default.IngestionGamePackage, ct).ConfigureAwait(false);
        }
        catch(HttpRequestException e) when (e.StatusCode is HttpStatusCode.MovedPermanently)
        {
            var redirectPackage = await GetAsyncWithErrors($"products/{productId}/packages/{packageId}", 
                                                            IngestionJsonSerializerContext.Default.IngestionRedirectPackage, 
                                                            new HashSet<HttpStatusCode>() {HttpStatusCode.MovedPermanently}, 
                                                            ct).ConfigureAwait(false);
            ingestionGamePackage = await GetAsync($"products/{productId}/packages/{redirectPackage.ToId}", IngestionJsonSerializerContext.Default.IngestionGamePackage, ct).ConfigureAwait(false);
        }

        var gamePackage = ingestionGamePackage.Map();
        return gamePackage;
    }

    public async Task<GamePackageAsset> CreatePackageAssetRequestAsync(string productId, string packageId, FileInfo fileInfo, GamePackageAssetType packageAssetType, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(fileInfo);

        var body = new IngestionGamePackageAssetBuilder(packageId, fileInfo, packageAssetType).Build();

        var ingestionGamePackageAsset = await PostAsync($"products/{productId}/packages/{packageId}/packageAssets", body, IngestionJsonSerializerContext.Default.IngestionGamePackageAsset, ct).ConfigureAwait(false);

        var gamePackageAsset = ingestionGamePackageAsset.Map();
        return gamePackageAsset;
    }

    public async Task<GamePackage> ProcessPackageRequestAsync(string productId, GamePackage gamePackage, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(gamePackage);

        gamePackage.State = GamePackageState.Uploaded;
        var body = gamePackage.Map();

        var ingestionGamePackage = await PutAsync($"products/{productId}/packages/{gamePackage.Id}", body, IngestionJsonSerializerContext.Default.IngestionGamePackage, ct);
        var newGamePackage = ingestionGamePackage.Map();
        return newGamePackage;
    }

    public async Task<GamePackageAsset> CommitPackageAssetAsync(string productId, string packageId, string packageAssetId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(packageAssetId);

        var body = new IngestionGamePackageAsset();

        var ingestionGamePackageAsset = await PutAsync($"products/{productId}/packages/{packageId}/packageAssets/{packageAssetId}/commit", body, IngestionJsonSerializerContext.Default.IngestionGamePackageAsset, ct).ConfigureAwait(false);

        var gamePackageAsset = ingestionGamePackageAsset.Map();
        return gamePackageAsset;
    }

    public async Task<GamePackageConfiguration> GetPackageConfigurationAsync(string productId, string currentDraftInstanceId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(currentDraftInstanceId);

        var packageSets = GetAsyncEnumerable($"products/{productId}/packageConfigurations/getByInstanceID(instanceID={currentDraftInstanceId})", IngestionJsonSerializerContext.Default.PagedCollectionIngestionGamePackageConfiguration, ct);

        var packageSet = await packageSets.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and currentDraftInstanceId '{currentDraftInstanceId}' not found.");

        var gamePackageConfiguration = packageSet.Map();
        return gamePackageConfiguration;
    }

    public async Task<GamePackageConfiguration> UpdatePackageConfigurationAsync(string productId, GamePackageConfiguration gamePackageConfiguration, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(gamePackageConfiguration);

        var packageSet = await GetAsync($"products/{productId}/packageConfigurations/{gamePackageConfiguration.Id}", IngestionJsonSerializerContext.Default.IngestionGamePackageConfiguration, ct)
            ?? throw new PackageConfigurationNotFoundException($"Package configuration for product '{productId}' and packageConfigurationId '{gamePackageConfiguration.Id}' not found.");

        var newPackageSet = packageSet.Merge(gamePackageConfiguration);

        // ODataEtag needs to be added to If-Match header on http client still.
        var customHeaders = new Dictionary<string, string>
        {
            { "If-Match", packageSet.ODataETag},
        };

        var result = await PutAsync($"products/{productId}/packageConfigurations/{newPackageSet.Id}", packageSet, IngestionJsonSerializerContext.Default.IngestionGamePackageConfiguration, customHeaders, ct).ConfigureAwait(false);
        return result.Map();
    }

    public async Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, CancellationToken ct)
    {
        return await CreateSandboxSubmissionRequestAsync(productId, currentDraftInstanceId, destinationSandboxName, null, ct).ConfigureAwait(false);
    }

    public async Task<GameSubmission> CreateSandboxSubmissionRequestAsync(string productId, string currentDraftInstanceId, string destinationSandboxName, GameSubmissionOptions gameSubmissionOptions, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(currentDraftInstanceId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(destinationSandboxName);

        var body = new IngestionSubmissionCreationRequestBuilder(currentDraftInstanceId, destinationSandboxName, IngestionSubmissionTargetType.Sandbox, gameSubmissionOptions).Build();

        var submission = await PostAsync($"products/{productId}/submissions", body, IngestionJsonSerializerContext.Default.IngestionSubmissionCreationRequest, IngestionJsonSerializerContext.Default.IngestionSubmission, ct).ConfigureAwait(false);

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
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(currentDraftInstanceId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(destinationFlightId);

        var body = new IngestionSubmissionCreationRequestBuilder(currentDraftInstanceId, destinationFlightId, IngestionSubmissionTargetType.Flight, gameSubmissionOptions).Build();

        var submission = await PostAsync($"products/{productId}/submissions", body, IngestionJsonSerializerContext.Default.IngestionSubmissionCreationRequest, IngestionJsonSerializerContext.Default.IngestionSubmission, ct).ConfigureAwait(false);

        var gameSubmission = submission.Map();

        if (gameSubmission.GameSubmissionState is GameSubmissionState.Failed)
        {
            gameSubmission.SubmissionValidationItems = await GetGameSubmissionValidationItemsFromFailureAsync(productId, submission.Id, ct);
        }

        return gameSubmission;
    }

    public async Task<GameSubmission> GetGameSubmissionAsync(string productId, string submissionId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);
        StringArgumentException.ThrowIfNullOrWhiteSpace(submissionId);

        var submission = await GetAsync($"products/{productId}/submissions/{submissionId}", IngestionJsonSerializerContext.Default.IngestionSubmission, ct).ConfigureAwait(false)
            ?? throw new SubmissionNotFoundException($"Submission for product '{productId}' and submissionId '{submissionId}' not found.");

        var gameSubmission = submission.Map();

        if (gameSubmission.GameSubmissionState is GameSubmissionState.Failed)
        {
            gameSubmission.SubmissionValidationItems = await GetGameSubmissionValidationItemsFromFailureAsync(productId, submission.Id, ct);
        }

        return gameSubmission;
    }

    public async Task<IReadOnlyCollection<IGamePackageBranch>> GetPackageBranchesAsync(string productId, CancellationToken ct)
    {
        StringArgumentException.ThrowIfNullOrWhiteSpace(productId);

        var flights = await GetAsyncEnumerable($"products/{productId}/flights", IngestionJsonSerializerContext.Default.PagedCollectionIngestionFlight, ct).ToListAsync(ct).ConfigureAwait(false);
        var branches = await GetAsyncEnumerable($"products/{productId}/branches/getByModule(module=Package)", IngestionJsonSerializerContext.Default.PagedCollectionIngestionBranch, ct).ToListAsync(ct).ConfigureAwait(false);

        var gamePackageBranches = new List<IGamePackageBranch>();
        foreach (var flight in flights)
        {
            var branch = branches.SingleOrDefault(b => b.FriendlyName.Equals(flight.Id))
                ?? throw new PackageBranchNotFoundException($"Package branch with flight name '{flight.Name}' not found.");

            gamePackageBranches.Add(flight.Map(branch));
            branches.Remove(branch);
        }

        gamePackageBranches.AddRange(branches.Select(b => b.Map()));
        return gamePackageBranches;
    }

    private async Task<List<GameSubmissionValidationItem>> GetGameSubmissionValidationItemsFromFailureAsync(string productId, string submissionId, CancellationToken ct)
    {
        var validations = GetAsyncEnumerable($"products/{productId}/submissions/{submissionId}/validations", IngestionJsonSerializerContext.Default.PagedCollectionIngestionSubmissionValidationItem, ct);

        var items = await validations.Select(x => x.Map()).ToListAsync(ct);
        return items;
    }
}