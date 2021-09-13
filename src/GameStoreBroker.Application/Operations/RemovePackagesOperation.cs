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
    internal class RemovePackagesOperation : Operation
    {
        private readonly IGameStoreBrokerService _storeBrokerService;
        private readonly ILogger<RemovePackagesOperation> _logger;
        private readonly RemovePackagesOperationSchema _config;

        public RemovePackagesOperation(IGameStoreBrokerService storeBrokerService, ILogger<RemovePackagesOperation> logger, IOptions<RemovePackagesOperationSchema> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService;
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting UploadXvcPackage operation.");
            
            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
            var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

            await _storeBrokerService.RemovePackagesAsync(product, packageBranch, ct).ConfigureAwait(false);
        }
    }
}