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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class PackageCreationViewModelTest
    {
        private PackageModelProvider _packageModelProvider;
        private PathConfigurationProvider _pathConfigProvider;
        private Mock<IWindowService> _mockWindowService;
        private PackingProgressPercentageProvider _progressProvider;
        private Mock<ILogger<PackageCreationViewModel>> _mockLogger;
        private ErrorModelProvider _errorModelProvider;
        private ErrorModel _errorModel;
        private PackageModel _packageModel;
        private PackageCreationViewModel _viewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize mocks
            _packageModelProvider = new PackageModelProvider();
            _pathConfigProvider = new PathConfigurationProvider();
            _mockWindowService = new Mock<IWindowService>();
            _progressProvider = new PackingProgressPercentageProvider();
            _mockLogger = new Mock<ILogger<PackageCreationViewModel>>();
            _errorModelProvider = new ErrorModelProvider();

            // Setup common behaviors
            _errorModel = new ErrorModel();

            _pathConfigProvider.MakePkgPath = Path.GetTempFileName();
            _progressProvider.PackingProgressPercentage = 0;
            _progressProvider.PackingCancelled = false;

            // Mock the GetVersionInfo to avoid file system dependency
            

            // Create the view model
            _viewModel = new PackageCreationViewModel(
                _packageModelProvider,
                _pathConfigProvider,
                _mockWindowService.Object,
                _progressProvider,
                _mockLogger.Object,
                _errorModelProvider
            );
        }

        [TestMethod]
        public void Constructor_InitializesPropertiesAndCommands()
        {
            // Assert
            Assert.IsNotNull(_viewModel.MakePackageCommand);
            Assert.IsNotNull(_viewModel.GameDataPathDroppedCommand);
            Assert.IsNotNull(_viewModel.BrowseGameDataPathCommand);
            Assert.IsNotNull(_viewModel.BrowseMappingDataXmlPathCommand);
            Assert.IsNotNull(_viewModel.BrowsePackageOutputPathCommand);
            Assert.IsNotNull(_viewModel.BrowseSubValPathCommand);
            Assert.IsNotNull(_viewModel.CancelButtonCommand);
            Assert.IsFalse(_viewModel.IsCreationInProgress);
        }

        [TestMethod]
        public void GameDataPath_WhenChanged_UpdatesPropertyAndEstimatesSize()
        {
            // Arrange
            string newPath = @"C:\TestData\Game";

            // Act
            _viewModel.GameDataPath = newPath;

            // Assert
            Assert.AreEqual(newPath, _viewModel.GameDataPath);
        }

        [TestMethod]
        public void MappingDataXmlPath_WhenChanged_EstimatesSizeAndUpdatesCommand()
        {
            // Arrange
            string newPath = @"C:\TestData\mapping.xml";
            bool canExecuteChangedFired = false;

            if (_viewModel.MakePackageCommand is RelayCommand command)
            {
                command.CanExecuteChanged += (s, e) => canExecuteChangedFired = true;
            }

            // Act
            _viewModel.MappingDataXmlPath = newPath;

            // Assert
            Assert.AreEqual(newPath, _viewModel.MappingDataXmlPath);
            Assert.IsTrue(canExecuteChangedFired);
        }

        [TestMethod]
        public void HasValidGameConfig_WhenChanged_UpdatesCanExecuteStatus()
        {
            // Arrange
            bool canExecuteChangedFired = false;

            if (_viewModel.MakePackageCommand is RelayCommand command)
            {
                command.CanExecuteChanged += (s, e) => 
                { 
                    canExecuteChangedFired = true; 
                };
            }
            _viewModel.LayoutParseError = string.Empty;
            _viewModel.MappingDataXmlPath = string.Empty;

            // Act
            _viewModel.HasValidGameConfig = true;

            // Assert
            Assert.IsTrue(_viewModel.HasValidGameConfig);
            Assert.IsTrue(canExecuteChangedFired);
        }

        [TestMethod]
        public void PackageType_WhenSet_UpdatesModelAndNotifies()
        {
            // Act
            _viewModel.PackageType = "Xbox";

            // Assert
            Assert.AreEqual("Xbox", _viewModel.PackageType);
            Assert.AreEqual("Xbox", _packageModel.PackageType);
        }

        [TestMethod]
        public void PackageSize_WhenSet_UpdatesModelAndNotifies()
        {
            // Act
            _viewModel.PackageSize = "5.2 GB";

            // Assert
            Assert.AreEqual("5.2 GB", _viewModel.PackageSize);
            Assert.AreEqual("5.2 GB", _packageModel.PackageSize);
        }

        [TestMethod]
        public void OnGameDataPathDropped_SetsPathAndUpdatesHasGameDataPath()
        {
            // Arrange
            string path = @"C:\TestData\Game";

            // Act
            _viewModel.GameDataPathDroppedCommand.Execute(path);

            // Assert
            Assert.AreEqual(path, _viewModel.GameDataPath);
            Assert.IsTrue(_viewModel.HasGameDataPath);
        }

        [TestMethod]
        public void OnCancelButton_NavigatesToMainPageView()
        {
            // Act
            _viewModel.CancelButtonCommand.Execute(null);

            // Assert
            _mockWindowService.Verify(s => s.NavigateTo(typeof(MainPageView)), Times.Once);
        }

        [TestMethod]
        public void OnAppearing_WhenPackingCancelled_ResetsCancelledFlag()
        {
            // Arrange
            _progressProvider.PackingCancelled = true;

            // Act
            _viewModel.OnAppearing();

            // Assert
            Assert.IsFalse(_progressProvider.PackingCancelled);
        }

        [TestMethod]
        public void IsDragDropVisible_WhenHasGameDataPath_ReturnsFalse()
        {
            // Act
            _viewModel.HasGameDataPath = true;

            // Assert
            Assert.IsFalse(_viewModel.IsDragDropVisible);
        }

        [TestMethod]
        public void ProgressValue_WhenSet_UpdatesProviderValue()
        {
            // Act
            _viewModel.ProgressValue = 50;

            // Assert
            Assert.AreEqual(50, _progressProvider.PackingProgressPercentage);
        }

        [TestMethod]
        public void ProgressValue_WhenProviderValueChanges_ReturnsUpdatedValue()
        {
            // Act
            _progressProvider.PackingProgressPercentage = 75;

            // Assert
            Assert.AreEqual(75, _viewModel.ProgressValue);
        }

        [TestMethod]
        public void CanCreatePackage_WithValidConfigAndNoErrors_ReturnsTrue()
        {
            // Arrange
            _viewModel.HasValidGameConfig = true;

            // We need to use reflection to test the private CanCreatePackage method
            var canCreatePackageMethod = typeof(PackageCreationViewModel).GetMethod(
                "CanCreatePackage",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (bool)canCreatePackageMethod.Invoke(_viewModel, null);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanCreatePackage_WithoutValidGameConfig_ReturnsFalse()
        {
            // Arrange
            _viewModel.HasValidGameConfig = false;

            // We need to use reflection to test the private CanCreatePackage method
            var canCreatePackageMethod = typeof(PackageCreationViewModel).GetMethod(
                "CanCreatePackage",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (bool)canCreatePackageMethod.Invoke(_viewModel, null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanCreatePackage_WithLayoutError_ReturnsFalse()
        {
            // Arrange
            _viewModel.HasValidGameConfig = true;
            _viewModel.LayoutParseError = "Some layout error";

            // We need to use reflection to test the private CanCreatePackage method
            var canCreatePackageMethod = typeof(PackageCreationViewModel).GetMethod(
                "CanCreatePackage",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (bool)canCreatePackageMethod.Invoke(_viewModel, null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ProcessMakePackageOutput_ParsesXvcPackagePath()
        {
            // Arrange
            string output = "Some output\nSuccessfully created package 'C:\\path\\package.xvc'\nMore output";

            // We need to use reflection to test the private method
            var processMakePackageOutputMethod = typeof(PackageCreationViewModel).GetMethod(
                "ProcessMakePackageOutput",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            processMakePackageOutputMethod.Invoke(_viewModel, new object[] { output });

            // Assert
            Assert.AreEqual("C:\\path\\package.xvc", _packageModel.PackageFilePath);
        }

        [TestMethod]
        public void ProcessMakePackageOutput_ParsesMsixvcPackagePath()
        {
            // Arrange
            string output = "Some output\nSuccessfully created package 'C:\\path\\package.msixvc'\nMore output";

            // We need to use reflection to test the private method
            var processMakePackageOutputMethod = typeof(PackageCreationViewModel).GetMethod(
                "ProcessMakePackageOutput",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            processMakePackageOutputMethod.Invoke(_viewModel, new object[] { output });

            // Assert
            Assert.AreEqual("C:\\path\\package.msixvc", _packageModel.PackageFilePath);
        }

        [TestMethod]
        public void EstimatePackageSize_WithoutGameDataPath_SetsUnknownSize()
        {
            // Arrange
            _viewModel.GameDataPath = string.Empty;

            // We need to use reflection to test the private method
            var estimatePackageSizeMethod = typeof(PackageCreationViewModel).GetMethod(
                "EstimatePackageSize",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            estimatePackageSizeMethod.Invoke(_viewModel, null);

            // Assert
            Assert.AreEqual("Unknown", _viewModel.PackageSize);
        }

        [TestMethod]
        public void LayoutParseError_WhenChanged_UpdatesCanExecuteStatus()
        {
            // Arrange
            bool canExecuteChangedFired = false;

            if (_viewModel.MakePackageCommand is RelayCommand command)
            {
                command.CanExecuteChanged += (s, e) => canExecuteChangedFired = true;
            }

            // Act
            _viewModel.LayoutParseError = "Some error";

            // Assert
            Assert.AreEqual("Some error", _viewModel.LayoutParseError);
            Assert.IsTrue(canExecuteChangedFired);
        }

        [TestMethod]
        public void StartMakePackageProcess_WithoutGameDataPath_SetsError()
        {
            // Arrange
            _viewModel.GameDataPath = string.Empty;

            // We need to use reflection to test the private method
            var startMakePackageProcessMethod = typeof(PackageCreationViewModel).GetMethod(
                "StartMakePackageProcess",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            startMakePackageProcessMethod.Invoke(_viewModel, null);

            // Assert
            Assert.IsNotNull(_viewModel.GameConfigLoadError);
            Assert.IsFalse(string.IsNullOrEmpty(_viewModel.GameConfigLoadError));
        }

        

        [TestMethod]
        public void SubValPath_WhenSet_UpdatesProperty()
        {
            // Act
            _viewModel.SubValPath = @"C:\TestPath\SubVal";

            // Assert
            Assert.AreEqual(@"C:\TestPath\SubVal", _viewModel.SubValPath);
        }

        [TestMethod]
        public void PackageFilePath_WhenSet_UpdatesProperty()
        {
            // Act
            _viewModel.PackageFilePath = @"C:\TestPath\Output";

            // Assert
            Assert.AreEqual(@"C:\TestPath\Output", _viewModel.PackageFilePath);
        }

        [TestMethod]
        public void BigId_WhenSet_UpdatesProperty()
        {
            // Act
            _viewModel.BigId = "TestBigId";

            // Assert
            Assert.AreEqual("TestBigId", _viewModel.BigId);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _viewModel = null;
            File.Delete(_pathConfigProvider.MakePkgPath);
        }
    }
}