using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using PackageUploader.ClientApi.Tools;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using PackageUploader.ClientApi.Client.Ingestion;
using System.Threading;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class PackageUploadViewModelTest
    {
        private PackageModelProvider _packageModelProvider;
        private Mock<IWindowService> _mockWindowService;
        private Mock<IProcessStarterService> _mockProcessStarterService;
        private Mock<IPackageUploaderService> _mockPackageUploaderService;
        private UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private ErrorModelProvider _errorModelProvider;
        private PackageModel _mockPackage;

        private PackageUploadViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            // Setup mocks
            _packageModelProvider = new PackageModelProvider();
            _mockWindowService = new Mock<IWindowService>();
            _mockProcessStarterService = new Mock<IProcessStarterService>();
            _mockPackageUploaderService = new Mock<IPackageUploaderService>();
            _uploadingProgressPercentageProvider = new UploadingProgressPercentageProvider();
            _errorModelProvider = new ErrorModelProvider();
/*            _mockGamePackageConfiguration = new Mock<GamePackageConfiguration>();

            _mockPackageUploaderService.Setup(x => x.GetPackageConfigurationAsync(null,
                                                                                  It.IsAny<IGamePackageBranch>(),
                                                                                  It.IsAny<CancellationToken>()))
                                       .ReturnsAsync(_mockGamePackageConfiguration.Object);*/

            _mockPackage = _packageModelProvider.Package = new PackageModel
            {
                BigId = "12345678",
                BranchId = "branch-123",
                PackageType = "Xbox Game Package",
                PackagePreviewImage = new System.Windows.Media.Imaging.BitmapImage(),
                PackageFilePath = @"C:\test\package.msixvc",
                Version = ""
            };

            // Create view model
            _viewModel = new PackageUploadViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                new PathConfigurationProvider(),
                new Msixvc2ToolResolver()
            );
        }

        [TestMethod]
        public void Test_IsUploadInProgress()
        {
            _viewModel.IsUploadInProgress = true;
            Assert.IsTrue(_viewModel.IsUploadInProgress);
            _viewModel.IsUploadInProgress = false;
            Assert.IsFalse(_viewModel.IsUploadInProgress);
        }

        [TestMethod]
        public void Test_BranchOrFlightDisplayName()
        {
            // TODO: This is really complicated because there's a lot of subsquent calls inside of this

            // This is default version, no subsequent calls
            var viewModel2 = new PackageUploadViewModel(
                 _packageModelProvider,
                 _mockPackageUploaderService.Object,
                 _mockWindowService.Object,
                 _uploadingProgressPercentageProvider,
                 _errorModelProvider,
                new PathConfigurationProvider(),
                new Msixvc2ToolResolver()
            );
            viewModel2.BranchOrFlightDisplayName = "Test";
            Assert.AreEqual("Test", viewModel2.BranchOrFlightDisplayName);
        }

        [TestMethod]
        public void Test_HasMarketGroups()
        {
            _viewModel.HasMarketGroups = true;
            Assert.IsTrue(_viewModel.HasMarketGroups);
            _viewModel.HasMarketGroups = false;
            Assert.IsFalse(_viewModel.HasMarketGroups);
        }

        [TestMethod]
        public void Test_MarketGroupName()
        {
            _viewModel.MarketGroupName = "Test";
            Assert.AreEqual("Test", _viewModel.MarketGroupName);
        }

        [TestMethod]
        public void Test_BranchAndFlightNames()
        {
            var names = new string[] { "Test1", "Test2" };
            _viewModel.BranchAndFlightNames = names;
            Assert.AreEqual(names, _viewModel.BranchAndFlightNames);

            // secondary effect
            _viewModel.BranchOrFlightDisplayName = "Test1";

            var viewModel2 = new PackageUploadViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                new PathConfigurationProvider(),
                new Msixvc2ToolResolver()
            );

            viewModel2.BranchAndFlightNames = names; // tests the former value is successfully retrieved
            Assert.AreEqual(_viewModel.BranchOrFlightDisplayName, viewModel2.BranchOrFlightDisplayName);
        }

        [TestMethod]
        public void Test_MarketGroupNames()
        {
            var names = new string[] { "Test1", "Test2" };
            _viewModel.MarketGroupNames = names;
            Assert.AreEqual(names, _viewModel.MarketGroupNames);

            // secondary effect
            _viewModel.MarketGroupName = "Test1";

            var viewModel2 = new PackageUploadViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                new PathConfigurationProvider(),
                new Msixvc2ToolResolver()
            );

            viewModel2.MarketGroupNames = names; // tests the former value is successfully retrieved
            Assert.AreEqual(_viewModel.MarketGroupName, viewModel2.MarketGroupName);
        }

        [TestMethod]
        public void Test_UpdateMarketGroups()
        {
            // TODO: Rather complicated series of events
            // one possible trigger is: BranchOrFlightDisplayName
        }

        [TestMethod]
        public void Test_ProgressValue()
        {
            var randomInt = new Random().Next() % 100;
            _uploadingProgressPercentageProvider.UploadingProgressPercentage = randomInt;
            Assert.AreEqual(_viewModel.ProgressValue.Percentage, randomInt);

            randomInt = new Random().Next() % 100;
            _viewModel.ProgressValue = new PackageUploadingProgress()
            {
                Percentage = randomInt,
                Stage = PackageUploadingProgressStage.UploadingPackage
            };
            Assert.AreEqual(randomInt, _viewModel.ProgressValue.Percentage);
            Assert.AreEqual(PackageUploadingProgressStage.UploadingPackage, _viewModel.ProgressValue.Stage);
        }

        [TestMethod]
        public void Test_GetPackage()
        {
            Assert.AreEqual(_mockPackage, _viewModel.Package);
        }

        [TestMethod]
        public void Test_IsPackageMissingStoreId()
        {
            _viewModel.IsPackageMissingStoreId = true;
            Assert.IsTrue(_viewModel.IsPackageMissingStoreId);
            _viewModel.IsPackageMissingStoreId = false;
            Assert.IsFalse(_viewModel.IsPackageMissingStoreId);
        }

        [TestMethod]
        public void Test_BigID()
        {
            // TODO: This triggers a lot of stuff too
        }

        [TestMethod]
        public void Test_PackageName()
        {
            _packageModelProvider.Package.PackageFilePath = string.Empty;
            Assert.AreEqual(string.Empty, _viewModel.PackageName);

            _packageModelProvider.Package.PackageFilePath = @"C:\test\package.msixvc";
            Assert.AreEqual("package.msixvc", _viewModel.PackageName);
        }

        [TestMethod]
        public void Test_ProductName()
        {
            // Has to do with the internal "_gameProduct" which ... is hard to get into
        }

        [TestMethod]
        public void Test_PackageFilePath()
        {
            // Safety testing. Actual testing would require like, a real file...

            // Non Existant File
            _packageModelProvider.Package.PackageFilePath = @"C:\test\package.msixvc";
            Assert.AreEqual(@"C:\test\package.msixvc", _viewModel.PackageFilePath); // VERY WERIRD SITUATION that would require someone manipulating this program's memory...
            Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));

            // Bad file path
            _packageModelProvider.Package.PackageFilePath = @"@ ok yes: ";
            Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));

            // Setter then sets a lot of things
        }

        [TestMethod]
        public void Test_EkbFilePath()
        {
            _packageModelProvider.Package.EkbFilePath = @"C:\test\package.ekb";
            Assert.AreEqual(@"C:\test\package.ekb", _viewModel.EkbFilePath);
        }

        [TestMethod]
        public void Test_SubValFilePath()
        {
            _packageModelProvider.Package.SubValFilePath = @"C:\test\package.subval";
            Assert.AreEqual(@"C:\test\package.subval", _viewModel.SubValFilePath);
        }

        [TestMethod]
        public void Test_SymbolBundleFilePath()
        {
            _packageModelProvider.Package.SymbolBundleFilePath = @"C:\test\package.symbol";
            Assert.AreEqual(@"C:\test\package.symbol", _viewModel.SymbolBundleFilePath);
        }

        [TestMethod]
        public void Test_PackageId()
        {
            _viewModel.PackageId = "12345678";
            Assert.AreEqual("12345678", _viewModel.PackageId);
        }

        [TestMethod]
        public void Test_PackageSize()
        {
            _packageModelProvider.Package.PackageSize = "12345678";
            Assert.AreEqual("12345678", _viewModel.PackageSize);
        }

        [TestMethod]
        public void Test_PackageType()
        {
            _packageModelProvider.Package.PackageType = "Xbox Game Package";
            Assert.AreEqual("Xbox Game Package", _viewModel.PackageType);
        }

        [TestMethod]
        public void Test_IsPackageUploadEnabled()
        {
            _viewModel.IsPackageUploadEnabled = true;
            Assert.IsTrue(_viewModel.IsPackageUploadEnabled);
            _viewModel.IsPackageUploadEnabled = false;
            Assert.IsFalse(_viewModel.IsPackageUploadEnabled);
        }

        [TestMethod]
        public void Test_PackageUploadTooltip()
        {
            _viewModel.PackageUploadTooltip = "Test";
            Assert.AreEqual("Test", _viewModel.PackageUploadTooltip);
        }

        [TestMethod]
        public void Test_IsLoadingBranchesAndFlights()
        {
            _viewModel.IsLoadingBranchesAndFlights = true;
            Assert.IsTrue(_viewModel.IsLoadingBranchesAndFlights);
            _viewModel.IsLoadingBranchesAndFlights = false;
            Assert.IsFalse(_viewModel.IsLoadingBranchesAndFlights);
        }

        [TestMethod]
        public void Test_IsLoadingMarkets()
        {
            _viewModel.IsLoadingMarkets = true;
            Assert.IsTrue(_viewModel.IsLoadingMarkets);
            _viewModel.IsLoadingMarkets = false;
            Assert.IsFalse(_viewModel.IsLoadingMarkets);
        }

        [TestMethod]
        public void Test_PackageErrorMessage()
        {
            _viewModel.PackageErrorMessage = "Test";
            Assert.AreEqual("Test", _viewModel.PackageErrorMessage);
        }

        [TestMethod]
        public void Test_BranchOrFlightErrorMessage()
        {
            _viewModel.BranchOrFlightErrorMessage = "Test";
            Assert.AreEqual("Test", _viewModel.BranchOrFlightErrorMessage);
        }

        [TestMethod]
        public void Test_MarketGroupErrorMessage()
        {
            _viewModel.MarketGroupErrorMessage = "Test";
            Assert.AreEqual("Test", _viewModel.MarketGroupErrorMessage);
        }

        [TestMethod]
        public void Test_PackagePreviewImage()
        {
            _viewModel.PackagePreviewImage = new System.Windows.Media.Imaging.BitmapImage();
            Assert.IsNotNull(_viewModel.PackagePreviewImage);

            _packageModelProvider.Package.PackagePreviewImage = null;
            Assert.IsNull(_viewModel.PackagePreviewImage);

            var validImage = new System.Windows.Media.Imaging.BitmapImage();
            _packageModelProvider.Package.PackagePreviewImage = validImage;
            Assert.AreEqual(validImage, _viewModel.PackagePreviewImage);
        }

        [TestMethod]
        public void Test_UploadPackageCommand()
        {
            // TODO: This is going to be a doozy
        }

        [TestMethod]
        public void Test_BrowseForPackageCommand()
        {
            // TODO: This opens up an OpenFile Dialog. ....
        }

        [TestMethod]
        public void Test_FileDroppedCommand()
        {
            string randomPath = Path.GetTempFileName();
            _viewModel.FileDroppedCommand.Execute(randomPath);
            Assert.AreEqual(randomPath, _viewModel.PackageFilePath);

            _viewModel.FileDroppedCommand.Execute("");
            Assert.AreEqual(_viewModel.PackageErrorMessage, Resources.Strings.PackageUpload.InvalidFilePathErrMsg);

            var badPath = @"@ ok yes: ";
            _viewModel.FileDroppedCommand.Execute(badPath);
            Assert.AreEqual(_viewModel.PackageErrorMessage, Resources.Strings.PackageUpload.InvalidFilePathErrMsg);

            File.Delete(randomPath);
        }

        [TestMethod]
        public void Test_CancelUploadCommand()
        {
            // Nothing testable, everything is internal
        }

        [TestMethod]
        public void Test_CancelButtonCommand()
        {
            _viewModel.CancelButtonCommand.Execute(null);
            _mockWindowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
        }



        [TestMethod]
        public void Test_ProcessSelectedPackage()
        {
            // very important to test this for user input shenanigans
        }

        #region MSIXVC2 capability probe on package selection

        private PackageUploadViewModel CreateViewModel(IMsixvc2ToolResolver resolver) =>
            new(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                new PathConfigurationProvider(),
                resolver);

        /// <summary>
        /// Writes a file that XvcFile.IsLikelyMsixvc2Package recognises: a .msixvc at least
        /// FirstReadSize (4096) bytes long starting with the ZIP local file header.
        /// </summary>
        private static string WriteFakeMsixvc2Package()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msixvc");
            var bytes = new byte[8192];
            bytes[0] = 0x50; bytes[1] = 0x4B; bytes[2] = 0x03; bytes[3] = 0x04;
            File.WriteAllBytes(path, bytes);
            return path;
        }

        [TestMethod]
        public async Task Msixvc2Probe_DoesNotBlockPackageSelection()
        {
            // Selecting a package must not freeze the UI while the capability probe runs. The probe
            // launches a child process and can block for up to the probe timeout, twice if
            // MakePkg.exe fails and we fall back to makepkg2.exe.
            var probeStarted = new ManualResetEventSlim(false);
            var releaseProbe = new ManualResetEventSlim(false);

            var resolver = new Mock<IMsixvc2ToolResolver>();
            resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(() =>
                    {
                        probeStarted.Set();
                        releaseProbe.Wait(TimeSpan.FromSeconds(30));
                        return new Msixvc2Tool(@"C:\gdk\MakePkg.exe", IsMakePkg2Fallback: false);
                    });

            var viewModel = CreateViewModel(resolver.Object);
            string packagePath = WriteFakeMsixvc2Package();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                viewModel.PackageFilePath = packagePath;
                stopwatch.Stop();

                Assert.IsTrue(viewModel.IsMsixvc2Package, "The fake package should be detected as MSIXVC2.");
                Assert.IsTrue(probeStarted.Wait(TimeSpan.FromSeconds(10)), "The probe should have been started in the background.");
                Assert.IsTrue(
                    stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                    $"Package selection blocked for {stopwatch.Elapsed.TotalSeconds:F1}s waiting on the capability probe.");

                releaseProbe.Set();
                await viewModel.Msixvc2ProbeTask;

                Assert.AreEqual(string.Empty, viewModel.Msixvc2UnavailableMessage);
            }
            finally
            {
                releaseProbe.Set();
                File.Delete(packagePath);
            }
        }

        [TestMethod]
        public async Task Msixvc2Probe_SetsUnavailableMessageAndBlocksUpload_WhenNoToolIsResolved()
        {
            var resolver = new Mock<IMsixvc2ToolResolver>();
            resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((Msixvc2Tool)null);

            var viewModel = CreateViewModel(resolver.Object);
            string packagePath = WriteFakeMsixvc2Package();

            try
            {
                bool canExecuteRaised = false;
                viewModel.UploadPackageCommand.CanExecuteChanged += (s, e) => canExecuteRaised = true;

                viewModel.PackageFilePath = packagePath;
                await viewModel.Msixvc2ProbeTask;

                Assert.AreEqual(
                    PackageUploader.UI.Resources.Strings.MainPage.MakePkg2NotFoundErrorMsg,
                    viewModel.Msixvc2UnavailableMessage);

                // IsUploadReady() gates on the message, so the probe result must block the upload.
                //
                // Note: RelayCommand routes CanExecuteChanged through WPF's
                // CommandManager.RequerySuggested, which only fires on a running dispatcher, so the
                // event itself can't be observed in a unit test host. CanExecute is the real
                // contract, so assert on that; canExecuteRaised is left in only to document that
                // the event is deliberately not asserted here.
                _ = canExecuteRaised;
                Assert.IsFalse(viewModel.UploadPackageCommand.CanExecute(null));
            }
            finally
            {
                File.Delete(packagePath);
            }
        }

        [TestMethod]
        public void Msixvc2Probe_UploadClickedWhileProbeInFlight_ShowsErrorAndDoesNotLaunchTool()
        {
            // A user can click Upload after selecting a package but before the probe completes,
            // while Msixvc2UnavailableMessage is still empty. StartMsixvc2Upload() re-resolves
            // synchronously, so that click must produce a clean error rather than launching a
            // missing tool or crashing.
            var releaseProbe = new ManualResetEventSlim(false);
            var probeStarted = new ManualResetEventSlim(false);

            var resolver = new Mock<IMsixvc2ToolResolver>();
            resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(() =>
                    {
                        // Only the background probe blocks; the synchronous re-resolve on the upload
                        // path returns "no tool" immediately.
                        if (!probeStarted.IsSet)
                        {
                            probeStarted.Set();
                            releaseProbe.Wait(TimeSpan.FromSeconds(30));
                        }

                        return null;
                    });

            var viewModel = CreateViewModel(resolver.Object);
            string packagePath = WriteFakeMsixvc2Package();

            try
            {
                viewModel.PackageFilePath = packagePath;

                Assert.IsTrue(probeStarted.Wait(TimeSpan.FromSeconds(10)));
                Assert.IsTrue(viewModel.IsMsixvc2Package);

                // Probe still in flight: the message hasn't been published yet.
                Assert.AreEqual(string.Empty, viewModel.Msixvc2UnavailableMessage);

                // Put the view model into an otherwise upload-ready state so the command is
                // genuinely clickable, which is what makes this race reachable in the real app.
                typeof(PackageUploadViewModel)
                    .GetField("_gameProduct", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .SetValue(viewModel, new GameProduct());
                viewModel.MarketGroupName = "default";

                Assert.IsTrue(
                    viewModel.UploadPackageCommand.CanExecute(null),
                    "Upload should be clickable while the probe is still in flight - that's the race being tested.");

                // Take the click's synchronous branch directly. UploadPackageProcessAsync is
                // 'async void', so an exception from its WPF navigation (Application.Current is null
                // in a unit test host) would be rethrown on the thread pool and kill the test
                // process rather than surface here. StartMsixvc2Upload is the whole MSIXVC2 branch
                // of that method, so invoking it directly tests the same path safely.
                var startMsixvc2Upload = typeof(PackageUploadViewModel)
                    .GetMethod("StartMsixvc2Upload", BindingFlags.NonPublic | BindingFlags.Instance)!;

                try
                {
                    startMsixvc2Upload.Invoke(viewModel, null);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException)
                {
                    // SetErrorAndGoToErrorPage navigates via System.Windows.Application.Current,
                    // which doesn't exist in a unit test host. The error state it sets beforehand is
                    // still observable, and that's what matters here.
                }

                // The tool was re-resolved synchronously, came back missing, and the upload was
                // abandoned: an error was raised, no MSIXVC2 tool path was handed to the uploader,
                // and we never navigated to the uploading screen.
                Assert.AreEqual("MSIXVC2 packaging tool not found", _errorModelProvider.Error.MainMessage);
                Assert.AreEqual(
                    PackageUploader.UI.Resources.Strings.MainPage.MakePkg2NotFoundErrorMsg,
                    _errorModelProvider.Error.DetailMessage);
                _mockWindowService.Verify(x => x.NavigateTo(typeof(Msixvc2UploadingView)), Times.Never);
                Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.Msixvc2ToolPath));
            }
            finally
            {
                releaseProbe.Set();
                File.Delete(packagePath);
            }
        }

        #endregion

        #region MSIXVC2 package preview image

        // A 1x1 PNG, used so the image extracted from the package is distinguishable from the placeholder.
        private const string OnePixelPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        /// <summary>
        /// Writes a real ZIP named .msixvc, which is what an MSIXVC2 package is. A filler entry keeps
        /// it over the 4096 bytes PackageFormatDetector requires before it will sniff the file.
        /// </summary>
        private static string WriteMsixvc2Package()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msixvc");

            const string gameConfig = """
                <?xml version="1.0" encoding="utf-8"?>
                <Game configVersion="1">
                  <Identity Name="TestGame" Publisher="CN=Test" Version="1.0.0.0" />
                  <ShellVisuals DefaultDisplayName="Test Game" PublisherDisplayName="Test" Square150x150Logo="square150x150logo.png" />
                  <StoreId>9NBLGGH12345</StoreId>
                </Game>
                """;

            using (var stream = File.Create(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                using (var configStream = archive.CreateEntry("MicrosoftGame.config").Open())
                {
                    var configBytes = System.Text.Encoding.UTF8.GetBytes(gameConfig);
                    configStream.Write(configBytes, 0, configBytes.Length);
                }

                using var fillerStream = archive.CreateEntry("filler.bin", CompressionLevel.NoCompression).Open();
                var filler = new byte[8192];
                new Random(1).NextBytes(filler);
                fillerStream.Write(filler, 0, filler.Length);
            }

            return path;
        }

        /// <summary>
        /// Stands in for the GDK's packageutil.exe: answers "fileinfo" with a listing in the real
        /// tool's fixed-column format, and answers "extract" by dropping the 1x1 PNG at the path the
        /// real tool would have written it to.
        /// </summary>
        private static string WriteFakePackageUtil(bool packageContainsLogo, out string scratchDirectory)
        {
            scratchDirectory = Path.Combine(Path.GetTempPath(), "XGPMTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratchDirectory);

            string listingPath = Path.Combine(scratchDirectory, "listing.txt");
            string logoRow = packageContainsLogo
                ? "REG   Square150x150Logo.png                              18.954 KB \n"
                : string.Empty;

            // The real listing's column rule is drawn with U+2500 and defines the column boundaries,
            // so it has to be reproduced faithfully - including being written as UTF-8.
            string listing =
                "packageutil version 2610.308.24000.0\n" +
                "\n" +
                "Chunk Name                                               Size       \n" +
                "───── ─────────────────────────────────────────────────── ────────── \n" +
                "      MicrosoftGame.config                                           \n" +
                "REG   MicrosoftGame.config                                1.041 KB   \n" +
                logoRow +
                "REG   StoreLogo.png                                       4.372 KB   \n" +
                "1000  ShamWow.DirectX.GameCore.WindowsDX12.exe            1.578 MB   \n";

            File.WriteAllText(listingPath, listing.Replace("\n", "\r\n"), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            string pngPath = Path.Combine(scratchDirectory, "logo.png");
            File.WriteAllBytes(pngPath, Convert.FromBase64String(OnePixelPngBase64));

            // Argument layout mirrors the real tool: "fileinfo <package>" and
            // "extract /pd <package> /file <name> /out <directory>".
            string batPath = Path.Combine(scratchDirectory, "packageutil.bat");
            File.WriteAllText(batPath,
                "@echo off\r\n" +
                "if \"%~1\"==\"fileinfo\" (\r\n" +
                $"  type \"{listingPath}\"\r\n" +
                "  exit /b 0\r\n" +
                ")\r\n" +
                "if \"%~1\"==\"extract\" (\r\n" +
                $"  copy /y \"{pngPath}\" \"%~7\\%~5\" >nul\r\n" +
                "  exit /b 0\r\n" +
                ")\r\n" +
                "exit /b 1\r\n");

            return batPath;
        }

        private PackageUploadViewModel CreateViewModelWithResolvedTool(string packageUtilPath = "")
        {
            var resolver = new Mock<IMsixvc2ToolResolver>();
            resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(new Msixvc2Tool(@"C:\gdk\MakePkg.exe", IsMakePkg2Fallback: false));

            var pathConfiguration = new PathConfigurationProvider { PackageUtilPath = packageUtilPath };

            return new PackageUploadViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                pathConfiguration,
                resolver.Object);
        }

        [TestMethod]
        public async Task Msixvc2Package_PreviewImageIsExtractedFromThePackage()
        {
            string packageUtilPath = WriteFakePackageUtil(packageContainsLogo: true, out string scratchDirectory);
            var viewModel = CreateViewModelWithResolvedTool(packageUtilPath);
            string packagePath = WriteMsixvc2Package();

            try
            {
                viewModel.PackageFilePath = packagePath;
                await viewModel.Msixvc2PreviewImageTask;

                Assert.IsTrue(viewModel.IsMsixvc2Package, "The package should be detected as MSIXVC2.");
                Assert.IsNotNull(viewModel.PackagePreviewImage, "A preview image should always be set.");
                Assert.AreEqual(1, viewModel.PackagePreviewImage.PixelWidth, "The preview should be the 1x1 logo from the package, not the placeholder.");
                Assert.AreEqual(1, viewModel.PackagePreviewImage.PixelHeight);
            }
            finally
            {
                File.Delete(packagePath);
                Directory.Delete(scratchDirectory, recursive: true);
            }
        }

        [TestMethod]
        public async Task Msixvc2Package_MissingLogoDoesNotFailExtraction()
        {
            string packageUtilPath = WriteFakePackageUtil(packageContainsLogo: false, out string scratchDirectory);
            var viewModel = CreateViewModelWithResolvedTool(packageUtilPath);
            string packagePath = WriteMsixvc2Package();

            try
            {
                viewModel.PackageFilePath = packagePath;
                await viewModel.Msixvc2PreviewImageTask;

                Assert.IsTrue(viewModel.IsMsixvc2Package, "The package should be detected as MSIXVC2.");
                Assert.AreEqual("9NBLGGH12345", viewModel.BigId, "The rest of the package information should still be extracted.");
                Assert.IsFalse(
                    viewModel.PackageErrorMessage.Contains("Logo", StringComparison.OrdinalIgnoreCase),
                    $"A missing logo must not surface an error: {viewModel.PackageErrorMessage}");
            }
            finally
            {
                File.Delete(packagePath);
                Directory.Delete(scratchDirectory, recursive: true);
            }
        }

        [TestMethod]
        public async Task Msixvc2Package_WithoutPackageUtilFallsBackToPlaceholder()
        {
            var viewModel = CreateViewModelWithResolvedTool(packageUtilPath: string.Empty);
            string packagePath = WriteMsixvc2Package();

            try
            {
                viewModel.PackageFilePath = packagePath;
                await viewModel.Msixvc2PreviewImageTask;

                Assert.IsTrue(viewModel.IsMsixvc2Package, "The package should be detected as MSIXVC2.");
                Assert.AreEqual("9NBLGGH12345", viewModel.BigId, "The rest of the package information should still be extracted.");
                Assert.IsTrue(
                    string.IsNullOrEmpty(viewModel.PackageErrorMessage),
                    $"A missing packageutil.exe must not surface an error: {viewModel.PackageErrorMessage}");
            }
            finally
            {
                File.Delete(packagePath);
            }
        }

        #endregion

        #region BuildMsixvc2UploadArguments

        private PackageUploadViewModel CreateViewModel(PathConfigurationProvider pathConfiguration) =>
            new(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                pathConfiguration,
                new Msixvc2ToolResolver());

        /// <summary>
        /// Builds a view model whose /uploadsource probe succeeds or fails deterministically, by
        /// pointing MakePkg2Path at a batch file with the wanted exit code.
        /// </summary>
        private PackageUploadViewModel CreateViewModelWithProbeResult(bool probeSucceeds, out string tempBat)
        {
            tempBat = Path.Combine(Path.GetTempPath(), $"probe_{Guid.NewGuid():N}.bat");
            File.WriteAllText(tempBat, probeSucceeds ? "@exit /b 0" : "@exit /b 1");
            return CreateViewModel(new PathConfigurationProvider { MakePkg2Path = tempBat });
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_PassesThePackageFile_NotItsDirectory()
        {
            // MakePkg hard-fails on a directory holding more than one .msixvc/.xvc, so the file the
            // user actually picked has to be passed through.
            var viewModel = CreateViewModel(new PathConfigurationProvider());
            viewModel.PackageFilePath = @"C:\out\Game.msixvc";

            string args = viewModel.BuildMsixvc2UploadArguments();

            Assert.IsTrue(args.StartsWith("upload /pd \"C:\\out\\Game.msixvc\""), args);
            Assert.IsFalse(args.Contains("/pd \"C:\\out\""), $"/pd must not be the containing directory: {args}");
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_WithBranch()
        {
            var viewModel = CreateViewModel(new PathConfigurationProvider());
            viewModel.PackageFilePath = @"C:\out\Game.msixvc";
            viewModel.BranchOrFlightDisplayName = "Branch: Main";
            viewModel.MarketGroupName = "default";
            viewModel.BigId = "9ABC123DEF456";

            string args = viewModel.BuildMsixvc2UploadArguments();

            Assert.IsTrue(args.Contains("/branch \"Main\""), args);
            Assert.IsFalse(args.Contains("/flight"), args);
            Assert.IsTrue(args.Contains("/market \"default\""), args);
            Assert.IsTrue(args.Contains("/storeid \"9ABC123DEF456\""), args);
            Assert.IsTrue(args.Contains("/auth CacheableBrowser"), args);
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_WithFlight()
        {
            var viewModel = CreateViewModel(new PathConfigurationProvider());
            viewModel.PackageFilePath = @"C:\out\Game.msixvc";
            viewModel.BranchOrFlightDisplayName = "Flight: TestFlight";
            viewModel.MarketGroupName = "default";
            viewModel.BigId = "None";

            string args = viewModel.BuildMsixvc2UploadArguments();

            Assert.IsTrue(args.Contains("/flight \"TestFlight\""), args);
            Assert.IsFalse(args.Contains("/branch"), args);
            Assert.IsFalse(args.Contains("/storeid"), args);
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_NoStoreId_NotIncluded()
        {
            var viewModel = CreateViewModel(new PathConfigurationProvider());
            viewModel.PackageFilePath = @"C:\out\Game.msixvc";
            viewModel.BranchOrFlightDisplayName = "Branch: Dev";
            viewModel.BigId = string.Empty;

            Assert.IsFalse(viewModel.BuildMsixvc2UploadArguments().Contains("/storeid"));
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_IncludesUploadSource_WhenProbeSucceeds()
        {
            var viewModel = CreateViewModelWithProbeResult(probeSucceeds: true, out string tempBat);
            try
            {
                viewModel.PackageFilePath = @"C:\out\Game.msixvc";
                viewModel.BigId = "9ABC123DEF456";

                string args = viewModel.BuildMsixvc2UploadArguments();

                Assert.IsTrue(args.Contains($"/uploadsource {IngestionExtensions.XgpmUploadSource}"), args);
                Assert.IsTrue(
                    args.IndexOf("/storeid", StringComparison.Ordinal) < args.IndexOf("/uploadsource", StringComparison.Ordinal),
                    $"/storeid must precede /uploadsource: {args}");
                Assert.IsTrue(
                    args.IndexOf("/uploadsource", StringComparison.Ordinal) < args.IndexOf("/auth", StringComparison.Ordinal),
                    $"/uploadsource must precede /auth: {args}");
            }
            finally
            {
                File.Delete(tempBat);
            }
        }

        [TestMethod]
        public void BuildMsixvc2UploadArguments_OmitsUploadSource_WhenProbeFails()
        {
            var viewModel = CreateViewModelWithProbeResult(probeSucceeds: false, out string tempBat);
            try
            {
                viewModel.PackageFilePath = @"C:\out\Game.msixvc";
                viewModel.BigId = "9ABC123DEF456";

                string args = viewModel.BuildMsixvc2UploadArguments();

                Assert.IsFalse(args.Contains("/uploadsource"), args);
                Assert.IsTrue(args.Contains("/auth CacheableBrowser"), args);
            }
            finally
            {
                File.Delete(tempBat);
            }
        }

        #endregion
    }
}