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
    private readonly string _productIdOption;
    private readonly string _bigIdOption;
    private readonly string _branchFriendlyNameOption;
    private readonly string _flightNameOption;
    private readonly string _marketGroupNameOption;

    public GetPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<GetPackagesOperation> logger, IOptions<GetPackagesOperationConfig> config, InvocationContext invocationContext) : base(logger)
    {
        _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isData = invocationContext.GetOptionValue(Program.DataOption);
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _productIdOption = invocationContext.GetOptionValue(Program.ProductIdOption);
        _bigIdOption = invocationContext.GetOptionValue(Program.BigIdOption);
        _branchFriendlyNameOption = invocationContext.GetOptionValue(Program.BranchFriendlyNameOption);
        _flightNameOption = invocationContext.GetOptionValue(Program.FlightNameOption);
        _marketGroupNameOption = invocationContext.GetOptionValue(Program.MarketGroupNameOption);

        // arg validations
        if (_productIdOption != null && _bigIdOption != null) throw new ArgumentException("Cannot pass both BigId and ProductId as optional arguments");
        if (_config.ProductId != null && _bigIdOption != null) throw new ArgumentException("Cannot pass both: config value productId, optional argument BigId");
        if (_config.BigId != null && _productIdOption != null) throw new ArgumentException("Cannot pass both: config value bigId, optional argument ProductId");
        if (_branchFriendlyNameOption != null && _flightNameOption != null) throw new ArgumentException("Cannot pass both BranchFriendlyName and FlightName as optional arguments");
        if (_config.BranchFriendlyName != null && _flightNameOption != null) throw new ArgumentException("Cannot pass both: config value branchFriendlyName, optional argument FlightName");
        if (_config.FlightName != null && _branchFriendlyNameOption != null) throw new ArgumentException("Cannot pass both: config value flightName, optional argument BranchFriendlyName");
    }

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        if (_productIdOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (productId) with {new}", _config.ProductId, _productIdOption);
            _config.ProductId = _productIdOption;
        }
        if (_bigIdOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (bigId) with {new}", _config.BigId, _bigIdOption);
            _config.BigId = _bigIdOption;
        }
        if (_branchFriendlyNameOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (branchFriendlyName) with {new}", _config.BranchFriendlyName, _branchFriendlyNameOption);
            _config.BranchFriendlyName = _branchFriendlyNameOption;
        }
        if (_flightNameOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (flightName) with {new}", _config.FlightName, _flightNameOption);
            _config.FlightName = _flightNameOption;
        }
        if (_marketGroupNameOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (marketGroupName) with {new}", _config.MarketGroupName, _marketGroupNameOption);
            _config.MarketGroupName = _marketGroupNameOption;
        }

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