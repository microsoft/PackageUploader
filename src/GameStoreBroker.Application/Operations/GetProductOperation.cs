// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class GetProductOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<GetProductOperation> _logger;
        private readonly BaseOperationSchema _config;

        public GetProductOperation(IGameStoreBrokerService storeBrokerService, ILogger<GetProductOperation> logger, IOptions<GetProductOperationSchema> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting GetProduct operation.");

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

            _logger.LogInformation("Product: {product}", product.ToJson());
        }
    }
}