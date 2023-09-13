// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Models;
using PackageUploader.ClientApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class GetPackagesOperation : Operation
{
    private readonly IPackageUploaderService _storeBrokerService;
    private readonly ILogger<GetPackagesOperation> _logger;
    private readonly GetPackagesOperationConfig _config;

    public GetPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<GetPackagesOperation> logger, IOptions<GetPackagesOperationConfig> config) : base(logger)
    {
        _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
        var packages = await _storeBrokerService.GetGamePackagesAsync(product, packageBranch, _config.MarketGroupName, ct)
            .Select(gamePackage => new Package(gamePackage))
            .ToListAsync(ct).ConfigureAwait(false);

        var packagesJson = PackagesToJson(packages);
        _logger.LogInformation("Packages: {packages}", packagesJson);

        var fileName = $"packages_{product.ProductName}_{packageBranch.Name}_${_config.MarketGroupName}.json";
        await File.WriteAllTextAsync(fileName, packagesJson, ct).ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string PackagesToJson(IEnumerable<Package> packages)
    {
        try
        {
            var serializedObject = JsonSerializer.Serialize(packages, DefaultJsonSerializerOptions);
            return serializedObject;
        }
        catch (Exception ex)
        {
            return $"Could not serialize packages to json - {ex.Message}";
        }
    }
}