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
                BigId = "12345678",
                PackageType = "PC",
                BranchId = "branch-123",
                PackagePreviewImage = new BitmapImage(),
                PackageName = "Test Game",
                Destination = "Branch: main",
                Market = "default",
                PackageIdentityName = "Publisher.TestGame",
                FolderSize = "1.04 GB",
                UploadSize = "1.001 GB"
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
        public void OnAppearing_LoadsAllPreviewFields()
        {
            // Act
            _viewModel.OnAppearing();
            
            // Assert
            Assert.AreEqual(_mockPackage.PackagePreviewImage, _viewModel.PackagePreviewImage);
            Assert.AreEqual("Test Game", _viewModel.ProductName);
            Assert.AreEqual("Branch: main", _viewModel.Destination);
            Assert.AreEqual("default", _viewModel.Market);
            Assert.AreEqual("Publisher.TestGame", _viewModel.PackageIdentityName);
            Assert.AreEqual("12345678", _viewModel.StoreId);
            Assert.AreEqual("1.04 GB", _viewModel.FolderSize);
            Assert.AreEqual("PC", _viewModel.PackageType);
            Assert.IsTrue(_viewModel.HasUploadSize);
            Assert.AreEqual("1.001 GB", _viewModel.UploadSize);
        }

        [TestMethod]
        public void OnAppearing_WithoutUploadSize_HidesUploadSize()
        {
            // Arrange
            _mockPackage.UploadSize = string.Empty;

            // Act
            _viewModel.OnAppearing();

            // Assert
            Assert.IsFalse(_viewModel.HasUploadSize);
        }

        [TestMethod]
        public void OnAppearing_WithoutPackageName_FallsBackToFileName()
        {
            // Arrange - legacy path with file but no package name
            string tempFile = Path.GetTempFileName();
            try
            {
                _mockPackage.PackageName = string.Empty;
                _mockPackage.PackageFilePath = tempFile;

                // Act
                _viewModel.OnAppearing();

                // Assert
                Assert.AreEqual(Path.GetFileName(tempFile), _viewModel.ProductName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void OnAppearing_WithNoNameOrFile_ShowsFallbackText()
        {
            // Arrange
            _mockPackage.PackageName = string.Empty;
            _mockPackage.PackageFilePath = string.Empty;

            // Act
            _viewModel.OnAppearing();

            // Assert
            Assert.AreEqual("Loose content upload", _viewModel.ProductName);
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

        [TestCleanup]
        public void Cleanup()
        {
        }
    }
}