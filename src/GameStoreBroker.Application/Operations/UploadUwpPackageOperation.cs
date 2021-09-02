// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.Application.Services;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class UploadUwpPackageOperation : Operation
    {
        private readonly IProductService _productService;
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<UploadUwpPackageOperation> _logger;
        private readonly UploadUwpPackageOperationSchema _config;

        public UploadUwpPackageOperation(IProductService productService, IGameStoreBrokerService storeBrokerService, ILogger<UploadUwpPackageOperation> logger, IOptions<UploadUwpPackageOperationSchema> config) : base(logger)
        {
            _productService = productService;
            _storeBrokerService = storeBrokerService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting UploadUwpPackage operation.");
            
            var product = await _productService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var packageBranch = await _productService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            var packageFilePath = new FileInfo(_config.PackageFilePath);
            await _storeBrokerService.UploadUwpPackageAsync(product, packageBranch, packageFilePath, ct);
        }
    }
}