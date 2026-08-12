// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Packaging;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class UploadXvcPackageOperation(
    IPackageUploaderService storeBrokerService,
    ILogger<UploadXvcPackageOperation> logger,
    IOptions<UploadXvcPackageOperationConfig> config,
    IMsixvc2UploadToolProvider msixvc2ToolProvider,
    IMsixvc2ProcessRunner msixvc2ProcessRunner,
    IMsixvc2DelegationGuard msixvc2DelegationGuard,
    Msixvc2CommandLineContext msixvc2CommandLineContext) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<UploadXvcPackageOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly UploadXvcPackageOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly IMsixvc2UploadToolProvider _msixvc2ToolProvider = msixvc2ToolProvider ?? throw new ArgumentNullException(nameof(msixvc2ToolProvider));
    private readonly IMsixvc2ProcessRunner _msixvc2ProcessRunner = msixvc2ProcessRunner ?? throw new ArgumentNullException(nameof(msixvc2ProcessRunner));
    private readonly IMsixvc2DelegationGuard _msixvc2DelegationGuard = msixvc2DelegationGuard ?? throw new ArgumentNullException(nameof(msixvc2DelegationGuard));
    private readonly Msixvc2CommandLineContext _msixvc2CommandLineContext = msixvc2CommandLineContext ?? throw new ArgumentNullException(nameof(msixvc2CommandLineContext));

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        // SAFETY: only MSIXVC2 packages may be delegated to MakePkg.exe. MakePkg.exe shells back out to
        // PackageUploader.exe for XVC1/MSIXVC1 uploads, so delegating any other package format here would
        // create an infinite process recursion between the two executables. This guard is deliberately the
        // package-format detection itself (not a config flag) so it cannot be bypassed by configuration.
        if (PackageFormatDetector.IsLikelyMsixvc2Package(_config.PackageFilePath))
        {
            // SAFETY (defense in depth): format detection is a heuristic and can false-positive on an XVC1
            // package whose encrypted tail happens to contain the ZIP end-of-central-directory signature.
            // If this process was itself started by a MakePkg.exe we delegated to, delegating again would be
            // exactly the unbounded cycle above, so fall through to the normal upload path instead.
            if (_msixvc2DelegationGuard.IsDelegatedInvocation)
            {
                _logger.LogWarning(
                    "Package '{PackageFilePath}' looks like MSIXVC2, but this PackageUploader process was started by MakePkg.exe ({EnvironmentVariable} is set). " +
                    "Uploading directly instead of delegating back to MakePkg.exe, to avoid an infinite MakePkg.exe/PackageUploader.exe loop.",
                    _config.PackageFilePath,
                    Msixvc2DelegationGuard.EnvironmentVariableName);
            }
            else
            {
                await UploadMsixvc2PackageAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);
        var marketGroupPackage = await _storeBrokerService.GetGameMarketGroupPackage(product, packageBranch, _config, ct).ConfigureAwait(false);

        var gamePackage = await _storeBrokerService.UploadGamePackageAsync(product, packageBranch, marketGroupPackage, _config.PackageFilePath, _config.GameAssets, _config.MinutesToWaitForProcessing, _config.DeltaUpload, isXvc: true, ct).ConfigureAwait(false);
        _logger.LogDebug("Configuration: PackageFilePath={PackageFilePath}, DeltaUpload={DeltaUpload}, AvailabilityDate={AvailabilityDate}", _config.PackageFilePath, _config.DeltaUpload, _config.AvailabilityDate);
        _logger.LogInformation("Uploaded package with id: {gamePackageId}", gamePackage.Id);

        if (_config.AvailabilityDate is not null || _config.PreDownloadDate is not null)
        {
            await _storeBrokerService.SetXvcConfigurationAsync(product, packageBranch, gamePackage, _config.MarketGroupName, _config, ct).ConfigureAwait(false);
            _logger.LogInformation("Configuration set for Xvc packages");
        }
    }

    /// <summary>
    /// Delegates the upload of an MSIXVC2 package to MakePkg.exe, which owns the MSIXVC2 upload protocol.
    /// Only ever reached when the package has been positively identified as MSIXVC2.
    /// </summary>
    private async Task UploadMsixvc2PackageAsync(CancellationToken ct)
    {
        _logger.LogInformation("MSIXVC2 package detected. Delegating the upload to MakePkg.exe.");

        if (!_msixvc2ToolProvider.IsAvailable || string.IsNullOrWhiteSpace(_msixvc2ToolProvider.ExecutablePath))
        {
            throw new InvalidOperationException(
                "The package is an MSIXVC2 package, but no MSIXVC2-capable MakePkg.exe was found. " +
                "Install the latest Microsoft GDK (or the Microsoft.Xbox.Packaging.Tools package) and try again.");
        }

        var bigId = await ResolveBigIdAsync(ct).ConfigureAwait(false);

        var executablePath = _msixvc2ToolProvider.ExecutablePath;
        var arguments = Msixvc2UploadArgumentBuilder.Build(_config, _msixvc2CommandLineContext, bigId, _logger);

        // The argument string can carry a client secret, so log a redacted form. The child process still
        // receives the real value.
        _logger.LogInformation("Running {executablePath} {arguments}", executablePath, Redact(arguments));

        var exitCode = await _msixvc2ProcessRunner.RunAsync(executablePath, arguments, ct).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"MakePkg.exe failed with exit code {exitCode}.");
        }

        _logger.LogInformation("MSIXVC2 package uploaded successfully.");
    }

    /// <summary>
    /// Replaces the value of any secret-bearing MakePkg.exe flag so credentials never reach the log file.
    /// </summary>
    private static string Redact(string arguments) =>
        Regex.Replace(
            arguments,
            "(?<flag>/(?:clientsecret|certpassword))\\s+\"[^\"]*\"",
            "${flag} \"***\"",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// MakePkg.exe identifies the product by Store ID (/storeid) only. When the config supplies a ProductId
    /// instead, resolve it to the corresponding Big ID through the ingestion service rather than failing.
    /// </summary>
    private async Task<string> ResolveBigIdAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_config.BigId))
        {
            return _config.BigId;
        }

        _logger.LogInformation("Resolving Big ID for product {productId}, which MakePkg.exe requires for MSIXVC2 uploads.", _config.ProductId);

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(product?.BigId))
        {
            throw new InvalidOperationException(
                $"Could not resolve a Big ID for product '{_config.ProductId}'. MakePkg.exe identifies products by Store ID for MSIXVC2 uploads; " +
                "set 'bigId' in the config file or pass --BigId.");
        }

        return product.BigId;
    }
}
