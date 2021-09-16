// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class UploadXvcPackageOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<UploadXvcPackageOperation> _logger;
        private readonly UploadXvcPackageOperationConfig _config;

        public UploadXvcPackageOperation(IGameStoreBrokerService storeBrokerService, ILogger<UploadXvcPackageOperation> logger, IOptions<UploadXvcPackageOperationConfig> config) : base(logger)
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
            
            await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, _config.MarketGroupId, _config.GameAssets, true, _config.MinutesToWaitForProcessing, ct).ConfigureAwait(false);
        }
    }
}