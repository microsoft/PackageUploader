// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.Application.Extensions;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_config.BranchFriendlyName))
            {
                if (_config.DestinationSandboxName.Equals("retail", StringComparison.OrdinalIgnoreCase) && !_config.Retail)
                {
                    throw new Exception("You need use the parameter --Retail to allow publish packages to RETAIL sandbox");
                }

                var packageBranch = await _storeBrokerService.GetPackageBranchByFriendlyNameAsync(product, _config.BranchFriendlyName, ct).ConfigureAwait(false);
                var submission = await _storeBrokerService.PublishPackagesToSandboxAsync(product, packageBranch, _config.DestinationSandboxName, _config.MinutesToWaitForPublishing, ct).ConfigureAwait(false);

                if (submission.GameSubmissionState == GameSubmissionState.Failed)
                {
                    _logger.LogError("Failed to publish.");
                }
                else if (submission.GameSubmissionState == GameSubmissionState.Published) 
                {
                    _logger.LogInformation("Game published.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(_config.FlightName))
            {
                // var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
            }
        }
    }
}
