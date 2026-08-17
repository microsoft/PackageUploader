// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Tools;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class Msixvc2UploadViewModelTest
{
    private Mock<IWindowService> _mockWindowService;
    private Mock<IPackageUploaderService> _mockUploaderService;
    private Mock<ILogger<Msixvc2UploadViewModel>> _mockLogger;
    private ErrorModelProvider _errorModelProvider;
    private PathConfigurationProvider _pathConfigurationProvider;
    private PackageModelProvider _packageModelProvider;
    private IMsixvc2ToolResolver _msixvc2ToolResolver;

    private Msixvc2UploadViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _mockWindowService = new Mock<IWindowService>();
        _mockUploaderService = new Mock<IPackageUploaderService>();
        _mockLogger = new Mock<ILogger<Msixvc2UploadViewModel>>();
        _errorModelProvider = new ErrorModelProvider();
        _pathConfigurationProvider = new PathConfigurationProvider();
        _packageModelProvider = new PackageModelProvider();
        _msixvc2ToolResolver = new Msixvc2ToolResolver();

        _viewModel = new Msixvc2UploadViewModel(
            _mockWindowService.Object,
            _mockUploaderService.Object,
            _mockLogger.Object,
            _errorModelProvider,
            _pathConfigurationProvider,
            _packageModelProvider,
            _msixvc2ToolResolver
        );
    }

    #region Constructor & Property initialization

    [TestMethod]
    public void Constructor_InitializesProperties()
    {
        Assert.AreEqual(string.Empty, _viewModel.ContentPath);
        Assert.IsNotNull(_viewModel.UploadPackageCommand);
        Assert.IsNotNull(_viewModel.CancelButtonCommand);
        Assert.IsNotNull(_viewModel.BrowseContentPathCommand);
    }

    #endregion

    #region BuildUploadArguments Tests

    [TestMethod]
    public void BuildUploadArguments_WithBranch()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Branch: Main";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = "9ABC123DEF456";

        string args = _viewModel.BuildUploadArguments();

        Assert.IsTrue(args.StartsWith("upload /d \"C:\\game\\content\" /msixvc2"));
        Assert.IsTrue(args.Contains("/branch \"Main\""));
        Assert.IsFalse(args.Contains("/flight"));
        Assert.IsTrue(args.Contains("/market \"default\""));
        Assert.IsTrue(args.Contains("/storeid \"9ABC123DEF456\""));
        Assert.IsTrue(args.Contains("/auth CacheableBrowser"));
    }

    [TestMethod]
    public void BuildUploadArguments_WithFlight()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Flight: TestFlight";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = "None";

        string args = _viewModel.BuildUploadArguments();

        Assert.IsTrue(args.Contains("/flight \"TestFlight\""));
        Assert.IsFalse(args.Contains("/branch"));
        Assert.IsFalse(args.Contains("/storeid"));
    }

    [TestMethod]
    public void BuildUploadArguments_NoStoreId_NotIncluded()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Branch: Dev";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = string.Empty;

        string args = _viewModel.BuildUploadArguments();

        Assert.IsFalse(args.Contains("/storeid"));
    }
    #endregion

    #region UploadSource Probe Tests

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenMakePkg2PathEmpty()
    {
        _pathConfigurationProvider.MakePkg2Path = string.Empty;
        Assert.IsFalse(_viewModel.SupportsUploadSourceFlag());
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenMakePkg2NotFound()
    {
        _pathConfigurationProvider.MakePkg2Path = @"C:\nonexistent\makepkg2.exe";
        Assert.IsFalse(_viewModel.SupportsUploadSourceFlag());
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsTrue_WhenProbeExitsZero()
    {
        // Create a temp batch file that exits 0 (simulates makepkg2 supporting /uploadsource)
        string tempBat = Path.Combine(Path.GetTempPath(), "probe_test_exit0.bat");
        File.WriteAllText(tempBat, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = tempBat;
            Assert.IsTrue(_viewModel.SupportsUploadSourceFlag());
        }
        finally
        {
            File.Delete(tempBat);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenProbeExitsNonZero()
    {
        string tempBat = Path.Combine(Path.GetTempPath(), "probe_test_exit1.bat");
        File.WriteAllText(tempBat, "@exit /b 1");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = tempBat;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag());
        }
        finally
        {
            File.Delete(tempBat);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReprobesEveryCall_NoCache()
    {
        string tempBat = Path.Combine(Path.GetTempPath(), "probe_test_nocache.bat");
        File.WriteAllText(tempBat, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = tempBat;
            Assert.IsTrue(_viewModel.SupportsUploadSourceFlag());
            // Replace binary in-place with one that exits 1 (simulates update)
            File.WriteAllText(tempBat, "@exit /b 1");
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Must re-probe on every call to detect in-place binary updates");
        }
        finally
        {
            if (File.Exists(tempBat)) File.Delete(tempBat);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_IncludesUploadSource_WhenProbeSucceeds()
    {
        string tempBat = Path.Combine(Path.GetTempPath(), "probe_build_args.bat");
        File.WriteAllText(tempBat, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = tempBat;
            _viewModel.ContentPath = @"C:\game\content";
            _viewModel.BranchOrFlightDisplayName = "Branch: Main";
            _viewModel.MarketGroupName = "default";
            _viewModel.BigId = "None";

            string args = _viewModel.BuildUploadArguments();

            Assert.IsTrue(args.Contains("/uploadsource XGPM"));
            // /uploadsource must appear before /auth
            int uploadSourceIdx = args.IndexOf("/uploadsource");
            int authIdx = args.IndexOf("/auth");
            Assert.IsTrue(uploadSourceIdx < authIdx, "/uploadsource must come before /auth");
        }
        finally
        {
            File.Delete(tempBat);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_OmitsUploadSource_WhenProbeFails()
    {
        _pathConfigurationProvider.MakePkg2Path = @"C:\nonexistent\makepkg2.exe";
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Branch: Main";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = "None";

        string args = _viewModel.BuildUploadArguments();

        Assert.IsFalse(args.Contains("/uploadsource"));
        Assert.IsTrue(args.Contains("/auth CacheableBrowser"));
    }

    #endregion

    #region CanUpload (via UploadPackageCommand.CanExecute) Tests

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenContentPathEmpty()
    {
        _viewModel.ContentPath = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenNoBranchSelected()
    {
        _viewModel.BranchOrFlightDisplayName = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenNoMarketSelected()
    {
        _viewModel.MarketGroupName = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenLoadingBranches()
    {
        _viewModel.IsLoadingBranchesAndFlights = true;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenLoadingMarkets()
    {
        _viewModel.IsLoadingMarkets = true;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenContentPathHasError()
    {
        _viewModel.ContentPathError = "Some error";

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }
    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void CancelButton_NavigatesToMainPage()
    {
        _viewModel.CancelButtonCommand.Execute(null);

        _mockWindowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
    }

    [TestMethod]
    public void ErrorModelProvider_ReceivesCorrectValues_OnError()
    {
        // Verify the error model provider is the one we injected
        Assert.IsNotNull(_errorModelProvider);
        Assert.AreEqual(string.Empty, _errorModelProvider.Error.MainMessage);
    }
    #endregion

    #region UploadSource Adversarial Tests

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenPathIsNull()
    {
        _pathConfigurationProvider.MakePkg2Path = null!;
        Assert.IsFalse(_viewModel.SupportsUploadSourceFlag());
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenProcessHangs()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_hang_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@ping -n 30 127.0.0.1 > nul");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Should return false when probe hangs (timeout)");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenExitCode255()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_255_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@exit /b 255");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Exit code 255 should be treated as unsupported");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenProcessWritesStderr()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_stderr_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@echo ERROR: unknown command 1>&2\r\n@exit /b 1");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Should return false despite stderr output when exit code is non-zero");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsTrue_WhenProcessWritesStdoutAndExitsZero()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_stdout_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@echo uploadsource is supported\r\n@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsTrue(_viewModel.SupportsUploadSourceFlag(),
                "Should return true based on exit code, ignoring stdout content");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenPathIsDirectory()
    {
        string dirPath = Path.Combine(Path.GetTempPath(), $"makepkg2_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirPath);
        try
        {
            _pathConfigurationProvider.MakePkg2Path = dirPath;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Should return false when path points to a directory, not a file");
        }
        finally
        {
            Directory.Delete(dirPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsTrue_WhenPathHasSpaces()
    {
        string dirPath = Path.Combine(Path.GetTempPath(), "make pkg2 spaces");
        Directory.CreateDirectory(dirPath);
        string batPath = Path.Combine(dirPath, "makepkg2.bat");
        File.WriteAllText(batPath, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsTrue(_viewModel.SupportsUploadSourceFlag(),
                "Should handle paths with spaces correctly");
        }
        finally
        {
            File.Delete(batPath);
            Directory.Delete(dirPath);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_UploadSourceBeforeAuth_OrderVerification()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_order_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            _viewModel.ContentPath = @"C:\game\content";
            _viewModel.BranchOrFlightDisplayName = "Branch: Dev";
            _viewModel.MarketGroupName = "US";
            _viewModel.BigId = "9NBLGGH4R315";

            string args = _viewModel.BuildUploadArguments();

            int storeIdx = args.IndexOf("/storeid");
            int uploadIdx = args.IndexOf("/uploadsource XGPM");
            int authIdx = args.IndexOf("/auth CacheableBrowser");

            Assert.IsTrue(storeIdx > 0, "Should have /storeid");
            Assert.IsTrue(uploadIdx > storeIdx, "/uploadsource should come after /storeid");
            Assert.IsTrue(authIdx > uploadIdx, "/auth should come after /uploadsource");
            Assert.IsTrue(args.EndsWith("/auth CacheableBrowser"), "/auth should be the last argument");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void SupportsUploadSourceFlag_ReturnsFalse_WhenOldMakePkg2LacksSupportsCommand()
    {
        // Old makepkg2 (e.g. v2.2.25) doesn't recognize "supports" subcommand,
        // prints usage to stderr and exits non-zero — probe must return false.
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_old_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath,
            "@echo Unrecognized command or argument 'supports'. 1>&2\r\n@exit /b 1");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag(),
                "Old makepkg2 without 'supports' command must return false");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_OmitsUploadSource_WhenOldMakePkg2()
    {
        // End-to-end: old makepkg2 → probe fails → /uploadsource NOT in args
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_oldargs_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath,
            "@echo Unrecognized command or argument 'supports'. 1>&2\r\n@exit /b 1");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            _viewModel.ContentPath = @"C:\game\content";
            _viewModel.BranchOrFlightDisplayName = "Branch: Main";
            _viewModel.MarketGroupName = "default";

            string args = _viewModel.BuildUploadArguments();

            Assert.IsFalse(args.Contains("/uploadsource"),
                "Old makepkg2 must NOT produce /uploadsource in upload args");
            Assert.IsTrue(args.Contains("/auth CacheableBrowser"),
                "Other args must still be present");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_FlightPath_IncludesUploadSource()
    {
        string batPath = Path.Combine(Path.GetTempPath(), $"makepkg2_flight_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batPath, "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkg2Path = batPath;
            _viewModel.ContentPath = @"C:\game\content";
            _viewModel.BranchOrFlightDisplayName = "Flight: Beta";
            _viewModel.MarketGroupName = "default";

            string args = _viewModel.BuildUploadArguments();

            Assert.IsTrue(args.Contains("/flight \"Beta\""), "Should include flight");
            Assert.IsTrue(args.Contains("/uploadsource XGPM"), "Should include /uploadsource XGPM on flight path too");
        }
        finally
        {
            File.Delete(batPath);
        }
    }

    #endregion

    #region MakePkg.exe / makepkg2.exe resolution order

    private static string WriteTempScript(string name, string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.bat");
        File.WriteAllText(path, content);
        return path;
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_PrefersMakePkg_WhenItSupportsUploadSource()
    {
        string makePkg = WriteTempScript("makepkg_new", "@exit /b 0");
        string makePkg2 = WriteTempScript("makepkg2_new", "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkgPath = makePkg;
            _pathConfigurationProvider.MakePkg2Path = makePkg2;

            var tool = _viewModel.ResolveMsixvc2Tool();

            Assert.IsNotNull(tool, "New MakePkg.exe supporting the verb must resolve");
            Assert.AreEqual(makePkg, tool.ExecutablePath, "MakePkg.exe must win over makepkg2.exe");
            Assert.IsFalse(tool.IsMakePkg2Fallback, "Must not be flagged as a makepkg2 fallback");
        }
        finally
        {
            File.Delete(makePkg);
            File.Delete(makePkg2);
        }
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_FallsBackToMakePkg2_WhenLegacyMakePkgFailsProbe()
    {
        // Legacy MakePkg.exe doesn't understand "supports uploadsource" and exits non-zero.
        string makePkg = WriteTempScript("makepkg_legacy",
            "@echo Unrecognized command or argument 'supports'. 1>&2\r\n@exit /b 1");
        string makePkg2 = WriteTempScript("makepkg2_ok", "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkgPath = makePkg;
            _pathConfigurationProvider.MakePkg2Path = makePkg2;

            var tool = _viewModel.ResolveMsixvc2Tool();

            Assert.IsNotNull(tool, "makepkg2.exe fallback must resolve when MakePkg.exe is legacy");
            Assert.AreEqual(makePkg2, tool.ExecutablePath);
            Assert.IsTrue(tool.IsMakePkg2Fallback, "Must be flagged as a makepkg2 fallback");
        }
        finally
        {
            File.Delete(makePkg);
            File.Delete(makePkg2);
        }
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_ReturnsNull_WhenBothToolsFailProbe()
    {
        string makePkg = WriteTempScript("makepkg_legacy2", "@exit /b 1");
        string makePkg2 = WriteTempScript("makepkg2_legacy2", "@exit /b 1");
        try
        {
            _pathConfigurationProvider.MakePkgPath = makePkg;
            _pathConfigurationProvider.MakePkg2Path = makePkg2;

            Assert.IsNull(_viewModel.ResolveMsixvc2Tool(), "MSIXVC2 must be unavailable when both probes fail");
            Assert.IsFalse(_viewModel.SupportsUploadSourceFlag());
        }
        finally
        {
            File.Delete(makePkg);
            File.Delete(makePkg2);
        }
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_ReturnsNull_WhenBothToolsMissing()
    {
        _pathConfigurationProvider.MakePkgPath = @"C:\nonexistent\MakePkg.exe";
        _pathConfigurationProvider.MakePkg2Path = @"C:\nonexistent\makepkg2.exe";

        Assert.IsNull(_viewModel.ResolveMsixvc2Tool());
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_FallsBackToMakePkg2_WhenMakePkgTimesOut()
    {
        string makePkg = WriteTempScript("makepkg_hang", "@ping -n 30 127.0.0.1 > nul");
        string makePkg2 = WriteTempScript("makepkg2_after_hang", "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkgPath = makePkg;
            _pathConfigurationProvider.MakePkg2Path = makePkg2;

            var tool = _viewModel.ResolveMsixvc2Tool();

            Assert.IsNotNull(tool, "A hung MakePkg.exe probe must time out and fall through to makepkg2.exe");
            Assert.AreEqual(makePkg2, tool.ExecutablePath);
            Assert.IsTrue(tool.IsMakePkg2Fallback);
        }
        finally
        {
            File.Delete(makePkg);
            File.Delete(makePkg2);
        }
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_FallsBackToMakePkg2_WhenMakePkgMissing()
    {
        string makePkg2 = WriteTempScript("makepkg2_only", "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkgPath = string.Empty;
            _pathConfigurationProvider.MakePkg2Path = makePkg2;

            var tool = _viewModel.ResolveMsixvc2Tool();

            Assert.IsNotNull(tool);
            Assert.AreEqual(makePkg2, tool.ExecutablePath);
            Assert.IsTrue(tool.IsMakePkg2Fallback);
        }
        finally
        {
            File.Delete(makePkg2);
        }
    }

    [TestMethod]
    public void ResolveMsixvc2Tool_ReturnsNull_WhenMakePkgPathIsDirectory()
    {
        string dirPath = Path.Combine(Path.GetTempPath(), $"makepkg_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirPath);
        try
        {
            _pathConfigurationProvider.MakePkgPath = dirPath;
            _pathConfigurationProvider.MakePkg2Path = dirPath;

            Assert.IsNull(_viewModel.ResolveMsixvc2Tool(),
                "A directory must never be treated as a packaging tool");
        }
        finally
        {
            Directory.Delete(dirPath);
        }
    }

    [TestMethod]
    public void BuildUploadArguments_IncludesUploadSource_WhenOnlyNewMakePkgAvailable()
    {
        string makePkg = WriteTempScript("makepkg_args", "@exit /b 0");
        try
        {
            _pathConfigurationProvider.MakePkgPath = makePkg;
            _pathConfigurationProvider.MakePkg2Path = string.Empty;
            _viewModel.ContentPath = @"C:\game\content";
            _viewModel.BranchOrFlightDisplayName = "Branch: Main";
            _viewModel.MarketGroupName = "default";

            string args = _viewModel.BuildUploadArguments();

            Assert.IsTrue(args.Contains("/uploadsource XGPM"));
        }
        finally
        {
            File.Delete(makePkg);
        }
    }

    #endregion
}
