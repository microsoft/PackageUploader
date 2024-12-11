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

internal class GetPackagesOperation : Operation
{
    private readonly IPackageUploaderService _storeBrokerService;
    private readonly ILogger<GetPackagesOperation> _logger;
    private readonly bool _isData;
    private readonly GetPackagesOperationConfig _config;

    public GetPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<GetPackagesOperation> logger, IOptions<GetPackagesOperationConfig> config, InvocationContext invocationContext) : base(logger)
    {
        _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isData = invocationContext.GetOptionValue(Program.DataOption);
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