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
    internal class GetProductOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly BaseOperationConfig _config;

        public GetProductOperation(IGameStoreBrokerService storeBrokerService, ILogger<GetProductOperation> logger, IOptions<GetProductOperationConfig> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

            _logger.LogInformation("Product: {product}", product.ToJson());
        }
    }
}