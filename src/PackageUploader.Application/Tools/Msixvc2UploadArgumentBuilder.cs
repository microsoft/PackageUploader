// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Extensions.Logging;
using PackageUploader.Application.Config;
using PackageUploader.ClientApi;
using System;
using System.IO;
using System.Text;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Translates an <see cref="UploadXvcPackageOperationConfig"/> into a MakePkg.exe "upload" command line.
///
/// Every flag emitted here is grounded in existing repository code that already shells out to the tool.
/// The closest precedent is <c>PackageUploadViewModel.BuildMsixvc2UploadArguments()</c>, which handles the
/// same scenario as the CLI: an already-built .msixvc package on disk. That builder emits
/// <c>upload /pd "&lt;dir&gt;" [/branch|/flight] [/market] [/storeid] /auth CacheableBrowser</c> and deliberately
/// does NOT pass /msixvc2 — that flag only appears in <c>Msixvc2UploadViewModel.BuildUploadArguments()</c>,
/// which packs from a loose content folder via /d. The CLI mirrors the /pd form.
///
/// Options that MakePkg.exe has no equivalent for are either warned about and ignored (when ignoring them
/// cannot change the outcome) or cause a <see cref="Msixvc2UnsupportedOptionException"/> (when it could).
/// </summary>
internal static class Msixvc2UploadArgumentBuilder
{
    /// <summary>
    /// The only /auth value with precedent in this repository. Both UI argument builders emit this literal.
    /// MakePkg.exe performs the interactive sign-in itself, so no other credential can be forwarded.
    /// </summary>
    private const string MakePkgAuthenticationValue = "CacheableBrowser";

    public static string Build(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext,
        string bigId,
        bool supportsUploadSource,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(commandLineContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(bigId);

        var packageDirectory = GetPackageDirectory(config.PackageFilePath);

        ValidateUnsupportedOptions(config, commandLineContext, packageDirectory, logger);

        var args = new StringBuilder();
        args.Append("upload");
        args.Append(Invariant($" /pd \"{packageDirectory}\""));

        if (!string.IsNullOrWhiteSpace(config.BranchFriendlyName))
        {
            args.Append(Invariant($" /branch \"{config.BranchFriendlyName}\""));
        }
        else if (!string.IsNullOrWhiteSpace(config.FlightName))
        {
            args.Append(Invariant($" /flight \"{config.FlightName}\""));
        }

        if (!string.IsNullOrWhiteSpace(config.MarketGroupName))
        {
            args.Append(Invariant($" /market \"{config.MarketGroupName}\""));
        }

        args.Append(Invariant($" /storeid \"{bigId}\""));

        if (supportsUploadSource)
        {
            args.Append(Invariant($" /uploadsource {IngestionExtensions.PackageUploaderUploadSource}"));
        }

        args.Append(Invariant($" /auth {MakePkgAuthenticationValue}"));

        return args.ToString();
    }

    private static void ValidateUnsupportedOptions(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext,
        string packageDirectory,
        ILogger logger)
    {
        ValidateGameAssets(config, packageDirectory, logger);

        // MakePkg.exe owns the upload lifecycle and reports completion itself, so a caller-supplied
        // processing timeout cannot change the outcome. An explicitly configured 30 is indistinguishable
        // from the default, so this is always a warning rather than an error.
        logger.LogWarning(
            "'minutesToWaitForProcessing' is not used for MSIXVC2 uploads; MakePkg.exe manages upload processing itself.");

        if (config.DeltaUpload)
        {
            logger.LogWarning(
                "'deltaUpload' is not used for MSIXVC2 uploads and will be ignored; MakePkg.exe decides its own chunk reuse strategy.");
        }

        // Availability/pre-download dates are applied today by a post-upload SetXvcConfigurationAsync call,
        // which needs the specific GamePackage that was just uploaded. MakePkg.exe does not report that
        // identity back, and guessing which package it created could reconfigure the wrong package.
        if (config.AvailabilityDate?.IsEnabled == true)
        {
            throw new Msixvc2UnsupportedOptionException(
                "'availabilityDate' cannot be applied during an MSIXVC2 upload because MakePkg.exe does not report which package it uploaded. " +
                "Remove it from this config and set the availability date in Partner Center, or with a separate PackageUploader invocation, after the upload completes.");
        }

        if (config.PreDownloadDate?.IsEnabled == true)
        {
            throw new Msixvc2UnsupportedOptionException(
                "'preDownloadDate' cannot be applied during an MSIXVC2 upload because MakePkg.exe does not report which package it uploaded. " +
                "Remove it from this config and set the pre-download date in Partner Center, or with a separate PackageUploader invocation, after the upload completes.");
        }

        if (!string.IsNullOrWhiteSpace(commandLineContext.TenantId))
        {
            throw new Msixvc2UnsupportedOptionException(
                "--TenantId has no MakePkg.exe equivalent for MSIXVC2 uploads. Remove it and sign in with the account's default tenant.");
        }

        // MakePkg.exe performs the sign-in itself and cannot accept a forwarded client secret or certificate.
        // CacheableBrowser is the only /auth value with precedent in this repository.
        if (commandLineContext.AuthenticationMethod is not IngestionExtensions.AuthenticationMethod.CacheableBrowser)
        {
            throw new Msixvc2UnsupportedOptionException(
                $"--Authentication {commandLineContext.AuthenticationMethod} cannot be forwarded to MakePkg.exe for MSIXVC2 uploads, " +
                "because MakePkg.exe performs its own interactive sign-in. " +
                $"Use --Authentication {IngestionExtensions.AuthenticationMethod.CacheableBrowser}.");
        }
    }

    /// <summary>
    /// MSIXVC2 packages do not use EKB or submission validator assets, and MakePkg.exe has no flags for them.
    /// MakePkg.exe expects any such files to sit alongside the package, so assets already in the package
    /// directory are harmless and are only warned about. Assets pointing elsewhere are a hard error, because
    /// silently dropping a file the user deliberately placed somewhere else would change the outcome.
    /// </summary>
    private static void ValidateGameAssets(UploadXvcPackageOperationConfig config, string packageDirectory, ILogger logger)
    {
        if (config.GameAssets is null)
        {
            return;
        }

        RequireInPackageDirectory(GameAssetPaths.EkbFilePath, config.GameAssets.EkbFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.SubValFilePath, config.GameAssets.SubValFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.SymbolsFilePath, config.GameAssets.SymbolsFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.DiscLayoutFilePath, config.GameAssets.DiscLayoutFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.SodbFilePath, config.GameAssets.SodbFilePath, packageDirectory);

        logger.LogWarning(
            "'gameAssets' is not used for MSIXVC2 uploads and will be ignored. The configured assets already sit in the package directory '{PackageDirectory}', where MakePkg.exe expects them.",
            packageDirectory);
    }

    private static void RequireInPackageDirectory(string propertyName, string? assetPath, string packageDirectory)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        var assetDirectory = Path.GetDirectoryName(Path.GetFullPath(assetPath));

        if (!string.Equals(assetDirectory, packageDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new Msixvc2UnsupportedOptionException(
                $"'gameAssets.{ToCamelCase(propertyName)}' points at '{assetPath}', which is outside the package directory '{packageDirectory}'. " +
                "MSIXVC2 uploads ignore gameAssets, and MakePkg.exe only picks up files that sit alongside the package, so this file would not be uploaded. " +
                "Move it into the package directory or remove it from the config file.");
        }
    }

    private static string GetPackageDirectory(string packageFilePath)
    {
        // MakePkg.exe uploads an already-built package directory via /pd, not an individual package file.
        var directory = Path.GetDirectoryName(Path.GetFullPath(packageFilePath));
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new Msixvc2UnsupportedOptionException(
                $"Could not determine the package directory for 'packageFilePath' value '{packageFilePath}'.");
        }

        return directory;
    }

    private static string ToCamelCase(string value) => char.ToLowerInvariant(value[0]) + value[1..];

    private static string Invariant(FormattableString formattable) => FormattableString.Invariant(formattable);

    /// <summary>Names of the GameAssets properties, used for nameof() in error messages.</summary>
    private static class GameAssetPaths
    {
        public const string EkbFilePath = nameof(EkbFilePath);
        public const string SubValFilePath = nameof(SubValFilePath);
        public const string SymbolsFilePath = nameof(SymbolsFilePath);
        public const string DiscLayoutFilePath = nameof(DiscLayoutFilePath);
        public const string SodbFilePath = nameof(SodbFilePath);
    }
}
