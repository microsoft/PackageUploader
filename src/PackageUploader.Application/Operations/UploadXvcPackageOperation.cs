﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class UploadXvcPackageOperation(IPackageUploaderService storeBrokerService, ILogger<UploadXvcPackageOperation> logger, IOptions<UploadXvcPackageOperationConfig> config) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<UploadXvcPackageOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly UploadXvcPackageOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
        var marketGroupPackage = await _storeBrokerService.GetGameMarketGroupPackage(product, packageBranch, _config, ct).ConfigureAwait(false);

        var gamePackage = await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, marketGroupPackage, _config.PackageFilePath, _config.GameAssets, _config.MinutesToWaitForProcessing, _config.DeltaUpload, isXvc: true, ct).ConfigureAwait(false);
        _logger.LogDebug("Configuration: PackageFilePath={PackageFilePath}, DeltaUpload={DeltaUpload}, AvailabilityDate={AvailabilityDate}", _config.PackageFilePath, _config.DeltaUpload, _config.AvailabilityDate);
        _logger.LogInformation("Uploaded package with id: {gamePackageId}", gamePackage.Id);

        if (_config.AvailabilityDate is not null || _config.PreDownloadDate is not null)
        {
            await _storeBrokerService.SetXvcConfigurationAsync(product, packageBranch, gamePackage, _config.MarketGroupName, _config, ct).ConfigureAwait(false);
            _logger.LogInformation("Configuration set for Xvc packages");
        }
    }
}