// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.Application.Config;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Translates an <see cref="UploadXvcPackageOperationConfig"/> into a MakePkg.exe "upload" command line.
///
/// The mapping is grounded in MakePkg.exe's own option declarations rather than in help text: every flag
/// emitted here appears in the upload command's supported-option list, so an option this builder emits is
/// one MakePkg.exe will accept for a packaged upload.
///
/// Notable grounding results:
/// <list type="bullet">
/// <item><c>/auth</c> accepts Default, Browser, CacheableBrowser, AzureCli, ManagedIdentity,
/// ManagedIdentityFederated, Environment, AzurePipelines, ClientSecret and ClientCertificate — so
/// non-interactive CI authentication is fully supported and is forwarded rather than rejected.</item>
/// <item>Certificate authentication accepts EXACTLY ONE selector: <c>/certpath</c>, <c>/certthumbprint</c>
/// or <c>/certsubject</c>. MakePkg.exe rejects a command line carrying more than one, so this builder
/// enforces the same rule up front with a message naming the configuration keys involved.</item>
/// <item><c>/pd</c> accepts either a package file or a directory containing a single package, so the
/// configured package file path is passed through verbatim rather than being reduced to its directory —
/// which would be ambiguous whenever a directory holds more than one package.</item>
/// <item><c>/sodb</c> exists, so the SODB asset path is forwarded rather than rejected.</item>
/// <item>The four date flags (<c>/availabilitydate</c>, <c>/clearavailabilitydate</c>,
/// <c>/predownloaddate</c>, <c>/clearpredownloaddate</c>) exist, so MakePkg.exe applies the schedule
/// itself. PackageUploader no longer writes dates through ingestion after an MSIXVC2 upload.</item>
/// <item><c>/disclayout</c> is rejected by MakePkg.exe for every non-XVC1 format, and MSIXVC2 has no
/// disc-layout asset upload at all, so a configured disc layout is a hard error rather than a silent
/// drop.</item>
/// </list>
///
/// Options that MakePkg.exe has no equivalent for are either warned about and ignored (when ignoring them
/// cannot change the outcome) or cause a <see cref="Msixvc2UnsupportedOptionException"/> (when it could).
/// </summary>
internal static class Msixvc2UploadArgumentBuilder
{
    /// <summary>Stands in for a credential in the command line built for logging.</summary>
    private const string RedactedValue = "***";

    /// <summary>
    /// The /auth values accepted by MakePkg.exe, taken verbatim from its option declarations.
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

        var commandLine = BuildCommandLine(config, commandLineContext, bigId);

        // The log-safe form is BUILT FROM A CONTEXT THAT NEVER HELD THE SECRET rather than produced by
        // scrubbing the finished command line. Post-hoc scrubbing has to keep a pattern in sync with the
        // exact spelling, spacing and quoting the builder happens to emit, and silently leaks the moment
        // those drift or a new credential flag is added. Substituting at the source cannot drift, and it
        // keeps the secret out of the value that reaches the logger entirely.
        var hasCredential =
            !string.IsNullOrWhiteSpace(commandLineContext.ClientSecret) ||
            !string.IsNullOrWhiteSpace(commandLineContext.CertificatePassword);

        var redactedCommandLine = hasCredential
            ? BuildCommandLine(
                config,
                commandLineContext with
                {
                    ClientSecret = string.IsNullOrWhiteSpace(commandLineContext.ClientSecret) ? commandLineContext.ClientSecret : RedactedValue,
                    CertificatePassword = string.IsNullOrWhiteSpace(commandLineContext.CertificatePassword) ? commandLineContext.CertificatePassword : RedactedValue,
                },
                bigId)
            : commandLine;

        return new Msixvc2UploadArguments(commandLine, redactedCommandLine);
    }

    private static string BuildCommandLine(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext,
        string bigId)
    {
        var args = new StringBuilder();
        args.Append("upload");

        // /pd takes the package file itself, so the configured path is passed through unchanged. Reducing
        // it to its directory would be ambiguous the moment a directory contains more than one package.
        args.Append(Invariant($" /pd \"{Path.GetFullPath(config.PackageFilePath)}\""));

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
        AppendPackageArguments(args, config);
        AppendScheduleArguments(args, config);

        return args.ToString();
    }

    /// <summary>
    /// Forwards the SODB asset, which has a direct MakePkg.exe equivalent.
    ///
    /// Delta upload is deliberately NOT forwarded. MSIXVC2 never re-uploads unchanged content, so the
    /// notion of an opt-in delta does not apply to this format, and MakePkg.exe decides for itself what to
    /// transfer. Passing a flag to say so would be redundant at best.
    /// </summary>
    private static void AppendPackageArguments(StringBuilder args, UploadXvcPackageOperationConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.GameAssets?.SodbFilePath))
        {
            args.Append(Invariant($" /sodb \"{config.GameAssets.SodbFilePath}\""));
        }
    }

    /// <summary>
    /// Forwards availability and pre-download dates.
    ///
    /// <see cref="GamePackageDate"/> carries a tri-state that maps exactly onto MakePkg.exe's flag pairs:
    /// an enabled date sets a value, a disabled date CLEARS any previously set value, and an absent date
    /// leaves the current value alone. The clear flags are what make the middle case expressible — without
    /// them a disabled date would be indistinguishable from an absent one, and PackageUploader would
    /// silently stop honouring a configuration that works on the XVC1 path.
    /// </summary>
    private static void AppendScheduleArguments(StringBuilder args, UploadXvcPackageOperationConfig config)
    {
        AppendDate(args, config.AvailabilityDate, "/availabilitydate", "/clearavailabilitydate", nameof(config.AvailabilityDate));
        AppendDate(args, config.PreDownloadDate, "/predownloaddate", "/clearpredownloaddate", nameof(config.PreDownloadDate));
    }

    private static void AppendDate(StringBuilder args, GamePackageDate date, string setFlag, string clearFlag, string configName)
    {
        if (date is null)
        {
            return;
        }

        if (!date.IsEnabled)
        {
            args.Append(' ').Append(clearFlag);
            return;
        }

        if (date.EffectiveDate is null)
        {
            // Defence in depth: UploadXvcPackageOperationConfig.Validate already rejects this combination,
            // so reaching here means the config was constructed in code rather than bound and validated.
            throw new Msixvc2UnsupportedOptionException(
                $"'{ToCamelCase(configName)}' is enabled but has no 'effectiveDate', so no date could be passed to MakePkg.exe. " +
                "Set an effective date or disable the option.");
        }

        args.Append(Invariant($" {setFlag} \"{FormatDate(date.EffectiveDate.Value)}\""));
    }

    /// <summary>
    /// Renders a date in the round-trip ISO 8601 form MakePkg.exe parses.
    ///
    /// <see cref="GamePackageDate"/> already normalizes to UTC on assignment, so in practice the value
    /// arrives with <see cref="DateTimeKind.Utc"/> and both paths agree on the instant. The switch is
    /// defence for a value that somehow arrives otherwise: MakePkg.exe parses with
    /// AssumeUniversal|AdjustToUniversal, so an <see cref="DateTimeKind.Unspecified"/> value is STAMPED as
    /// UTC rather than converted from local time. Converting would shift the instant by the host's offset
    /// and silently move a release date.
    /// </summary>
    private static string FormatDate(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return utc.ToString("o", CultureInfo.InvariantCulture);
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

    /// <summary>
    /// Emits the single certificate selector MakePkg.exe expects.
    ///
    /// MakePkg.exe requires EXACTLY ONE of /certpath, /certthumbprint and /certsubject and fails the whole
    /// upload when given more than one. That rule is enforced here instead of being discovered by the child
    /// process, so the error names PackageUploader's own configuration keys and arrives before any work
    /// starts. /certstore and /certlocation are not selectors — they only narrow a store lookup — so they
    /// are emitted only alongside the two store-based selectors.
    /// </summary>
    private static void AppendCertificateArguments(StringBuilder args, Msixvc2CommandLineContext context)
    {
        RequireCredential(context.ClientId, "/clientid", "a client id", "AadAuthInfo:ClientId");

        var selectors = new List<(string Flag, string Value, string ConfigPath, bool UsesStore)>();

        if (!string.IsNullOrWhiteSpace(context.CertificatePath))
        {
            selectors.Add(("/certpath", context.CertificatePath, "AadAuthInfo:CertificatePath", false));
        }

        if (!string.IsNullOrWhiteSpace(context.CertificateThumbprint))
        {
            selectors.Add(("/certthumbprint", context.CertificateThumbprint, "AadAuthInfo:CertificateThumbprint", true));
        }

        if (!string.IsNullOrWhiteSpace(context.CertificateSubject))
        {
            selectors.Add(("/certsubject", context.CertificateSubject, "AadAuthInfo:CertificateSubject", true));
        }

        if (selectors.Count == 0)
        {
            throw new Msixvc2UnsupportedOptionException(
                "MakePkg.exe requires a certificate path, thumbprint, or subject (/certpath, /certthumbprint or /certsubject) " +
                "for certificate authentication during an MSIXVC2 upload, but none was configured. " +
                "Set AadAuthInfo:CertificatePath, AadAuthInfo:CertificateThumbprint or AadAuthInfo:CertificateSubject in the config file.");
        }

        if (selectors.Count > 1)
        {
            throw new Msixvc2UnsupportedOptionException(
                "MakePkg.exe accepts only one certificate selector for an MSIXVC2 upload, but " +
                $"{string.Join(", ", selectors.ConvertAll(selector => selector.ConfigPath))} are all configured. " +
                "Remove all but one of them from the config file.");
        }

        var (flag, value, _, usesStore) = selectors[0];

        args.Append(Invariant($" {flag} \"{value}\""));

        if (!usesStore)
        {
            // A certificate file may be password-protected. The password is a credential, so it is
            // redacted before anything is logged.
            if (!string.IsNullOrWhiteSpace(context.CertificatePassword))
            {
                args.Append(Invariant($" /certpassword \"{context.CertificatePassword}\""));
            }

            return;
        }

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
    /// authenticate an AAD application with, respectively, a client secret or a certificate, so the
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

        if (config.DeltaUpload)
        {
            // Not an error: MSIXVC2 never re-uploads unchanged content, so the caller already gets what
            // 'deltaUpload' asks for. Warned rather than silently dropped so the flag's absence from the
            // command line is not mistaken for a bug.
            logger.LogWarning(
                "'deltaUpload' is not passed to MakePkg.exe for MSIXVC2 uploads; MSIXVC2 packages always avoid re-uploading unchanged content.");
        }

        // MakePkg.exe owns the upload lifecycle and reports completion itself, so a caller-supplied
        // processing timeout cannot change the outcome. An explicitly configured 30 is indistinguishable
        // from the default, so this is always a warning rather than an error.
        logger.LogWarning(
            "'minutesToWaitForProcessing' is not used for MSIXVC2 uploads; MakePkg.exe manages upload processing itself.");
    }

    /// <summary>
    /// MakePkg.exe uploads the EKB, submission validator log, and symbol bundle when they sit alongside the
    /// package, discovering them by co-location rather than by path, so those paths are not forwarded and
    /// assets pointing elsewhere are a hard error — silently dropping a file the user deliberately placed
    /// somewhere else would change the outcome.
    ///
    /// SODB is different: it has a real MakePkg.exe flag, so its path is forwarded from anywhere and is not
    /// held to the co-location rule. Disc layout is different again: MakePkg.exe has no MSIXVC2 disc-layout
    /// upload at all, so it is always rejected.
    /// </summary>
    private static void ValidateGameAssets(UploadXvcPackageOperationConfig config, string packageDirectory, ILogger logger)
    {
        if (config.GameAssets is null)
        {
            return;
        }

        RejectDiscLayout(config.GameAssets.DiscLayoutFilePath);

        RequireInPackageDirectory(GameAssetPaths.EkbFilePath, config.GameAssets.EkbFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.SubValFilePath, config.GameAssets.SubValFilePath, packageDirectory);
        RequireInPackageDirectory(GameAssetPaths.SymbolsFilePath, config.GameAssets.SymbolsFilePath, packageDirectory);

        if (string.IsNullOrWhiteSpace(config.GameAssets.EkbFilePath) &&
            string.IsNullOrWhiteSpace(config.GameAssets.SubValFilePath) &&
            string.IsNullOrWhiteSpace(config.GameAssets.SymbolsFilePath))
        {
            return;
        }

        logger.LogWarning(
            "'gameAssets' paths other than 'sodbFilePath' are not forwarded to MakePkg.exe for MSIXVC2 uploads. The configured assets already sit in the package directory '{PackageDirectory}', where MakePkg.exe discovers and uploads them.",
            packageDirectory);
    }

    /// <summary>
    /// Fails whenever a disc layout asset is configured. MakePkg.exe rejects /disclayout for every
    /// non-XVC1 format and has no MSIXVC2 disc-layout asset upload, so there is no location and no flag
    /// that would get the file uploaded. Accepting it silently would produce a successful upload that is
    /// missing an asset the caller asked for.
    /// </summary>
    private static void RejectDiscLayout(string discLayoutFilePath)
    {
        if (string.IsNullOrWhiteSpace(discLayoutFilePath))
        {
            return;
        }

        throw new Msixvc2UnsupportedOptionException(
            $"'gameAssets.{ToCamelCase(GameAssetPaths.DiscLayoutFilePath)}' points at '{discLayoutFilePath}', but MSIXVC2 uploads are performed by MakePkg.exe, which does not upload a disc layout file for MSIXVC2 packages. " +
            "Moving the file next to the package does not help, because there is no MSIXVC2 disc-layout upload at all. " +
            "Remove it from the config file.");
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
                "MakePkg.exe only picks up files that sit alongside the package, so this file would not be uploaded. " +
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
    }
}
