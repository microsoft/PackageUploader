using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class UploadingFinishedViewModelTest
    {
        private Mock<IWindowService> _mockWindowService;
        private PackageModelProvider _packageModelProvider;
        private PathConfigurationProvider _pathConfigurationService;
        private Mock<ILogger<PackagingFinishedViewModel>> _mockLogger;
        private Mock<IProcessStarterService> _mockProcessStarterService;
        private PackageModel _mockPackage;
        private UploadingFinishedViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            // Setup mocks
            _mockWindowService = new Mock<IWindowService>();
            _packageModelProvider = new PackageModelProvider();
            _pathConfigurationService = new PathConfigurationProvider();
            _mockLogger = new Mock<ILogger<PackagingFinishedViewModel>>();
            _mockProcessStarterService = new Mock<IProcessStarterService>();
            
            // Setup mock package
            _mockPackage = new PackageModel
            {
                PackageFilePath = Path.GetTempFileName(),
                Version = "1.0.0.0",
                BigId = "12345678",
                PackageType = "Xbox Game Package",
                BranchId = "branch-123",
                PackagePreviewImage = new BitmapImage()
            };
            
            _packageModelProvider.Package = _mockPackage;
            
            
            // Setup App.GetLogFilePath for log tests
            typeof(App).GetField("_logFilePath", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, @"C:\test\logs\app.log");
            
            // Create view model
            _viewModel = new UploadingFinishedViewModel(
                _mockWindowService.Object,
                _packageModelProvider,
                _pathConfigurationService,
                _mockLogger.Object,
                _mockProcessStarterService.Object);
        }

        [TestMethod]
        public void Constructor_InitializesCommands()
        {
            // Assert
            Assert.IsNotNull(_viewModel.HomeCommand);
            Assert.IsNotNull(_viewModel.ViewLogsCommand);
            Assert.IsNotNull(_viewModel.ViewInPartnerCenterCommand);
        }

        [TestMethod]
        public void OnAppearing_LoadsPackageInformation()
        {
            // Arrange
            var packagePath = _mockPackage.PackageFilePath;
            var fileInfo = new FileInfo(packagePath);
            var mockFileLength = 1024 * 1024 * 10; // 10 MB
            
            // Use reflection to set private field values if necessary for file info
            SetFileInfoLength(fileInfo, mockFileLength);
            
            // Act
            _viewModel.OnAppearing();
            
            // Assert
            Assert.AreEqual(_mockPackage.PackagePreviewImage, _viewModel.PackagePreviewImage);
            Assert.AreEqual(_mockPackage.Version, _viewModel.VersionNum);
            Assert.AreEqual(_mockPackage.BigId, _viewModel.StoreId);
            Assert.AreEqual(Path.GetFileName(packagePath), _viewModel.PackageFileName);
            Assert.AreEqual("0 B", _viewModel.PackageSize); // temp file is empty
            Assert.AreEqual(_mockPackage.PackageType, _viewModel.PackageType);
        }

        [TestMethod]
        public void OnHome_NavigatesToMainPageView()
        {
            // Act
            _viewModel.OnHome();

            // Assert - verify navigation to the main page
            _mockWindowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
        }

        [TestMethod]
        public void OnViewLogs_OpensExplorerWithLogPath()
        {
            // Arrange
            string logPath = App.GetLogFilePath();
            var mockProcess = new Mock<Process>();
            _mockProcessStarterService
                .Setup(p => p.Start("explorer.exe", $"/select, \"{logPath}\""))
                .Returns(mockProcess.Object);
            
            // Act
            _viewModel.OnViewLogs();
            
            // Assert
            _mockProcessStarterService.Verify(
                p => p.Start("explorer.exe", $"/select, \"{logPath}\""),
                Times.Once);
        }

        [TestMethod]
        public void OnViewInPartnerCenter_OpensBrowserWithCorrectUrl()
        {
            // Arrange
            string storeId = _mockPackage.BigId;
            string branchId = _mockPackage.BranchId;
            string expectedUrl = $"https://partner.microsoft.com/en-us/dashboard/products/{storeId}/packages/{branchId}";
                
            var mockProcess = new Mock<Process>();
            _mockProcessStarterService
                .Setup(p => p.Start(It.Is<ProcessStartInfo>(
                    psi => psi.FileName.Equals(expectedUrl) && psi.UseShellExecute)))
                .Returns(mockProcess.Object);

            _viewModel.StoreId = storeId;

            // Act
            _viewModel.OnViewInPartnerCenter();
            
            // Assert
            _mockProcessStarterService.Verify(
                p => p.Start(It.Is<ProcessStartInfo>(
                    psi => psi.FileName.Equals(expectedUrl) && psi.UseShellExecute)),
                Times.Once);
        }

        [TestMethod]
        public void TranslateFileSize_FormatsCorrectly()
        {
            // Arrange & Act - Use reflection to call private static method
            string bytesResult = InvokePrivateStaticMethod<string>(
                typeof(UploadingFinishedViewModel), "TranslateFileSize", 500L);
                
            string kbResult = InvokePrivateStaticMethod<string>(
                typeof(UploadingFinishedViewModel), "TranslateFileSize", 1500L);
                
            string mbResult = InvokePrivateStaticMethod<string>(
                typeof(UploadingFinishedViewModel), "TranslateFileSize", 1500 * 1024L);
                
            string gbResult = InvokePrivateStaticMethod<string>(
                typeof(UploadingFinishedViewModel), "TranslateFileSize", 1500 * 1024 * 1024L);

            // Assert
            Assert.AreEqual("500 B", bytesResult);
            Assert.AreEqual("1 KB", kbResult);
            Assert.AreEqual("1 MB", mbResult);
            Assert.AreEqual("1.46 GB", gbResult);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Delete temp file
            if (File.Exists(_mockPackage.PackageFilePath))
            {
                File.Delete(_mockPackage.PackageFilePath);
            }
        }

        #region Helper Methods

        private static void SetStaticProperty(Type type, string propertyName, object value)
        {
            var propertyInfo = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
                
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(null, value);
            }
        }

        private static void SetFileInfoLength(FileInfo fileInfo, long length)
        {
            // This is a hack for testing purposes since FileInfo length is read-only
            // In real tests, you'd use a file system abstraction instead
            var fieldInfo = typeof(FileInfo).GetField(
                "length", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(fileInfo, length);
            }
        }

        private static T InvokePrivateStaticMethod<T>(Type type, string methodName, params object[] parameters)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
                
            return (T)method?.Invoke(null, parameters);
        }

        #endregion
    }
}