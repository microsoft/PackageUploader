// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;
using PackageUploader.Application.Test.Config;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using System.Runtime.CompilerServices;

namespace PackageUploader.Application.Test.Tools;

[TestClass]
public class UploadXvcPackageOperationMsixvc2Test
{
    private const string BigId = "9NBLGGH4R315";
    private const string ResolvedMakePkgPath = @"C:\GDK\bin\MakePkg.exe";

    private readonly Mock<IPackageUploaderService> _serviceMock = new();
    private readonly Mock<ILogger<UploadXvcPackageOperation>> _loggerMock = new();
    private readonly Mock<IMsixvc2UploadToolProvider> _toolProviderMock = new();
    private readonly Mock<IMsixvc2ProcessRunner> _processRunnerMock = new();
    private readonly Mock<IMakePkgFeatureProbe> _featureProbeMock = new();

    private UploadXvcPackageOperation CreateOperation(
        UploadXvcPackageOperationConfig config,
        IngestionExtensions.AuthenticationMethod authenticationMethod = IngestionExtensions.AuthenticationMethod.CacheableBrowser) =>
        CreateOperation(config, new Msixvc2CommandLineContext(authenticationMethod));

    private UploadXvcPackageOperation CreateOperation(
        UploadXvcPackageOperationConfig config,
        Msixvc2CommandLineContext commandLineContext) =>
        new(_serviceMock.Object,
            _loggerMock.Object,
            Options.Create(config),
            _toolProviderMock.Object,
            _processRunnerMock.Object,
            _featureProbeMock.Object,
            commandLineContext);

    private static UploadXvcPackageOperationConfig CreateConfig(string packageFilePath) => new TestUploadXvcPackageOperationConfig
    {
        OperationName = "UploadXvcPackage",
        BigId = BigId,
        BranchFriendlyName = "Main",
        MarketGroupName = "default",
        PackageFilePath = packageFilePath,
    };

    private static string ExpectedArguments(string packageFilePath, string extra = "") =>
        $"upload /pd \"{Path.GetFullPath(packageFilePath)}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" /auth CacheableBrowser{extra}";

    private static GameProduct CreateProduct(string productId, string bigId)
    {
        var product = (GameProduct)RuntimeHelpers.GetUninitializedObject(typeof(GameProduct));
        typeof(GameProduct).GetProperty("ProductId")!.SetValue(product, productId);
        typeof(GameProduct).GetProperty("BigId")!.SetValue(product, bigId);
        return product;
    }

    /// <summary>
    /// The happy-path environment: a resolved MakePkg.exe that advertises the capability gate.
    /// </summary>
    private void SetUpAvailableTool()
    {
        _toolProviderMock.SetupGet(x => x.IsAvailable).Returns(true);
        _toolProviderMock.SetupGet(x => x.ExecutablePath).Returns(ResolvedMakePkgPath);
        _featureProbeMock
            .Setup(x => x.SupportsAsync(It.IsAny<string>(), MakePkgFeatures.Xvc1Upload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private string _capturedArguments = null!;

    private void SetUpSuccessfulRun() =>
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, arguments, _) => _capturedArguments = arguments)
            .ReturnsAsync(new Msixvc2ProcessResult(0));

    private string CapturedArguments()
    {
        Assert.IsNotNull(_capturedArguments, "MakePkg.exe was never launched.");
        return _capturedArguments;
    }

    [TestMethod]
    public async Task Msixvc2PackageWithCapability_ShellsOutWithExpectedArguments()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(ResolvedMakePkgPath, ExpectedArguments(package.Path), It.IsAny<CancellationToken>()),
            Times.Once);
        _serviceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Msixvc2PackageWithProductIdOnly_ResolvesBigIdThroughIngestion()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.BigId = null;
        config.ProductId = "1234567890";

        _serviceMock
            .Setup(x => x.GetProductByProductIdAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct("1234567890", BigId));

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(ResolvedMakePkgPath, ExpectedArguments(package.Path), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Msixvc2PackageWithUnresolvableProductId_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var config = CreateConfig(package.Path);
        config.BigId = null;
        config.ProductId = "1234567890";

        _serviceMock
            .Setup(x => x.GetProductByProductIdAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct("1234567890", bigId: null!));

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("Could not resolve a Big ID");
    }

    [TestMethod]
    public async Task Msixvc2PackageWithoutTool_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        _toolProviderMock.SetupGet(x => x.IsAvailable).Returns(false);
        // Exercises IMsixvc2UploadToolProvider's documented contract: ExecutablePath is null when
        // IsAvailable is false.
        _toolProviderMock.SetupGet(x => x.ExecutablePath).Returns((string)null!);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("no MSIXVC2-capable MakePkg.exe was found");
    }

    #region Capability probe

    /// <summary>
    /// The capability gate doubles as the loop-breaker. MakePkg.exe releases that predate the
    /// "xvc1upload" capability performed XVC1 uploads by shelling back out to PackageUploader.exe, so
    /// launching one risks an unbounded MakePkg.exe/PackageUploader.exe cycle. A tool that fails the probe
    /// must never be launched at all.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2PackageWithToolLackingCapability_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        _toolProviderMock.SetupGet(x => x.IsAvailable).Returns(true);
        _toolProviderMock.SetupGet(x => x.ExecutablePath).Returns(ResolvedMakePkgPath);
        _featureProbeMock
            .Setup(x => x.SupportsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("too old to perform the upload");
        _loggerMock.VerifyLogErrorContains(MakePkgFeatures.Xvc1Upload);
    }

    /// <summary>
    /// The probe must be run against the resolved executable, not some independently discovered path —
    /// otherwise the capability answer could describe a different binary than the one that is launched.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2Package_ProbesTheResolvedExecutable()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        _featureProbeMock.Verify(
            x => x.SupportsAsync(ResolvedMakePkgPath, MakePkgFeatures.Xvc1Upload, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// The probe launches a process, so it must never run for a package that will not be delegated.
    /// </summary>
    [TestMethod]
    public async Task NonMsixvc2Package_NeverProbes()
    {
        using var package = TempPackageFile.CreateLegacyXvc();
        SetUpAvailableTool();
        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        _featureProbeMock.Verify(
            x => x.SupportsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Loose game content

    /// <summary>
    /// The MSIXVC2 pack-and-upload flow belongs to MakePkg.exe end to end. PackageUploader has no packaging
    /// step, so loose content must be refused outright — and refused with a message that says why, rather
    /// than the "package file not found" the upload layer would otherwise produce.
    /// </summary>
    [TestMethod]
    public async Task LooseGameContentDirectory_FailsAndNeitherProbesNorShellsOut()
    {
        using var content = TempLooseContent.Create();

        SetUpAvailableTool();

        var result = await CreateOperation(CreateConfig(content.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _featureProbeMock.VerifyNoOtherCalls();
        _serviceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("loose game content");
        _loggerMock.VerifyLogErrorContains("MicrosoftGame.config");
    }

    /// <summary>
    /// Pointing straight at the MicrosoftGame.config is the other natural way to ask for the loose flow,
    /// and it is refused for the same reason.
    /// </summary>
    [TestMethod]
    public async Task LooseGameContentConfigFile_Fails()
    {
        using var content = TempLooseContent.Create();

        SetUpAvailableTool();

        var result = await CreateOperation(CreateConfig(content.GameConfigPath)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("loose game content");
    }

    #endregion

    /// <summary>
    /// Format guard: only a package positively identified as MSIXVC2 is ever handed to MakePkg.exe.
    /// </summary>
    [TestMethod]
    public async Task NonMsixvc2Package_NeverShellsOut()
    {
        using var package = TempPackageFile.CreateLegacyXvc();
        SetUpAvailableTool();
        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _serviceMock.Verify(x => x.GetProductByBigIdAsync(BigId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The legacy XVC path must be byte-for-byte identical: the same ingestion calls in the same order,
    /// with the same arguments, and no MakePkg.exe involvement.
    /// </summary>
    [TestMethod]
    public async Task NonMsixvc2Package_TakesUnchangedLegacyUploadPath()
    {
        using var package = TempPackageFile.CreateLegacyXvc();
        SetUpAvailableTool();

        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets { EkbFilePath = "ekb", SubValFilePath = "sub" };

        var product = CreateProduct("1234567890", BigId);
        var branch = (GamePackageBranch)RuntimeHelpers.GetUninitializedObject(typeof(GamePackageBranch));
        var marketGroupPackage = (GameMarketGroupPackage)RuntimeHelpers.GetUninitializedObject(typeof(GameMarketGroupPackage));
        typeof(GameMarketGroupPackage).GetProperty("Name")!.SetValue(marketGroupPackage, "default");
        var packageConfiguration = (GamePackageConfiguration)RuntimeHelpers.GetUninitializedObject(typeof(GamePackageConfiguration));
        typeof(GamePackageConfiguration).GetProperty("MarketGroupPackages")!.SetValue(packageConfiguration, new List<GameMarketGroupPackage> { marketGroupPackage });
        var gamePackage = (GamePackage)RuntimeHelpers.GetUninitializedObject(typeof(GamePackage));

        _serviceMock.Setup(x => x.GetProductByBigIdAsync(BigId, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _serviceMock.Setup(x => x.GetPackageBranchByFriendlyNameAsync(product, "Main", It.IsAny<CancellationToken>())).ReturnsAsync(branch);
        _serviceMock.Setup(x => x.GetPackageConfigurationAsync(product, branch, It.IsAny<CancellationToken>())).ReturnsAsync(packageConfiguration);
        _serviceMock
            .Setup(x => x.UploadGamePackageAsync(product, branch, marketGroupPackage, package.Path, config.GameAssets, 30, false, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gamePackage);

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _serviceMock.Verify(
            x => x.UploadGamePackageAsync(product, branch, marketGroupPackage, package.Path, config.GameAssets, 30, false, true, It.IsAny<CancellationToken>()),
            Times.Once);
        _serviceMock.Verify(
            x => x.SetXvcConfigurationAsync(It.IsAny<GameProduct>(), It.IsAny<IGamePackageBranch>(), It.IsAny<GamePackage>(), It.IsAny<string>(), It.IsAny<IXvcGameConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task MissingPackageFile_NeverShellsOut()
    {
        SetUpAvailableTool();
        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        var result = await CreateOperation(CreateConfig(@"C:\does\not\exist.msixvc")).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
    }

    #region Options with no MakePkg.exe equivalent

    /// <summary>
    /// A config option with no MakePkg.exe equivalent must fail fast rather than be silently dropped.
    /// Disc layout is the case in point: MakePkg.exe rejects /disclayout for every non-XVC1 format and has
    /// no MSIXVC2 disc-layout asset upload, so accepting it would produce a "successful" upload that is
    /// missing an asset the caller asked for.
    /// </summary>
    [TestMethod]
    public async Task UnsupportedConfigOption_FailsWithExplicitErrorAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets
        {
            DiscLayoutFilePath = Path.Combine(Path.GetDirectoryName(package.Path)!, "layout.txt"),
        };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("does not upload a disc layout file for MSIXVC2 packages");
    }

    /// <summary>
    /// MakePkg.exe discovers co-located assets itself, so an asset pointing somewhere else would not be
    /// uploaded. That has to be an error rather than a silent drop.
    /// </summary>
    [TestMethod]
    public async Task GameAssetOutsidePackageDirectory_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets { EkbFilePath = @"C:\elsewhere\game.ekb" };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("outside the package directory");
    }

    /// <summary>
    /// An asset that already sits next to the package is discovered by MakePkg.exe, so the upload proceeds
    /// — with a warning saying the path itself is not forwarded.
    /// </summary>
    [TestMethod]
    public async Task GameAssetInsidePackageDirectory_WarnsAndStillShellsOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets
        {
            EkbFilePath = Path.Combine(Path.GetDirectoryName(package.Path)!, "game.ekb"),
        };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(ExpectedArguments(package.Path), CapturedArguments());
        _loggerMock.VerifyLogWarningContains("are not forwarded to MakePkg.exe");
    }

    #endregion

    #region Forwarded package options

    /// <summary>
    /// MSIXVC2 never re-uploads unchanged content, so there is nothing for a delta flag to ask for and
    /// MakePkg.exe decides what to transfer. The request must not reach the command line, and must not be
    /// silently swallowed either.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithDeltaUpload_WarnsAndDoesNotForwardAnyDeltaFlag()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.DeltaUpload = true;

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(ExpectedArguments(package.Path), CapturedArguments());
        _loggerMock.VerifyLogWarningContains("'deltaUpload' is not passed to MakePkg.exe");
    }

    /// <summary>
    /// SODB has a real MakePkg.exe flag, so unlike the co-located assets its path is forwarded verbatim
    /// from wherever the caller put it.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithSodbAsset_ForwardsSodbPath()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.GameAssets = new GameAssets { SodbFilePath = @"C:\elsewhere\game.sodb" };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(ExpectedArguments(package.Path, " /sodb \"C:\\elsewhere\\game.sodb\""), CapturedArguments());
    }

    #endregion

    #region Availability and pre-download dates

    /// <summary>
    /// MakePkg.exe applies the schedule itself, so the dates go over the command line and PackageUploader
    /// makes no ingestion call at all.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithAvailabilityDate_ForwardsDateAndDoesNotTouchIngestion()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.AvailabilityDate = new GamePackageDate
        {
            IsEnabled = true,
            EffectiveDate = new DateTime(2030, 5, 6, 7, 8, 9, DateTimeKind.Utc),
        };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(
            ExpectedArguments(package.Path, " /availabilitydate \"2030-05-06T07:00:00.0000000Z\""),
            CapturedArguments());
        _serviceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Msixvc2WithPreDownloadDate_ForwardsBothDatesInOrder()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.AvailabilityDate = new GamePackageDate
        {
            IsEnabled = true,
            EffectiveDate = new DateTime(2030, 5, 6, 7, 8, 9, DateTimeKind.Utc),
        };
        config.PreDownloadDate = new GamePackageDate
        {
            IsEnabled = true,
            EffectiveDate = new DateTime(2030, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(
            ExpectedArguments(
                package.Path,
                " /availabilitydate \"2030-05-06T07:00:00.0000000Z\" /predownloaddate \"2030-05-01T00:00:00.0000000Z\""),
            CapturedArguments());
    }

    /// <summary>
    /// A disabled date is not the same as an absent one: it means "clear whatever is set". MakePkg.exe's
    /// dedicated clear flags are what make that expressible, and the XVC1 path behaves the same way.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithDisabledDates_ForwardsClearFlags()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.AvailabilityDate = new GamePackageDate { IsEnabled = false };
        config.PreDownloadDate = new GamePackageDate { IsEnabled = false };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(
            ExpectedArguments(package.Path, " /clearavailabilitydate /clearpredownloaddate"),
            CapturedArguments());
    }

    [TestMethod]
    public async Task Msixvc2WithoutDates_ForwardsNoDateFlags()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.AreEqual(ExpectedArguments(package.Path), CapturedArguments());
    }

    /// <summary>
    /// <see cref="GamePackageDate"/> normalizes on assignment: it converts to UTC and truncates to the hour.
    /// The XVC1 path already sends that normalized value to Partner Center, so forwarding the same value to
    /// MakePkg.exe verbatim is what keeps the two paths agreeing on the instant. This test pins the
    /// normalization, because a change to it would silently move release dates on both paths.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithLocalKindDate_ForwardsTheModelNormalizedUtcValue()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var local = new DateTime(2030, 5, 6, 7, 8, 9, DateTimeKind.Local);

        var config = CreateConfig(package.Path);
        config.AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = local };

        await CreateOperation(config).RunAsync(CancellationToken.None);

        var utc = local.ToUniversalTime();
        var expectedValue = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc)
            .ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(
            ExpectedArguments(package.Path, $" /availabilitydate \"{expectedValue}\""),
            CapturedArguments());
    }

    #endregion

    #region Authentication

    /// <summary>
    /// End-to-end proof of the CodeQL finding's underlying concern: with client-secret authentication the
    /// secret must reach MakePkg.exe but must never appear in any log entry.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithClientSecret_PassesSecretToProcessButNeverLogsIt()
    {
        const string secret = "super-secret-value";

        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientSecret,
                TenantId: "tenant-1",
                ClientId: "client-1",
                ClientSecret: secret));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);

        // The child process gets the real credential...
        _processRunnerMock.Verify(
            x => x.RunAsync(
                ResolvedMakePkgPath,
                It.Is<string>(a => a.Contains($"/clientsecret \"{secret}\"", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // ...and the logger never does, at any level.
        _loggerMock.VerifyNeverLogged(secret);
    }

    [TestMethod]
    public async Task Msixvc2WithCertificatePath_ForwardsCertPath()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1",
                CertificatePath: @"C:\certs\upload.pfx"));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(
                ResolvedMakePkgPath,
                "upload /pd \"" + Path.GetFullPath(package.Path) + "\" /branch \"Main\" /market \"default\" " +
                $"/storeid \"{BigId}\" /auth ClientCertificate /tenantid \"tenant-1\" /clientid \"client-1\" /certpath \"C:\\certs\\upload.pfx\"",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// A password-protected PFX must still reach MakePkg.exe, and the password must never reach a log —
    /// the same rule as a client secret.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithCertificatePassword_PassesPasswordToProcessButNeverLogsIt()
    {
        const string password = "pfx-pass-phrase";

        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1",
                CertificatePath: @"C:\certs\upload.pfx",
                CertificatePassword: password));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        Assert.IsTrue(CapturedArguments().Contains($"/certpassword \"{password}\"", StringComparison.Ordinal));
        _loggerMock.VerifyNeverLogged(password);
    }

    /// <summary>
    /// /certstore and /certlocation narrow a store lookup, so they belong with a subject or thumbprint
    /// selector and would be meaningless alongside a certificate file.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithCertificateSubject_ForwardsSubjectAndStoreModifiers()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1",
                CertificateSubject: "CN=Contoso",
                CertificateStore: "My",
                CertificateLocation: "CurrentUser"));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(
                ResolvedMakePkgPath,
                It.Is<string>(a => a.EndsWith(
                    "/auth ClientCertificate /tenantid \"tenant-1\" /clientid \"client-1\" /certsubject \"CN=Contoso\" /certstore \"My\" /certlocation \"CurrentUser\"",
                    StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Msixvc2WithCertificatePathAndStoreModifiers_OmitsStoreModifiers()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1",
                CertificatePath: @"C:\certs\upload.pfx",
                CertificateStore: "My",
                CertificateLocation: "CurrentUser"));

        await operation.RunAsync(CancellationToken.None);

        var arguments = CapturedArguments();
        Assert.IsFalse(arguments.Contains("/certstore", StringComparison.Ordinal), arguments);
        Assert.IsFalse(arguments.Contains("/certlocation", StringComparison.Ordinal), arguments);
    }

    /// <summary>
    /// MakePkg.exe accepts exactly one certificate selector and fails the whole upload when given more.
    /// Catching that here means the error names PackageUploader's own config keys and arrives before any
    /// work starts.
    /// </summary>
    [TestMethod]
    public async Task Msixvc2WithMultipleCertificateSelectors_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1",
                CertificatePath: @"C:\certs\upload.pfx",
                CertificateThumbprint: "ABCDEF"));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("accepts only one certificate selector");
    }

    [TestMethod]
    public async Task Msixvc2WithNoCertificateSelector_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var operation = CreateOperation(
            CreateConfig(package.Path),
            new Msixvc2CommandLineContext(
                IngestionExtensions.AuthenticationMethod.ClientCertificate,
                TenantId: "tenant-1",
                ClientId: "client-1"));

        var result = await operation.RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("requires a certificate path, thumbprint, or subject");
    }

    #endregion

    [TestMethod]
    public async Task NonZeroExitCode_FailsOperation()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Msixvc2ProcessResult(7));

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _loggerMock.VerifyLogErrorContains("MakePkg.exe failed with exit code 7");
    }

    [TestMethod]
    public async Task Cancellation_PropagatesTokenAndFailsOperation()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        using var cts = new CancellationTokenSource();
        SetUpAvailableTool();

        var observedToken = CancellationToken.None;
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, _, ct) => observedToken = ct)
            .ThrowsAsync(new OperationCanceledException());

        await cts.CancelAsync();

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(cts.Token);

        Assert.AreEqual(1, result);
        Assert.IsTrue(observedToken.IsCancellationRequested, "The operation cancellation token must be handed to the process runner.");
    }
}
