﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations
{
    internal class RemovePackagesOperation : Operation
    {
        private readonly IPackageUploaderService _storeBrokerService;
        private readonly ILogger<RemovePackagesOperation> _logger;
        private readonly RemovePackagesOperationConfig _config;

        public RemovePackagesOperation(IPackageUploaderService storeBrokerService, ILogger<RemovePackagesOperation> logger, IOptions<RemovePackagesOperationConfig> config) : base(logger)
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

            await _storeBrokerService.RemovePackagesAsync(product, packageBranch, _config.MarketGroupName, ct).ConfigureAwait(false);
        }
    }
}