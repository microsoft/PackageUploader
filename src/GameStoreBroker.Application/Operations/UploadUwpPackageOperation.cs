// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Extensions;
using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Operations
{
    internal class UploadUwpPackageOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<UploadUwpPackageOperation> _logger;
        private readonly UploadUwpPackageOperationSchema _config;

        public UploadUwpPackageOperation(IGameStoreBrokerService storeBrokerService, ILogger<UploadUwpPackageOperation> logger, IOptions<UploadUwpPackageOperationSchema> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting UploadUwpPackage operation.");
            
            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            var gameAssets = new GameAssets
            {
                PackageFilePath = _config.PackageFilePath
            };
            
            await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, gameAssets, false, _config.MinutesToWaitForProcessing, ct).ConfigureAwait(false);
        }
    }
}