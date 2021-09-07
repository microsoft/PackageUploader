// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.Application.Services;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class UploadGamePackageOperation : Operation
    {
        private readonly IProductService _productService;
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<UploadGamePackageOperation> _logger;
        private readonly UploadGamePackageOperationSchema _config;

        public UploadGamePackageOperation(IProductService productService, IGameStoreBrokerService storeBrokerService, ILogger<UploadGamePackageOperation> logger, IOptions<UploadGamePackageOperationSchema> config) : base(logger)
        {
            _productService = productService;
            _storeBrokerService = storeBrokerService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting UploadGamePackage operation.");
            
            var product = await _productService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var packageBranch = await _productService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
            
            await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, _config.GameAssets, _config.MinutesToWaitForProcessing, ct).ConfigureAwait(false);
        }
    }
}