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
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class GetPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<GetPackagesOperation> logger, IOptions<GetPackagesOperationConfig> config, InvocationContext invocationContext) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<GetPackagesOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly bool _isData = invocationContext.GetOptionValue(ParameterHelper.DataOption);
    private readonly GetPackagesOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

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

        if (_isData)
            Console.WriteLine(packagesJson);
    }

    public static string PackagesToJson(IEnumerable<Package> packages)
    {
        try
        {
            var serializedObject = JsonSerializer.Serialize(packages, PackageUploaderJsonSerializerContext.Default.IEnumerablePackage);
            return serializedObject;
        }
        catch (Exception ex)
        {
            return $"Could not serialize packages to json - {ex.Message}";
        }
    }
}