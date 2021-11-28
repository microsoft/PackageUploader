// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.Application.Extensions;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class ImportPackagesOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ImportPackagesOperationConfig _config;

        public ImportPackagesOperation(IGameStoreBrokerService storeBrokerService, ILogger<ImportPackagesOperation> logger, IOptions<ImportPackagesOperationConfig> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var originPackageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
            var destinationPackageBranch = await _storeBrokerService.GetDestinationGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            await _storeBrokerService.ImportPackagesAsync(product, originPackageBranch, destinationPackageBranch, _config.MarketGroupName, _config.Overwrite, _config, ct).ConfigureAwait(false);
        }
    }
}
