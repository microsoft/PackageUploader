// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Config;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Packaging;
using System;
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
            // Two independent signals say "MakePkg.exe is already in this call chain", and either one means
            // delegating again risks the unbounded cycle above.
            //
            // Both fall through to the normal XVC1 upload rather than failing, which is the outcome that is
            // correct either way: for a false-positive XVC1 package the upload simply succeeds, and for a
            // genuine MSIXVC2 package it fails, which is what an un-delegatable MSIXVC2 package should do.
            if (_msixvc2DelegationGuard.IsDelegatedInvocation)
            {
                _logger.LogWarning(
                    "Package '{PackageFilePath}' looks like MSIXVC2, but this PackageUploader process was started by MakePkg.exe ({EnvironmentVariable} is set). " +
                    "Uploading directly instead of delegating back to MakePkg.exe, to avoid an infinite MakePkg.exe/PackageUploader.exe loop. " +
                    "If the upload fails, one possible cause is that the package really is MSIXVC2 and was handed over in error; installing the latest Microsoft GDK is the first thing to try.",
                    _config.PackageFilePath,
                    Msixvc2DelegationGuard.EnvironmentVariableName);

                await UploadXvcPackageAsync(ct).ConfigureAwait(false);
                return;
            }

            // The environment stamp above only covers cycles this executable started. When MakePkg.exe is the
            // entry point it invokes us without any stamp, so the parent process is checked too. MakePkg.exe
            // only invokes PackageUploader.exe for XVC1/MSIXVC1 packages, so a MakePkg.exe parent contradicts
            // the MSIXVC2 detection, and the parent is the more trustworthy of the two signals.
            var makePkgParent = _msixvc2DelegationGuard.GetMakePkgParentProcessName();

            if (makePkgParent is not null)
            {
                _logger.LogWarning(
                    "Package '{PackageFilePath}' looks like MSIXVC2, but PackageUploader was started by '{ParentProcessName}', which only hands over " +
                    "XVC1/MSIXVC1 packages. Treating the package as XVC1 and uploading directly, to avoid an infinite MakePkg.exe/PackageUploader.exe loop. " +
                    "If the upload fails, one possible cause is that the package really is MSIXVC2 and was handed over in error; installing the latest Microsoft GDK is the first thing to try.",
                    _config.PackageFilePath,
                    makePkgParent);

                await UploadXvcPackageAsync(ct).ConfigureAwait(false);
                return;
            }

            await UploadMsixvc2PackageAsync(ct).ConfigureAwait(false);
            return;
        }

        await UploadXvcPackageAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// The original, unchanged XVC1/MSIXVC1 upload path, which is also the fallback whenever an MSIXVC2
    /// detection cannot be acted on because MakePkg.exe is already in the process chain.
    /// </summary>
    private async Task UploadXvcPackageAsync(CancellationToken ct)
    {
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
                "Install the latest Microsoft GDK and try again.");
        }

        var bigId = await ResolveBigIdAsync(ct).ConfigureAwait(false);

        var executablePath = _msixvc2ToolProvider.ExecutablePath;
        var arguments = Msixvc2UploadArgumentBuilder.Build(_config, _msixvc2CommandLineContext, bigId, _logger);

        // Only the redacted form is ever logged. It is built from credential-free inputs rather than
        // scrubbed after the fact, so no credential reaches the logger. The child process below still
        // receives the real command line.
        _logger.LogInformation("Running {executablePath} {arguments}", executablePath, arguments.RedactedCommandLine);

        var result = await _msixvc2ProcessRunner.RunAsync(executablePath, arguments.CommandLine, ct).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"MakePkg.exe failed with exit code {result.ExitCode}.");
        }

        _logger.LogInformation("MSIXVC2 package uploaded successfully.");

        // Mirrors the XVC1 condition exactly, so a configured date behaves the same on both paths —
        // including a disabled date, which clears any previously set value rather than being a no-op.
        if (_config.AvailabilityDate is not null || _config.PreDownloadDate is not null)
        {
            await SetMsixvc2ConfigurationAsync(result.UploadedPackageId, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies availability and pre-download dates to the package MakePkg.exe just uploaded.
    ///
    /// MakePkg.exe does not set these itself, but it does name the package it created, so the same
    /// ingestion call the XVC1 path uses can be reused. The reported identity is resolved against the
    /// packages actually present in the target branch and market group rather than being trusted
    /// outright: that both yields the real <see cref="GamePackage"/> and proves the identity belongs
    /// where the dates are about to be written.
    ///
    /// Every failure here is loud. The upload has already succeeded at this point, so silently skipping
    /// the dates would leave a package live on a date the caller never asked for.
    /// </summary>
    private async Task SetMsixvc2ConfigurationAsync(string uploadedPackageId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(uploadedPackageId))
        {
            throw new InvalidOperationException(
                "The MSIXVC2 package uploaded successfully, but MakePkg.exe did not report which package it created, " +
                "so 'availabilityDate'/'preDownloadDate' could not be applied. The upload itself is unaffected. " +
                "Set the dates in Partner Center, or re-run this operation without them once the dates are set.");
        }

        var product = await _storeBrokerService.GetProductAsync(_config, ct).ConfigureAwait(false);
        var packageBranch = await _storeBrokerService.GetGamePackageBranch(product, _config, ct).ConfigureAwait(false);

        GamePackage gamePackage = null;

        await foreach (var package in _storeBrokerService
                           .GetGamePackagesAsync(product, packageBranch, _config.MarketGroupName, ct)
                           .ConfigureAwait(false))
        {
            if (string.Equals(package.Id, uploadedPackageId, StringComparison.OrdinalIgnoreCase))
            {
                gamePackage = package;
                break;
            }
        }

        if (gamePackage is null)
        {
            throw new InvalidOperationException(
                $"The MSIXVC2 package uploaded successfully, but the package MakePkg.exe reported ('{uploadedPackageId}') is not in " +
                $"market group '{_config.MarketGroupName}', so 'availabilityDate'/'preDownloadDate' were not applied. " +
                "The upload itself is unaffected. Set the dates in Partner Center.");
        }

        _logger.LogInformation("Uploaded package with id: {gamePackageId}", gamePackage.Id);

        await _storeBrokerService.SetXvcConfigurationAsync(product, packageBranch, gamePackage, _config.MarketGroupName, _config, ct).ConfigureAwait(false);
        _logger.LogInformation("Configuration set for Xvc packages");
    }

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
