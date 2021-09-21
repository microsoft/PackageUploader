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
    internal class UploadUwpPackageOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<UploadUwpPackageOperation> _logger;
        private readonly UploadUwpPackageOperationConfig _config;

        public UploadUwpPackageOperation(IGameStoreBrokerService storeBrokerService, ILogger<UploadUwpPackageOperation> logger, IOptions<UploadUwpPackageOperationConfig> config) : base(logger)
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

            var gamePackage = await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, _config.MarketGroupId, _config.PackageFilePath, null, _config.MinutesToWaitForProcessing, ct).ConfigureAwait(false);
            _logger.LogInformation("Uploaded package with id: {gamePackageId}", gamePackage.Id);

            if (_config.AvailabilityDate is not null || _config.MandatoryDate is not null || _config.MandatoryDate is not null)
            {
                await _storeBrokerService.SetUwpConfigurationAsync(product, packageBranch, _config.MarketGroupId, _config, ct).ConfigureAwait(false);
                _logger.LogInformation("Set Uwp Package dates set");
            }
        }
    }
}
