// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Models;
using PackageUploader.ClientApi;
using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class GetProductOperation(IPackageUploaderService storeBrokerService, ILogger<GetProductOperation> logger, IOptions<GetProductOperationConfig> config, InvocationContext invocationContext) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<GetProductOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly bool _isData = invocationContext.GetOptionValue(ParameterHelper.DataOption);
    private readonly GetProductOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        var gameProduct = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var gamePackageBranches = await _storeBrokerService.GetPackageBranchesAsync(gameProduct, ct).ConfigureAwait(false);

        var product = new Product(gameProduct, gamePackageBranches);

        var productJson = product.ToJson();
        _logger.LogInformation("Product: {product}", productJson);

        if (_isData)
            Console.WriteLine(productJson);
    }
}