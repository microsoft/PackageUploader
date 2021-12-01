// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations
{
    internal sealed class PublishPackagesOperation : Operation
    {
        private readonly IPackageUploaderService _storeBrokerService;
        private readonly ILogger<PublishPackagesOperation> _logger;
        private readonly PublishPackagesOperationConfig _config;

        public PublishPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<PublishPackagesOperation> logger, IOptions<PublishPackagesOperationConfig> config) : base(logger)
        {
            _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

            var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

            GameSubmission submission;

            if (string.IsNullOrWhiteSpace(_config.FlightName) && !string.IsNullOrWhiteSpace(_config.BranchFriendlyName) && !string.IsNullOrWhiteSpace(_config.DestinationSandboxName))
            {
                var packageBranch = await _storeBrokerService.GetPackageBranchByFriendlyNameAsync(product, _config.BranchFriendlyName, ct).ConfigureAwait(false);
                submission = await _storeBrokerService.PublishPackagesToSandboxAsync(product, packageBranch, _config.DestinationSandboxName, _config.PublishConfiguration, _config.MinutesToWaitForPublishing, ct).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(_config.FlightName) && string.IsNullOrWhiteSpace(_config.BranchFriendlyName) && string.IsNullOrWhiteSpace(_config.DestinationSandboxName))
            {
                var packageFlight = await _storeBrokerService.GetPackageFlightByFlightNameAsync(product, _config.FlightName, ct).ConfigureAwait(false);
                submission = await _storeBrokerService.PublishPackagesToFlightAsync(product, packageFlight, _config.PublishConfiguration, _config.MinutesToWaitForPublishing, ct).ConfigureAwait(false);
            }
            else
            {
                throw new Exception($"{nameof(_config.FlightName)} or ({nameof(_config.BranchFriendlyName)} and {nameof(_config.DestinationSandboxName)}) is required.");
            }

            // Log validation errors if any
            if (submission.SubmissionValidationItems is not null && submission.SubmissionValidationItems.Any())
            {
                submission.SubmissionValidationItems.ForEach(validationItem =>
                {
                    if (validationItem.Severity is GameSubmissionValidationSeverity.Error)
                    {
                        _logger.Log(GetLogLevel(validationItem.Severity), validationItem.Message);
                    }
                });
            }
        }

        private static LogLevel GetLogLevel(GameSubmissionValidationSeverity severity) =>
            severity switch
            {
                GameSubmissionValidationSeverity.Informational => LogLevel.Information,
                GameSubmissionValidationSeverity.Warning => LogLevel.Warning,
                GameSubmissionValidationSeverity.Error => LogLevel.Error,
                _ => LogLevel.Warning
            };
    }
}
