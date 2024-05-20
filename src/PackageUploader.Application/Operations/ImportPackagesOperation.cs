// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class ImportPackagesOperation : Operation
{
    private readonly IPackageUploaderService _storeBrokerService;
    private readonly ILogger<ImportPackagesOperation> _logger;
    private readonly ImportPackagesOperationConfig _config;

    public ImportPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<ImportPackagesOperation> logger, IOptions<ImportPackagesOperationConfig> config) : base(logger)
    {
        _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var originPackageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
        var destinationPackageBranch = await _storeBrokerService.GetDestinationGamePackageBranch(product, _config, ct).ConfigureAwait(false);

        await _storeBrokerService.ImportPackagesAsync(product, originPackageBranch, destinationPackageBranch, _config.MarketGroupName, _config.Overwrite, _config, _config.GetMarketGroupPackageMetadata(), ct).ConfigureAwait(false);
    }
}