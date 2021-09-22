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
using GameStoreBroker.Application.Extensions;

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

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_config.BranchFriendlyName))
            {
                var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
                var submission = await _storeBrokerService.PublishPackagesToSandboxAsync(product, packageBranch, _config.DestinationSandboxName, _config.MinutesToWaitForPublishing, ct);
            }
            else if (!string.IsNullOrWhiteSpace(_config.FlightName))
            {
                // var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
            }
        }
    }
}
