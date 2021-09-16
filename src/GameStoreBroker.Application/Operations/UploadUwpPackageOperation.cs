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

            if (_config.AvailabilityDateConfig is not null)
            {
                await _storeBrokerService.SetUwpAvailabilityDateAsync(product, packageBranch, _config.MarketGroupId, _config.AvailabilityDateConfig.AvailabilityDate, ct).ConfigureAwait(false);
                _logger.LogInformation("Availability date set");
            }

            if (_config.MandatoryDateConfig is not null)
            {
                await _storeBrokerService.SetUwpAvailabilityDateAsync(product, packageBranch, _config.MarketGroupId, _config.MandatoryDateConfig.MandatoryDate, ct).ConfigureAwait(false);
                _logger.LogInformation("Mandatory date set");
            }
        }
    }
}