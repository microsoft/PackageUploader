using GameStoreBroker.Application.Config;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal sealed class PublishPackagesOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<PublishPackagesOperation> _logger;
        private readonly PublishPackagesOperationConfig _config;

        public PublishPackagesOperation(IGameStoreBrokerService storeBrokerService, ILogger<PublishPackagesOperation> logger, IOptions<PublishPackagesOperationConfig> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override Task ProcessAsync(CancellationToken ct)
        {
            //_logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            //var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
            //var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            //var gamePackage = await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, _config.MarketGroupId, _config.PackageFilePath, _config.GameAssets, _config.MinutesToWaitForProcessing, ct).ConfigureAwait(false);
            //_logger.LogInformation("Uploaded package with id: {gamePackageId}", gamePackage.Id);

            //if (_config.AvailabilityDate is not null)
            //{
            //    await _storeBrokerService.SetXvcAvailabilityDateAsync(product, packageBranch, gamePackage, _config.MarketGroupId, _config.AvailabilityDate, ct).ConfigureAwait(false);
            //    _logger.LogInformation("Availability date set");
            //}

            return null;
        }
    }
}
