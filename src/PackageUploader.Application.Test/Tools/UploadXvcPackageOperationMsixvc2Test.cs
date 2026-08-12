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
    private readonly Mock<IMsixvc2DelegationGuard> _delegationGuardMock = new();

    private UploadXvcPackageOperation CreateOperation(
        UploadXvcPackageOperationConfig config,
        IngestionExtensions.AuthenticationMethod authenticationMethod = IngestionExtensions.AuthenticationMethod.CacheableBrowser) =>
        new(_serviceMock.Object,
            _loggerMock.Object,
            Options.Create(config),
            _toolProviderMock.Object,
            _processRunnerMock.Object,
            _delegationGuardMock.Object,
            new Msixvc2CommandLineContext(authenticationMethod));

    private static UploadXvcPackageOperationConfig CreateConfig(string packageFilePath) => new TestUploadXvcPackageOperationConfig
    {
        OperationName = "UploadXvcPackage",
        BigId = BigId,
        BranchFriendlyName = "Main",
        MarketGroupName = "default",
        PackageFilePath = packageFilePath,
    };

    private static string ExpectedArguments(string packageFilePath) =>
        $"upload /pd \"{Path.GetDirectoryName(Path.GetFullPath(packageFilePath))}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" /auth CacheableBrowser";

    private static GameProduct CreateProduct(string productId, string bigId)
    {
        var product = (GameProduct)RuntimeHelpers.GetUninitializedObject(typeof(GameProduct));
        typeof(GameProduct).GetProperty("ProductId")!.SetValue(product, productId);
        typeof(GameProduct).GetProperty("BigId")!.SetValue(product, bigId);
        return product;
    }

    private void SetUpAvailableTool()
    {
        _toolProviderMock.SetupGet(x => x.IsAvailable).Returns(true);
        _toolProviderMock.SetupGet(x => x.ExecutablePath).Returns(ResolvedMakePkgPath);
    }

    private void SetUpSuccessfulRun() =>
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

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
    public async Task Msixvc2PackageWithoutCapability_FailsAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        _toolProviderMock.SetupGet(x => x.IsAvailable).Returns(false);
        // Exercises IMsixvc2UploadToolProvider's documented contract: ExecutablePath is null when
        // IsAvailable is false. null! (not a behavior change - the value is still null) because this
        // test project compiles with nullable reference types while the interface's project does not.
        _toolProviderMock.SetupGet(x => x.ExecutablePath).Returns((string)null!);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("no MSIXVC2-capable MakePkg.exe was found");
    }

    /// <summary>
    /// Circular-dependency guard: MakePkg.exe shells back out to PackageUploader.exe for XVC1/MSIXVC1
    /// uploads, so a non-MSIXVC2 package must NEVER be delegated to MakePkg.exe.
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

    [TestMethod]
    public async Task UnsupportedConfigOption_FailsWithExplicitErrorAndDoesNotShellOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();

        var config = CreateConfig(package.Path);
        config.AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) };

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogErrorContains("'availabilityDate' cannot be applied");
    }

    [TestMethod]
    public async Task IgnorableConfigOption_WarnsAndStillShellsOut()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();

        var config = CreateConfig(package.Path);
        config.DeltaUpload = true;

        var result = await CreateOperation(config).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(ResolvedMakePkgPath, ExpectedArguments(package.Path), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task NonZeroExitCode_FailsOperation()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        _processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _loggerMock.VerifyLogErrorContains("MakePkg.exe failed with exit code 7");
    }

    /// <summary>
    /// Loop-breaker, direction 1: a normal (non-delegated) invocation delegates as usual.
    /// </summary>
    [TestMethod]
    public async Task DelegationGuardAbsent_Delegates()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(false);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(ResolvedMakePkgPath, ExpectedArguments(package.Path), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Loop-breaker, direction 2: when this process was itself launched by MakePkg.exe, never delegate back.
    /// Format detection is a heuristic, so a false positive on an XVC1 package could otherwise produce
    /// PackageUploader.exe -> MakePkg.exe -> PackageUploader.exe recursion without bound.
    /// </summary>
    [TestMethod]
    public async Task DelegationGuardPresent_NeverShellsOutAndTakesLegacyPath()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(true);

        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _serviceMock.Verify(x => x.GetProductByBigIdAsync(BigId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DelegationGuardPresent_LogsWarning()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(true);
        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        _loggerMock.VerifyLogWarningContains(Msixvc2DelegationGuard.EnvironmentVariableName);
    }

    /// <summary>
    /// Loop-breaker, direction 3: the environment stamp only covers cycles PackageUploader.exe itself
    /// starts. When MakePkg.exe is the entry point it invokes us with no stamp, so a MakePkg.exe parent must
    /// independently suppress delegation.
    /// </summary>
    [TestMethod]
    public async Task MakePkgParentProcess_NeverShellsOutAndTakesLegacyPath()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(false);
        _delegationGuardMock.Setup(x => x.GetMakePkgParentProcessName()).Returns("MakePkg.exe");

        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(3, result);
        _processRunnerMock.VerifyNoOtherCalls();
        _serviceMock.Verify(x => x.GetProductByBigIdAsync(BigId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task MakePkgParentProcess_LogsWarningNamingTheParent()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(false);
        _delegationGuardMock.Setup(x => x.GetMakePkgParentProcessName()).Returns("makepkg2.exe");
        _serviceMock
            .Setup(x => x.GetProductByBigIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("took the normal XVC upload path"));

        await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        _loggerMock.VerifyLogWarningContains("makepkg2.exe");
    }

    /// <summary>
    /// The parent check must not become a blanket block: an ordinary parent (or an undeterminable one, which
    /// the provider also reports as null) has to leave normal MSIXVC2 delegation working.
    /// </summary>
    [TestMethod]
    public async Task NonMakePkgParentProcess_StillDelegates()
    {
        using var package = TempPackageFile.CreateMsixvc2();
        SetUpAvailableTool();
        SetUpSuccessfulRun();
        _delegationGuardMock.SetupGet(x => x.IsDelegatedInvocation).Returns(false);
        // null is the documented "parent unknown / not MakePkg" value. null! (not a behavior change) because
        // this test project compiles with nullable reference types while the interface's project does not.
        _delegationGuardMock.Setup(x => x.GetMakePkgParentProcessName()).Returns((string)null!);

        var result = await CreateOperation(CreateConfig(package.Path)).RunAsync(CancellationToken.None);

        Assert.AreEqual(0, result);
        _processRunnerMock.Verify(
            x => x.RunAsync(ResolvedMakePkgPath, ExpectedArguments(package.Path), It.IsAny<CancellationToken>()),
            Times.Once);
    }

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

        var operation = new UploadXvcPackageOperation(
            _serviceMock.Object,
            _loggerMock.Object,
            Options.Create(CreateConfig(package.Path)),
            _toolProviderMock.Object,
            _processRunnerMock.Object,
            _delegationGuardMock.Object,
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
