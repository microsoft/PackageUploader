using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Test.Model;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class PackagingFinishedViewModelTest
    {
        private Mock<IWindowService> _mockWindowService;
        private PackageModelProvider _packageModelProvider;
        private PathConfigurationProvider _pathConfigurationService;
        private ValidatorResultsProvider _validatorResultsProvider;
        private Mock<IAuthenticationService> _mockAuthenticationService;
        private Mock<IProcessStarterService> _mockProcessStarterService;
        private Mock<ILogger<PackagingFinishedViewModel>> _mockLogger;
        private PackageModel _mockPackage;
        private PackagingFinishedViewModel _viewModel;
        private PartialGameConfigModelTest _partialGameConfigModelTest;

        [TestInitialize]
        public void Setup()
        {
            // Setup mocks
            _mockWindowService = new Mock<IWindowService>();
            _packageModelProvider = new PackageModelProvider();
            _pathConfigurationService = new ();
            _validatorResultsProvider = new ValidatorResultsProvider();
            _mockAuthenticationService = new Mock<IAuthenticationService>();
            _mockProcessStarterService = new Mock<IProcessStarterService>();
            _mockLogger = new Mock<ILogger<PackagingFinishedViewModel>>();
            _partialGameConfigModelTest = new PartialGameConfigModelTest();
            _partialGameConfigModelTest.Setup();

            _mockPackage = new PackageModel
            {
                PackageFilePath = @"C:\test\package.msixvc",
                GameConfigFilePath = _partialGameConfigModelTest.getGoodConfigPath(),
            };
            
            _packageModelProvider.Package = _mockPackage;
            _packageModelProvider.PackagingLogFilepath = @"C:\test\logs\packaging.log";
            
            // Setup path for WdApp.exe
            string wdAppDirectoryPath = @"C:\test\tools";
            _pathConfigurationService.MakePkgPath = Path.Combine(wdAppDirectoryPath, "MakePkg.exe");

            // Create the view model with mocked dependencies
            _viewModel = new PackagingFinishedViewModel(
                _mockWindowService.Object,
                _packageModelProvider,
                _pathConfigurationService,
                _mockAuthenticationService.Object,
                _mockLogger.Object,
                _mockProcessStarterService.Object,
                _validatorResultsProvider);
        }

        [TestMethod]
        public void Constructor_InitializesCommands()
        {
            // Assert
            Assert.IsNotNull(_viewModel.InstallGameCommand);
            Assert.IsNotNull(_viewModel.ViewPackageCommand);
            Assert.IsNotNull(_viewModel.ConfigureUploadCommand);
            Assert.IsNotNull(_viewModel.ViewLogsCommand);
            Assert.IsNotNull(_viewModel.GoHomeCommand);
        }

        [TestMethod]
        public void ViewPackage_StartsExplorerProcess()
        {
            // Arrange
            var mockProcess = new Mock<Process>();
            _mockProcessStarterService.Setup(p => p.Start("explorer.exe", $"/select, \"{_mockPackage.PackageFilePath}\""))
                .Returns(mockProcess.Object);

            // Act
            _viewModel.ViewPackage();

            // Assert
            _mockProcessStarterService.Verify(p => p.Start("explorer.exe", $"/select, \"{_mockPackage.PackageFilePath}\""), Times.Once);
        }

        [TestMethod]
        public void ViewLogs_StartsExplorerProcess()
        {
            // Arrange
            var logPath = _packageModelProvider.PackagingLogFilepath;
            var mockProcess = new Mock<Process>();
            _mockProcessStarterService.Setup(p => p.Start("explorer.exe", $"/select, \"{logPath}\""))
                .Returns(mockProcess.Object);

            // Act
            InvokePrivateMethod(_viewModel, "ViewLogs");

            // Assert
            _mockProcessStarterService.Verify(p => p.Start("explorer.exe", $"/select, \"{logPath}\""), Times.Once);
        }

        [TestMethod]
        public void CanInstallGame_ReturnsFalse_WhenWdAppDoesNotExist()
        {
            // Act
            bool result = (bool)InvokePrivateMethod(_viewModel, "CanInstallGame");

            // Assert - WdApp.exe doesn't exist in our test setup
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void InstallGame_CreatesBatchFileAndStartsProcess()
        {

            // Arrange
            string tempPath = Path.GetTempFileName();

            var mockProcess = new Mock<Process>();
            _mockProcessStarterService.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
                .Returns(mockProcess.Object);

            _packageModelProvider.Package.PackageFilePath = tempPath;

            // Act
            _viewModel.InstallGame();

            // Assert
            _mockProcessStarterService.Verify(p => p.Start(It.Is<ProcessStartInfo>(info => 
                info.FileName.Equals("cmd.exe") && 
                info.Arguments.StartsWith("/c") && info.Arguments.Contains("InstallGame.bat") &&
                info.UseShellExecute.Equals(true))), Times.Once);

            // Cleanup
            File.Delete(tempPath);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void InstallGame_CreatesBatchFileAndStartsProcess_SafetyTest()
        {
            // Arrange
            string wdAppPath = Path.Combine(Path.GetDirectoryName(_pathConfigurationService.MakePkgPath), "WdApp.exe");
            string batchFilePath = Path.Combine(Path.GetTempPath(), "install_game.bat");

            var mockProcess = new Mock<Process>();
            _mockProcessStarterService.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
                .Returns(mockProcess.Object);

            // Act
            _viewModel.InstallGame();
        }

        [TestMethod]
        public void GoHome_NavigatesToMainPage()
        {
            // Arrange - Setup a mock application dispatcher

            // Act
            _viewModel.GoHome();

            // Assert
            _mockWindowService.Verify(w => w.NavigateTo(typeof(MainPageView)), Times.Once);
        }

        [TestMethod]
        public void ConfigureUpload_NavigatesToUploadView_WhenUserIsLoggedIn()
        {
            // Arrange
            _mockAuthenticationService.Setup(a => a.IsUserLoggedIn).Returns(true);

            // Act
            _viewModel.ConfigureUpload();

            // Assert
            Assert.IsFalse(_viewModel.IsSigningIn);
            _mockWindowService.Verify(w => w.NavigateTo(typeof(PackageUploadView)), Times.Once);
        }

        [TestMethod]
        public void ConfigureUpload_AttemptsSignIn_WhenUserIsNotLoggedIn()
        {
            // Arrange
            _mockAuthenticationService.Setup(a => a.IsUserLoggedIn).Returns(false);
            _mockAuthenticationService.Setup(a => a.SignInAsync()).ReturnsAsync(true);

            // Act
            _viewModel.ConfigureUpload();

            // Assert
            _mockAuthenticationService.Verify(a => a.SignInAsync(), Times.Once);
            Assert.IsFalse(_viewModel.IsSigningIn);
            _mockWindowService.Verify(w => w.NavigateTo(typeof(PackageUploadView)), Times.Once);
        }

        [TestMethod]
        public void ConfigureUpload_DoesNotNavigate_WhenSignInFails()
        {
            // Arrange
            _mockAuthenticationService.Setup(a => a.IsUserLoggedIn).Returns(false);
            _mockAuthenticationService.Setup(a => a.SignInAsync()).ReturnsAsync(false);

            // Act
            _viewModel.ConfigureUpload();

            // Assert
            _mockAuthenticationService.Verify(a => a.SignInAsync(), Times.Once);
            Assert.IsFalse(_viewModel.IsSigningIn);
            _mockWindowService.Verify(w => w.NavigateTo(It.IsAny<Type>()), Times.Never);
        }

        [TestMethod]
        public void TranslateFileSize_ReturnsCorrectFormat()
        {
            // Arrange & Act
            string bytesResult = InvokePrivateStaticMethod<string>(typeof(PackagingFinishedViewModel), "TranslateFileSize", 500L);
            string kbResult = InvokePrivateStaticMethod<string>(typeof(PackagingFinishedViewModel), "TranslateFileSize", 1500L);
            string mbResult = InvokePrivateStaticMethod<string>(typeof(PackagingFinishedViewModel), "TranslateFileSize", 1500 * 1024L);
            string gbResult = InvokePrivateStaticMethod<string>(typeof(PackagingFinishedViewModel), "TranslateFileSize", 1500 * 1024 * 1024L);

            // Assert
            Assert.AreEqual("500 B", bytesResult);
            Assert.AreEqual("1 KB", kbResult);
            Assert.AreEqual("1 MB", mbResult);
            Assert.AreEqual("1.46 GB", gbResult);
        }

        //[TestMethod]
        public void OnAppearing_LoadsPackageInformation()
        {
            // Arrange - Setup a mock game config model
            // For this to work in a test, we'd need to intercept the PartialGameConfigModel creation
            // or make it injectable for testing

            // Mocking a file info for package
            var packageInfo = new FileInfo(_mockPackage.PackageFilePath);
            var img = new BitmapImage();

            // TODO: Using reflection to set private fields for testing
            SetPrivateField(_viewModel, "_gameConfigModel", new PartialGameConfigModel(_mockPackage.GameConfigFilePath)
            {
                // Need to figure out how to get a real image in there...
                Identity = new Identity { Version = "1.0.0.0" },
                StoreId = "12345",
                ShellVisuals = new ShellVisuals { Square150x150Logo = img.UriSource.ToString() }
            });
            
            // Act
            _viewModel.OnAppearing();
            
            // Assert
            Assert.AreEqual("1.0.0.0", _viewModel.VersionNum);
            Assert.AreEqual("12345", _viewModel.StoreId);
            Assert.IsNotNull(_viewModel.PackagePreviewImage);
            Assert.AreEqual(packageInfo.Name, _viewModel.PackageFileName);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #region Helper Methods

        private object InvokePrivateMethod(object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Instance);
            return method?.Invoke(obj, parameters);
        }

        private static T InvokePrivateStaticMethod<T>(Type type, string methodName, params object[] parameters)
        {
            var method = type.GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Static);
            return (T)method?.Invoke(null, parameters);
        }

        #endregion
    }
}