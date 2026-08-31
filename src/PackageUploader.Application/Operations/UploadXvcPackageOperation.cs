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
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal class UploadXvcPackageOperation(
    IPackageUploaderService storeBrokerService,
    ILogger<UploadXvcPackageOperation> logger,
    IOptions<UploadXvcPackageOperationConfig> config,
    IMsixvc2UploadToolProvider msixvc2ToolProvider,
    IMsixvc2ProcessRunner msixvc2ProcessRunner,
    IMakePkgFeatureProbe makePkgFeatureProbe,
    Msixvc2CommandLineContext msixvc2CommandLineContext) : Operation(logger)
{
    private readonly IPackageUploaderService _storeBrokerService = storeBrokerService ?? throw new ArgumentNullException(nameof(storeBrokerService));
    private readonly ILogger<UploadXvcPackageOperation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly UploadXvcPackageOperationConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly IMsixvc2UploadToolProvider _msixvc2ToolProvider = msixvc2ToolProvider ?? throw new ArgumentNullException(nameof(msixvc2ToolProvider));
    private readonly IMsixvc2ProcessRunner _msixvc2ProcessRunner = msixvc2ProcessRunner ?? throw new ArgumentNullException(nameof(msixvc2ProcessRunner));
    private readonly IMakePkgFeatureProbe _makePkgFeatureProbe = makePkgFeatureProbe ?? throw new ArgumentNullException(nameof(makePkgFeatureProbe));
    private readonly Msixvc2CommandLineContext _msixvc2CommandLineContext = msixvc2CommandLineContext ?? throw new ArgumentNullException(nameof(msixvc2CommandLineContext));

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting {operationName} operation.", _config.GetOperationName());

        // Loose content is rejected before anything else. PackageUploader uploads a BUILT package; it has no
        // packaging step, and for MSIXVC2 the pack-and-upload flow belongs to MakePkg.exe from end to end.
        // Without this the caller gets "Package file not found" from the upload layer, which is true but
        // says nothing about why a perfectly valid content directory was refused.
        if (PackageFormatDetector.IsLooseGameContent(_config.PackageFilePath))
        {
            var gameConfig = PackageFormatDetector.FindGameConfig(_config.PackageFilePath);

            throw new InvalidOperationException(
                $"'{_config.PackageFilePath}' is loose game content, not a built package" +
                (gameConfig is null ? "" : $" (it is described by '{gameConfig}')") + ". " +
                "PackageUploader uploads an existing package file and cannot build one. " +
                "Use MakePkg.exe to pack and upload loose content, or set 'packageFilePath' to the built .msixvc/.xvc file.");
        }

        // SAFETY: only MSIXVC2 packages are ever delegated to MakePkg.exe. This guard is deliberately the
        // package-format detection itself (not a config flag) so it cannot be bypassed by configuration,
        // and it keeps the XVC1/MSIXVC1 path below completely untouched.
        if (PackageFormatDetector.IsLikelyMsixvc2Package(_config.PackageFilePath))
        {
            await UploadMsixvc2PackageAsync(ct).ConfigureAwait(false);
            return;
        }

        await UploadXvcPackageAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// The original XVC1/MSIXVC1 upload path, unchanged by MSIXVC2 support.
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

        var executablePath = _msixvc2ToolProvider.ExecutablePath;

        // SAFETY + capability gate, in one check.
        //
        // Older MakePkg.exe releases performed XVC1/MSIXVC1 uploads by shelling back out to
        // PackageUploader.exe. Delegating to one of those would put the two executables in an unbounded
        // MakePkg.exe -> PackageUploader.exe -> MakePkg.exe cycle if the package format were ever
        // misidentified. The release that started advertising "xvc1upload" is the same release that stopped
        // invoking PackageUploader.exe, so this single probe answers both questions at once: the tool can
        // perform the upload, AND it is not a tool that would call back into us. A tool that fails the probe
        // is never launched, so no cycle is reachable and no separate recursion guard is needed.
        if (!await _makePkgFeatureProbe.SupportsAsync(executablePath, MakePkgFeatures.Xvc1Upload, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"The package is an MSIXVC2 package, but '{executablePath}' is too old to perform the upload " +
                $"(it does not report the '{MakePkgFeatures.Xvc1Upload}' capability). " +
                "Install the latest Microsoft GDK and try again.");
        }

        var bigId = await ResolveBigIdAsync(ct).ConfigureAwait(false);

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
