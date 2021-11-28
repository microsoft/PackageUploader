// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class ValidateConfigOperation : Operation
    {
        private readonly ValidateConfigOperationConfig _config;
        private readonly IServiceProvider _serviceProvider;

        public ValidateConfigOperation(IOptions<ValidateConfigOperationConfig> config, ILogger<GenerateConfigTemplateOperation> logger, 
            IServiceProvider serviceProvider) : base(logger)
        {
            _config = config.Value;
            _serviceProvider = serviceProvider;
        }

        protected override Task ProcessAsync(CancellationToken ct)
        {
            var isValidConfig = _config.ValidateOperationName switch
            {
                OperationName.GetProduct => _serviceProvider.GetRequiredService<IOptions<GetProductOperationConfig>>()?.Value is not null,
                OperationName.UploadXvcPackage => _serviceProvider.GetRequiredService<IOptions<UploadXvcPackageOperationConfig>>()?.Value is not null,
                OperationName.UploadUwpPackage => _serviceProvider.GetRequiredService<IOptions<UploadUwpPackageOperationConfig>>()?.Value is not null,
                OperationName.ImportPackages => _serviceProvider.GetRequiredService<IOptions<ImportPackagesOperationConfig>>()?.Value is not null,
                OperationName.PublishPackages => _serviceProvider.GetRequiredService<IOptions<PublishPackagesOperationConfig>>()?.Value is not null,
                OperationName.RemovePackages => _serviceProvider.GetRequiredService<IOptions<RemovePackagesOperationConfig>>()?.Value is not null,
                _ => throw new NotImplementedException($"{_config.ValidateOperationName} config validation has not been implemented."),
            };

            if (isValidConfig)
            {
                _logger.LogInformation("The configuration file is valid.");
            }

            return Task.CompletedTask;
        }
    }
}
