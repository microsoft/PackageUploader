// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Schema;
using GameStoreBroker.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class GetProductOperation : Operation
    {
        private readonly IProductService _productService;
        private readonly ILogger<GetProductOperation> _logger;
        private readonly BaseOperationSchema _config;

        public GetProductOperation(IProductService productService, ILogger<GetProductOperation> logger, IOptions<GetProductOperationSchema> config) : base(logger)
        {
            _productService = productService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting GetProduct operation.");

            var product = await _productService.GetProductAsync(_config, ct).ConfigureAwait(false);

            _logger.LogInformation("Product: {product}", product.ToJson());
        }
    }
}