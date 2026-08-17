// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.Application.Config;
using PackageUploader.ClientApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Translates an <see cref="UploadXvcPackageOperationConfig"/> into a MakePkg.exe "upload" command line.
///
/// Every flag emitted here is grounded in the verbatim help output of the MSIXVC2-capable packaging tool
/// (<c>makepkg2.exe upload /?</c>, version 2604.405.14000.0), cross-checked against the two existing UI
/// argument builders. The closest in-repo precedent is <c>PackageUploadViewModel.BuildMsixvc2UploadArguments()</c>,
/// which handles the same scenario as the CLI: an already-built .msixvc package on disk. That builder emits
/// the <c>/pd</c> form and deliberately does NOT pass <c>/msixvc2</c> — that flag only appears in
/// <c>Msixvc2UploadViewModel.BuildUploadArguments()</c>, which packs from a loose content folder via <c>/d</c>.
///
/// Notable grounding results:
/// <list type="bullet">
/// <item><c>/auth</c> accepts Default, Browser, CacheableBrowser, AzureCli, ManagedIdentity,
/// ManagedIdentityFederated, Environment, AzurePipelines, ClientSecret and ClientCertificate — so
/// non-interactive CI authentication is fully supported and is forwarded rather than rejected.</item>
/// <item><c>/tenantid</c>, <c>/clientid</c>, <c>/clientsecret</c>, <c>/certthumbprint</c>, <c>/certstore</c>,
/// <c>/certlocation</c> and <c>/resourceid</c> all exist and carry the credential material.</item>
/// <item>There is no flag naming a certificate <em>file</em>, and no flag for a certificate <em>subject</em>,
/// so those two configurations are rejected rather than silently authenticating as a different identity.</item>
/// <item><c>/uploadsource</c> exists but its enum only accepts <c>makepkg2</c> and <c>XGPM</c>. There is no
/// value representing PackageUploader, so the flag is omitted and the tool's own default is used.</item>
/// </list>
///
/// CAVEAT: the binary this mapping was verified against is <c>makepkg2.exe</c>, not the renamed
/// <c>MakePkg.exe</c> that ships with the GDK once the two tools are merged. The legacy <c>makepkg.exe</c>
/// is demonstrably a different surface (it has <c>/tenantid</c> but no <c>/auth</c> at all), so if the merged
/// MakePkg.exe diverges on <c>/auth</c>, this mapping — and especially
/// <see cref="ResolveAuthenticationMethod"/> — is the first thing to re-verify against its help output.
///
/// Options that MakePkg.exe has no equivalent for are either warned about and ignored (when ignoring them
/// cannot change the outcome) or cause a <see cref="Msixvc2UnsupportedOptionException"/> (when it could).
///
/// Note that <c>availabilityDate</c>/<c>preDownloadDate</c> are deliberately NOT handled here. MakePkg.exe
/// has no flag for them, but they are still honoured: the operation applies them through ingestion after the
/// upload, using the package identity MakePkg.exe reports. This builder must therefore leave them alone
/// rather than treat them as unsupported.
/// </summary>
internal static class Msixvc2UploadArgumentBuilder
{
    /// <summary>Stands in for a credential in the command line built for logging.</summary>
    private const string RedactedValue = "***";

    /// <summary>
    /// The /auth values accepted by the MSIXVC2 packaging tool, taken verbatim from its help output.
    /// PackageUploader's own AuthenticationMethod enum uses the same names for all of these.
    /// </summary>
    private static readonly HashSet<IngestionExtensions.AuthenticationMethod> DirectlySupportedAuthenticationMethods =
    [
        IngestionExtensions.AuthenticationMethod.Default,
        IngestionExtensions.AuthenticationMethod.Browser,
        IngestionExtensions.AuthenticationMethod.CacheableBrowser,
        IngestionExtensions.AuthenticationMethod.AzureCli,
        IngestionExtensions.AuthenticationMethod.ManagedIdentity,
        IngestionExtensions.AuthenticationMethod.ManagedIdentityFederated,
        IngestionExtensions.AuthenticationMethod.Environment,
        IngestionExtensions.AuthenticationMethod.AzurePipelines,
        IngestionExtensions.AuthenticationMethod.ClientSecret,
        IngestionExtensions.AuthenticationMethod.ClientCertificate,
    ];

    public static Msixvc2UploadArguments Build(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext,
        string bigId,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(commandLineContext);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(bigId);

        var packageDirectory = GetPackageDirectory(config.PackageFilePath);

        ValidateUnsupportedOptions(config, packageDirectory, logger);

        var commandLine = BuildCommandLine(config, commandLineContext, bigId, packageDirectory);

        // The log-safe form is BUILT FROM A CONTEXT THAT NEVER HELD THE SECRET rather than produced by
        // scrubbing the finished command line. Post-hoc scrubbing has to keep a pattern in sync with the
        // exact spelling, spacing and quoting the builder happens to emit, and silently leaks the moment
        // those drift or a new credential flag is added. Substituting at the source cannot drift, and it
        // keeps the secret out of the value that reaches the logger entirely.
        var redactedCommandLine = string.IsNullOrWhiteSpace(commandLineContext.ClientSecret)
            ? commandLine
            : BuildCommandLine(
                config,
                commandLineContext with { ClientSecret = RedactedValue },
                bigId,
                packageDirectory);

        return new Msixvc2UploadArguments(commandLine, redactedCommandLine);
    }

    private static string BuildCommandLine(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext,
        string bigId,
        string packageDirectory)
    {
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

        AppendAuthenticationArguments(args, commandLineContext);

        return args.ToString();
    }

    /// <summary>
    /// Forwards the selected authentication method and its credential material. MakePkg.exe performs the
    /// token acquisition itself, so PackageUploader hands over the same identity the user configured rather
    /// than forcing an interactive sign-in — otherwise MSIXVC2 upload would be impossible from any
    /// non-interactive pipeline.
    /// </summary>
    private static void AppendAuthenticationArguments(StringBuilder args, Msixvc2CommandLineContext context)
    {
        var method = ResolveAuthenticationMethod(context.AuthenticationMethod);

        args.Append(Invariant($" /auth {method}"));

        if (!string.IsNullOrWhiteSpace(context.TenantId))
        {
            args.Append(Invariant($" /tenantid \"{context.TenantId}\""));
        }

        if (!string.IsNullOrWhiteSpace(context.ClientId))
        {
            args.Append(Invariant($" /clientid \"{context.ClientId}\""));
        }

        switch (method)
        {
            case IngestionExtensions.AuthenticationMethod.ClientSecret:
                RequireCredential(context.ClientId, "/clientid", "a client id", "AadAuthInfo:ClientId or ClientSecretAuthInfo:ClientId");
                RequireCredential(context.ClientSecret, "/clientsecret", "a client secret", "AadAuthInfo:ClientSecret or ClientSecretAuthInfo:ClientSecret");
                args.Append(Invariant($" /clientsecret \"{context.ClientSecret}\""));
                break;

            case IngestionExtensions.AuthenticationMethod.ClientCertificate:
                AppendCertificateArguments(args, context);
                break;

            case IngestionExtensions.AuthenticationMethod.ManagedIdentityFederated:
                if (!string.IsNullOrWhiteSpace(context.ResourceId))
                {
                    args.Append(Invariant($" /resourceid \"{context.ResourceId}\""));
                }
                break;
        }
    }

    private static void AppendCertificateArguments(StringBuilder args, Msixvc2CommandLineContext context)
    {
        // makepkg2 authenticates from a certificate STORE (thumbprint + store + location). It exposes
        // /certpassword but no flag naming a certificate file, so a PFX path cannot be forwarded.
        if (!string.IsNullOrWhiteSpace(context.CertificatePath))
        {
            throw new Msixvc2UnsupportedOptionException(
                $"Certificate file authentication ('{context.CertificatePath}') cannot be forwarded to MakePkg.exe for MSIXVC2 uploads, " +
                "because MakePkg.exe only accepts a certificate from a Windows certificate store (/certthumbprint, /certstore, /certlocation) " +
                "and has no option naming a certificate file. " +
                $"Import the certificate into a store and use --Authentication {IngestionExtensions.AuthenticationMethod.AppCert} " +
                "with AadAuthInfo:CertificateThumbprint, or choose a different authentication method.");
        }

        // makepkg2 has no certificate-subject option. Resolving the subject ourselves and forwarding the
        // resulting thumbprint would be guesswork about which certificate the user meant.
        if (!string.IsNullOrWhiteSpace(context.CertificateSubject))
        {
            throw new Msixvc2UnsupportedOptionException(
                $"Certificate subject authentication ('{context.CertificateSubject}') cannot be forwarded to MakePkg.exe for MSIXVC2 uploads, " +
                "because MakePkg.exe selects certificates by thumbprint only. " +
                "Set AadAuthInfo:CertificateThumbprint instead of AadAuthInfo:CertificateSubject.");
        }

        RequireCredential(context.ClientId, "/clientid", "a client id", "AadAuthInfo:ClientId");
        RequireCredential(context.CertificateThumbprint, "/certthumbprint", "a certificate thumbprint", "AadAuthInfo:CertificateThumbprint");

        args.Append(Invariant($" /certthumbprint \"{context.CertificateThumbprint}\""));

        if (!string.IsNullOrWhiteSpace(context.CertificateStore))
        {
            args.Append(Invariant($" /certstore \"{context.CertificateStore}\""));
        }

        if (!string.IsNullOrWhiteSpace(context.CertificateLocation))
        {
            args.Append(Invariant($" /certlocation \"{context.CertificateLocation}\""));
        }
    }

    /// <summary>
    /// Maps PackageUploader's AuthenticationMethod onto a /auth value MakePkg.exe accepts.
    /// All but two names are shared verbatim. AppSecret and AppCert are PackageUploader's legacy names for
    /// the same AAD application flows that MakePkg.exe calls ClientSecret and ClientCertificate — both
    /// authenticate an AAD application with, respectively, a client secret or a store certificate, so the
    /// rename is a straight alias rather than a behavioral change.
    /// </summary>
    private static IngestionExtensions.AuthenticationMethod ResolveAuthenticationMethod(
        IngestionExtensions.AuthenticationMethod method) => method switch
        {
            IngestionExtensions.AuthenticationMethod.AppSecret => IngestionExtensions.AuthenticationMethod.ClientSecret,
            IngestionExtensions.AuthenticationMethod.AppCert => IngestionExtensions.AuthenticationMethod.ClientCertificate,
            _ when DirectlySupportedAuthenticationMethods.Contains(method) => method,
            _ => throw new Msixvc2UnsupportedOptionException(
                $"--Authentication {method} has no MakePkg.exe equivalent for MSIXVC2 uploads. " +
                "MakePkg.exe accepts: Default, Browser, CacheableBrowser, AzureCli, ManagedIdentity, " +
                "ManagedIdentityFederated, Environment, AzurePipelines, ClientSecret, ClientCertificate."),
        };

    /// <summary>
    /// Throws when a credential MakePkg.exe requires for the chosen method is missing.
    /// <paramref name="value"/> may be null or empty — that is precisely the condition being detected.
    /// </summary>
    private static void RequireCredential(string value, string flag, string description, string configPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Msixvc2UnsupportedOptionException(
                $"MakePkg.exe requires {description} ({flag}) for this authentication method during an MSIXVC2 upload, " +
                $"but none was configured. Set {configPath} in the config file.");
        }
    }

    private static void ValidateUnsupportedOptions(
        UploadXvcPackageOperationConfig config,
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

    /// <summary>
    /// Fails when a configured asset path sits outside the package directory.
    /// <paramref name="assetPath"/> may be null or empty, in which case the asset is simply not configured.
    /// </summary>
    private static void RequireInPackageDirectory(string propertyName, string assetPath, string packageDirectory)
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
