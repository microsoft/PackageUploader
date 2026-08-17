// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.Application.Config;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System;
using System.IO;

namespace PackageUploader.Application.Test.Tools;

[TestClass]
public class Msixvc2UploadArgumentBuilderTest
{
    private const string BigId = "9NBLGGH4R315";

    private Mock<ILogger> _loggerMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _loggerMock = new Mock<ILogger>();
    }

    private static UploadXvcPackageOperationConfig CreateConfig(string packagePath) => new()
    {
        BigId = BigId,
        BranchFriendlyName = "Main",
        MarketGroupName = "default",
        PackageFilePath = packagePath,
    };

    private static Msixvc2CommandLineContext BrowserContext() =>
        new(IngestionExtensions.AuthenticationMethod.CacheableBrowser);

    /// <summary>
    /// Returns the executable command line. Redaction is asserted separately, so the existing argument
    /// expectations continue to describe exactly what MakePkg.exe receives.
    /// </summary>
    private string Build(UploadXvcPackageOperationConfig config, Msixvc2CommandLineContext context) =>
        Msixvc2UploadArgumentBuilder.Build(config, context, BigId, _loggerMock.Object).CommandLine;

    private Msixvc2UploadArguments BuildBoth(UploadXvcPackageOperationConfig config, Msixvc2CommandLineContext context) =>
        Msixvc2UploadArgumentBuilder.Build(config, context, BigId, _loggerMock.Object);

    [TestMethod]
    public void Build_WithBranch_ProducesExpectedArguments()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);

        var arguments = Build(config, BrowserContext());

        Assert.AreEqual(
            $"upload /pd \"{package.Directory}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" /auth CacheableBrowser",
            arguments);
    }

    [TestMethod]
    public void Build_WithFlight_UsesFlightInsteadOfBranch()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        config.BranchFriendlyName = null;
        config.FlightName = "Alpha Flight";

        var arguments = Build(config, BrowserContext());

        StringAssert.Contains(arguments, "/flight \"Alpha Flight\"");
        Assert.IsFalse(arguments.Contains("/branch", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_DoesNotEmitUploadSource()
    {
        // makepkg2's /uploadsource enum only accepts 'makepkg2' and 'XGPM'; there is no value that
        // represents PackageUploader, so the flag is deliberately omitted.
        using var package = TempPackageFile.CreateMsixvc2();

        var arguments = Build(CreateConfig(package.Path), BrowserContext());

        Assert.IsFalse(arguments.Contains("/uploadsource", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Build_WithoutMarketGroup_OmitsMarketFlag()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        config.MarketGroupName = null;

        var arguments = Build(config, BrowserContext());

        Assert.IsFalse(arguments.Contains("/market", StringComparison.Ordinal));
    }

    #region Authentication

    [TestMethod]
    public void Build_WithAppSecret_MapsToClientSecretAndForwardsCredentials()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.AppSecret,
            TenantId: "tenant-1",
            ClientId: "client-1",
            ClientSecret: "secret-1");

        var arguments = Build(CreateConfig(package.Path), context);

        Assert.AreEqual(
            $"upload /pd \"{package.Directory}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" " +
            "/auth ClientSecret /tenantid \"tenant-1\" /clientid \"client-1\" /clientsecret \"secret-1\"",
            arguments);
    }

    [TestMethod]
    public void Build_WithClientSecret_ForwardsVerbatim()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.ClientSecret,
            TenantId: "tenant-1",
            ClientId: "client-1",
            ClientSecret: "secret-1");

        var arguments = Build(CreateConfig(package.Path), context);

        StringAssert.Contains(arguments, "/auth ClientSecret");
    }

    /// <summary>
    /// The whole point of the split return: the loggable command line must not contain the secret, while the
    /// executable one must still carry it verbatim.
    /// </summary>
    [TestMethod]
    public void Build_WithClientSecret_RedactedCommandLineOmitsTheSecret()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.ClientSecret,
            TenantId: "tenant-1",
            ClientId: "client-1",
            ClientSecret: "super-secret-value");

        var arguments = BuildBoth(CreateConfig(package.Path), context);

        Assert.IsFalse(
            arguments.RedactedCommandLine.Contains("super-secret-value", StringComparison.Ordinal),
            "The redacted command line must never contain the secret, since it is what gets logged.");
        StringAssert.Contains(arguments.RedactedCommandLine, "/clientsecret \"***\"");

        // The executable form is unaffected: MakePkg.exe still receives the real credential.
        StringAssert.Contains(arguments.CommandLine, "/clientsecret \"super-secret-value\"");
    }

    /// <summary>
    /// Redaction must replace only the secret. Everything else has to survive, or the logged command line
    /// stops being a faithful record of what actually ran.
    /// </summary>
    [TestMethod]
    public void Build_WithClientSecret_RedactedCommandLineMatchesApartFromTheSecret()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.ClientSecret,
            TenantId: "tenant-1",
            ClientId: "client-1",
            ClientSecret: "super-secret-value");

        var arguments = BuildBoth(CreateConfig(package.Path), context);

        Assert.AreEqual(
            arguments.CommandLine.Replace("\"super-secret-value\"", "\"***\"", StringComparison.Ordinal),
            arguments.RedactedCommandLine);
    }

    /// <summary>
    /// With no secret to hide there is nothing to diverge, so both forms stay identical.
    /// </summary>
    [TestMethod]
    public void Build_WithoutClientSecret_RedactedCommandLineIsIdentical()
    {
        using var package = TempPackageFile.CreateMsixvc2();

        var arguments = BuildBoth(CreateConfig(package.Path), BrowserContext());

        Assert.AreEqual(arguments.CommandLine, arguments.RedactedCommandLine);
    }

    [TestMethod]
    public void Build_WithAppCert_MapsToClientCertificateAndForwardsStoreDetails()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.AppCert,
            TenantId: "tenant-1",
            ClientId: "client-1",
            CertificateThumbprint: "ABC123",
            CertificateStore: "My",
            CertificateLocation: "CurrentUser");

        var arguments = Build(CreateConfig(package.Path), context);

        Assert.AreEqual(
            $"upload /pd \"{package.Directory}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" " +
            "/auth ClientCertificate /tenantid \"tenant-1\" /clientid \"client-1\" " +
            "/certthumbprint \"ABC123\" /certstore \"My\" /certlocation \"CurrentUser\"",
            arguments);
    }

    [TestMethod]
    public void Build_WithAzurePipelines_ForwardsMethodWithoutCredentials()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(IngestionExtensions.AuthenticationMethod.AzurePipelines);

        var arguments = Build(CreateConfig(package.Path), context);

        StringAssert.Contains(arguments, "/auth AzurePipelines");
        Assert.IsFalse(arguments.Contains("/clientsecret", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_WithManagedIdentityFederated_ForwardsResourceId()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.ManagedIdentityFederated,
            ClientId: "client-1",
            ResourceId: "resource-1");

        var arguments = Build(CreateConfig(package.Path), context);

        StringAssert.Contains(arguments, "/auth ManagedIdentityFederated");
        StringAssert.Contains(arguments, "/resourceid \"resource-1\"");
    }

    [TestMethod]
    public void Build_WithClientSecretButNoSecret_Throws()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.AppSecret,
            TenantId: "tenant-1",
            ClientId: "client-1");

        var exception = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(
            () => Build(CreateConfig(package.Path), context));

        StringAssert.Contains(exception.Message, "/clientsecret");
    }

    [TestMethod]
    public void Build_WithCertificateFilePath_Throws()
    {
        // makepkg2 authenticates from a certificate store only; there is no flag naming a PFX file.
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.ClientCertificate,
            TenantId: "tenant-1",
            ClientId: "client-1",
            CertificatePath: @"C:\certs\app.pfx");

        var exception = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(
            () => Build(CreateConfig(package.Path), context));

        StringAssert.Contains(exception.Message, "app.pfx");
    }

    [TestMethod]
    public void Build_WithCertificateSubject_Throws()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.AppCert,
            TenantId: "tenant-1",
            ClientId: "client-1",
            CertificateSubject: "CN=Contoso");

        var exception = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(
            () => Build(CreateConfig(package.Path), context));

        StringAssert.Contains(exception.Message, "CN=Contoso");
    }

    [TestMethod]
    public void Build_WithCertificateButNoThumbprint_Throws()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var context = new Msixvc2CommandLineContext(
            IngestionExtensions.AuthenticationMethod.AppCert,
            TenantId: "tenant-1",
            ClientId: "client-1");

        var exception = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(
            () => Build(CreateConfig(package.Path), context));

        StringAssert.Contains(exception.Message, "/certthumbprint");
    }

    #endregion

    #region Unsupported options

    /// <summary>
    /// Availability and pre-download dates are applied after the upload, by the operation, using the package
    /// identity MakePkg.exe reports. They are deliberately NOT command line arguments, so the builder must
    /// neither reject them nor try to encode them.
    /// </summary>
    [TestMethod]
    public void Build_WithAvailabilityAndPreDownloadDates_IsUnaffected()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        var expected = Build(config, BrowserContext());

        config.AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(5) };
        config.PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) };

        Assert.AreEqual(expected, Build(config, BrowserContext()));
    }

    [TestMethod]
    public void Build_WithDeltaUpload_WarnsAndContinues()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        config.DeltaUpload = true;

        var arguments = Build(config, BrowserContext());

        Assert.IsFalse(string.IsNullOrEmpty(arguments));
        _loggerMock.VerifyLogWarningContains("deltaUpload");
    }

    [TestMethod]
    public void Build_AlwaysWarnsAboutMinutesToWaitForProcessing()
    {
        using var package = TempPackageFile.CreateMsixvc2();

        Build(CreateConfig(package.Path), BrowserContext());

        _loggerMock.VerifyLogWarningContains("minutesToWaitForProcessing");
    }

    [TestMethod]
    public void Build_WithGameAssetsInPackageDirectory_WarnsAndContinues()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets
        {
            EkbFilePath = Path.Combine(package.Directory, "package.ekb"),
            SubValFilePath = Path.Combine(package.Directory, "validator.xml"),
        };

        var arguments = Build(config, BrowserContext());

        Assert.IsFalse(string.IsNullOrEmpty(arguments));
        _loggerMock.VerifyLogWarningContains("gameAssets");
    }

    [TestMethod]
    public void Build_WithGameAssetsOutsidePackageDirectory_Throws()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        var config = CreateConfig(package.Path);
        var strayPath = Path.Combine(Path.GetTempPath(), "elsewhere", "package.ekb");
        config.GameAssets = new GameAssets { EkbFilePath = strayPath };

        var exception = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(
            () => Build(config, BrowserContext()));

        StringAssert.Contains(exception.Message, "ekbFilePath");
        StringAssert.Contains(exception.Message, package.Directory);
    }

    #endregion
}

