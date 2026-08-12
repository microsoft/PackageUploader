// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.Application.Config;
using PackageUploader.Application.Test.Config;
using PackageUploader.Application.Tools;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Test.Tools;

[TestClass]
public class Msixvc2UploadArgumentBuilderTest
{
    private const string BigId = "9NBLGGH4R315";
    private static readonly string PackageFilePath = Path.Combine(Path.GetTempPath(), "packages", "game.msixvc");
    private static readonly string PackageDirectory = Path.GetDirectoryName(Path.GetFullPath(PackageFilePath))!;

    private static readonly Msixvc2CommandLineContext CacheableBrowserContext =
        new(IngestionExtensions.AuthenticationMethod.CacheableBrowser, TenantId: null);

    private readonly Mock<ILogger> _loggerMock = new();

    private static UploadXvcPackageOperationConfig CreateConfig() => new TestUploadXvcPackageOperationConfig
    {
        OperationName = "UploadXvcPackage",
        BigId = BigId,
        BranchFriendlyName = "Main",
        MarketGroupName = "default",
        PackageFilePath = PackageFilePath,
    };

    private string Build(UploadXvcPackageOperationConfig config, Msixvc2CommandLineContext? context = null, bool supportsUploadSource = false) =>
        Msixvc2UploadArgumentBuilder.Build(config, context ?? CacheableBrowserContext, BigId, supportsUploadSource, _loggerMock.Object);

    [TestMethod]
    public void Build_BranchConfig_ProducesExpectedArguments()
    {
        var args = Build(CreateConfig());

        Assert.AreEqual(
            $"upload /pd \"{PackageDirectory}\" /branch \"Main\" /market \"default\" /storeid \"{BigId}\" /auth CacheableBrowser",
            args);
    }

    [TestMethod]
    public void Build_FlightConfigWithUploadSource_ProducesExpectedArguments()
    {
        var config = CreateConfig();
        config.BranchFriendlyName = null;
        config.FlightName = "PreviewFlight";
        config.MarketGroupName = "NorthAmerica";

        var args = Build(config, supportsUploadSource: true);

        Assert.AreEqual(
            $"upload /pd \"{PackageDirectory}\" /flight \"PreviewFlight\" /market \"NorthAmerica\" /storeid \"{BigId}\" /uploadsource PackageUploader /auth CacheableBrowser",
            args);
    }

    /// <summary>
    /// /msixvc2 belongs to the pack-from-content-folder form (Msixvc2UploadViewModel, which uses /d).
    /// The already-built-package form used by the CLI mirrors PackageUploadViewModel and must not emit it.
    /// </summary>
    [TestMethod]
    public void Build_DoesNotEmitMsixvc2FlagForAlreadyBuiltPackage() =>
        Assert.DoesNotContain("/msixvc2", Build(CreateConfig()));

    [TestMethod]
    public void Build_ResolvedBigIdIsUsedInsteadOfConfigProductId()
    {
        var config = CreateConfig();
        config.BigId = null;
        config.ProductId = "00000000-0000-0000-0000-000000000001";

        var args = Msixvc2UploadArgumentBuilder.Build(config, CacheableBrowserContext, "9RESOLVED123", supportsUploadSource: false, _loggerMock.Object);

        StringAssert.Contains(args, "/storeid \"9RESOLVED123\"");
    }

    [TestMethod]
    public void Build_GameAssetsInPackageDirectory_WarnsAndContinues()
    {
        var config = CreateConfig();
        config.GameAssets = new GameAssets
        {
            EkbFilePath = Path.Combine(PackageDirectory, "game.ekb"),
            SubValFilePath = Path.Combine(PackageDirectory, "validator.xml"),
        };

        var args = Build(config);

        StringAssert.Contains(args, "/storeid");
        _loggerMock.VerifyLogWarningContains("'gameAssets' is not used for MSIXVC2 uploads");
    }

    [TestMethod]
    public void Build_GameAssetsOutsidePackageDirectory_Throws()
    {
        var strayPath = Path.Combine(Path.GetTempPath(), "elsewhere", "game.ekb");
        var config = CreateConfig();
        config.GameAssets = new GameAssets
        {
            EkbFilePath = strayPath,
            SubValFilePath = Path.Combine(PackageDirectory, "validator.xml"),
        };

        var ex = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(() => Build(config));

        StringAssert.Contains(ex.Message, "gameAssets.ekbFilePath");
        StringAssert.Contains(ex.Message, strayPath);
        StringAssert.Contains(ex.Message, PackageDirectory);
    }

    [TestMethod]
    public void Build_MinutesToWaitForProcessing_WarnsAndContinues()
    {
        var config = CreateConfig();
        config.MinutesToWaitForProcessing = 60;

        var args = Build(config);

        StringAssert.Contains(args, "/storeid");
        _loggerMock.VerifyLogWarningContains("'minutesToWaitForProcessing' is not used for MSIXVC2 uploads");
    }

    [TestMethod]
    public void Build_DeltaUpload_WarnsAndContinues()
    {
        var config = CreateConfig();
        config.DeltaUpload = true;

        var args = Build(config);

        StringAssert.Contains(args, "/storeid");
        _loggerMock.VerifyLogWarningContains("'deltaUpload' is not used for MSIXVC2 uploads");
    }

    [TestMethod]
    public void Build_AvailabilityDate_Throws()
    {
        var config = CreateConfig();
        config.AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) };

        var ex = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(() => Build(config));

        StringAssert.Contains(ex.Message, "'availabilityDate' cannot be applied");
    }

    [TestMethod]
    public void Build_DisabledAvailabilityDate_DoesNotThrow()
    {
        var config = CreateConfig();
        config.AvailabilityDate = new GamePackageDate { IsEnabled = false };

        StringAssert.Contains(Build(config), "/storeid");
    }

    [TestMethod]
    public void Build_PreDownloadDate_Throws()
    {
        var config = CreateConfig();
        config.PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) };

        var ex = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(() => Build(config));

        StringAssert.Contains(ex.Message, "'preDownloadDate' cannot be applied");
    }

    [TestMethod]
    public void Build_TenantId_Throws()
    {
        var ex = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(() => Build(
            CreateConfig(),
            new Msixvc2CommandLineContext(IngestionExtensions.AuthenticationMethod.CacheableBrowser, "contoso.onmicrosoft.com")));

        StringAssert.Contains(ex.Message, "TenantId");
    }

    [TestMethod]
    [DataRow(IngestionExtensions.AuthenticationMethod.AppSecret)]
    [DataRow(IngestionExtensions.AuthenticationMethod.Browser)]
    [DataRow(IngestionExtensions.AuthenticationMethod.AzureCli)]
    public void Build_NonCacheableBrowserAuthentication_Throws(IngestionExtensions.AuthenticationMethod method)
    {
        var ex = Assert.ThrowsExactly<Msixvc2UnsupportedOptionException>(() => Build(
            CreateConfig(),
            new Msixvc2CommandLineContext(method, TenantId: null)));

        StringAssert.Contains(ex.Message, method.ToString());
    }
}
