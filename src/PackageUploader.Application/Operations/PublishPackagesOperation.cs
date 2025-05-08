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
using System.CommandLine.Invocation;

namespace PackageUploader.Application.Operations;

internal sealed class PublishPackagesOperation : Operation
{
    private readonly IPackageUploaderService _storeBrokerService;
    private readonly ILogger<PublishPackagesOperation> _logger;
    private readonly PublishPackagesOperationConfig _config;
    private readonly string _productIdOption;
    private readonly string _bigIdOption;
    private readonly string _branchFriendlyNameOption;
    private readonly string _flightNameOption;
    private readonly string _destinationSandboxName;

    public PublishPackagesOperation(IPackageUploaderService storeBrokerService, ILogger<PublishPackagesOperation> logger, IOptions<PublishPackagesOperationConfig> config, InvocationContext invocationContext) : base(logger)
    {
        _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _productIdOption = invocationContext.GetOptionValue(Program.ProductIdOption);
        _bigIdOption = invocationContext.GetOptionValue(Program.BigIdOption);
        _branchFriendlyNameOption = invocationContext.GetOptionValue(Program.BranchFriendlyNameOption);
        _flightNameOption = invocationContext.GetOptionValue(Program.FlightNameOption);
        _destinationSandboxName = invocationContext.GetOptionValue(Program.DestinationSandboxName);

        // arg validations
        if (_productIdOption != null && _bigIdOption != null) throw new ArgumentException("Cannot pass both BigId and ProductId as optional arguments");
        if (_config.ProductId != null && _bigIdOption != null) throw new ArgumentException("Cannot pass both: config value productId, optional argument BigId");
        if (_config.BigId != null && _productIdOption != null) throw new ArgumentException("Cannot pass both: config value bigId, optional argument ProductId");
        if (_branchFriendlyNameOption != null && _flightNameOption != null) throw new ArgumentException("Cannot pass both BranchFriendlyName and FlightName as optional arguments");
        if (_destinationSandboxName != null && _flightNameOption != null) throw new ArgumentException("Cannot pass both DestinationSandboxName and FlightName as optional arguments");
        if ((_config.BranchFriendlyName != null && _config.DestinationSandboxName != null) && _flightNameOption != null) throw new ArgumentException("Cannot pass both: (config values branchFriendlyName and destinationSandboxName), optional argument FlightName");
        if (_config.FlightName != null && (_branchFriendlyNameOption != null && _destinationSandboxName != null)) throw new ArgumentException("Cannot pass both: config value flightName, (optional argument BranchFriendlyName and DestinationSandboxName)");
    }

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        if (_productIdOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (productId) with {new}", _config.ProductId, _productIdOption);
            _config.ProductId = _productIdOption;
        }
        if (_bigIdOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (bigId) with {new}", _config.BigId, _bigIdOption);
            _config.BigId = _bigIdOption;
        }
        if (_branchFriendlyNameOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (branchFriendlyName) with {new}", _config.BranchFriendlyName, _branchFriendlyNameOption);
            _config.BranchFriendlyName = _branchFriendlyNameOption;
        }
        if (_flightNameOption != null)
        {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (flightName) with {new}", _config.FlightName, _flightNameOption);
            _config.FlightName = _flightNameOption;
        }
        if (_destinationSandboxName != null) {
            _logger.LogInformation("Optional argument passed. Replacing config value {old} (destinationSandboxName) with {new}", _config.DestinationSandboxName, _destinationSandboxName);
            _config.DestinationSandboxName = _destinationSandboxName;
        }

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
        
        var validationFailed = false;
        if (submission.SubmissionValidationItems is not null && submission.SubmissionValidationItems.Any())
        {
            submission.SubmissionValidationItems.ForEach(validationItem =>
            {
                if (validationItem.Severity is GameSubmissionValidationSeverity.Error)
                {
                    validationFailed = true;
                    _logger.Log(GetLogLevel(validationItem.Severity), "{validationMessage}", validationItem.Message);
                }
            });
        }
        
        if (validationFailed)
        {
            throw new Exception("Submission Validation Failed");
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