// Copyright (c) Microsoft Corporation.
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

internal class RemovePackagesOperation(IPackageUploaderService storeBrokerService, ILogger<RemovePackagesOperation> logger, IOptions<RemovePackagesOperationConfig> config) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<RemovePackagesOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RemovePackagesOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

        await _storeBrokerService.RemovePackagesAsync(product, packageBranch, _config.MarketGroupName, _config.PackageFileName, ct).ConfigureAwait(false);
    }
}