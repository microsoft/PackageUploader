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
/*        private ErrorModel _errorModel;
        private PackageModel _packageModel;*/
        private PackageCreationViewModel _viewModel;

        private string _goodMappingFilePath; 
        private string _badMappingFilePath;
        private string _gameDataPath;

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
            //_errorModel = new ErrorModel();

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

            // mock files
            _goodMappingFilePath = Path.GetTempFileName();
            _badMappingFilePath = Path.GetTempFileName();
            _gameDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_gameDataPath);
            File.WriteAllBytes(Path.Combine(_gameDataPath, "testfile.txt"), new byte[1024 * 1024]); // 1 MB
            File.WriteAllBytes(Path.Combine(_gameDataPath, "testfile2.bin"), new byte[1024 * 1024]); // 1 MB
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
            // This sets the test variable to a valid path, even though it doesn't exist.
            // We only care about validity for this test.
            string newPath = @"C:\TestData\Game";

            // Act
            _viewModel.GameDataPath = newPath;

            // Assert
            Assert.AreEqual(newPath, _viewModel.GameDataPath);
        }

        [TestMethod]
        public void MappingDataXmlPath_WhenChanged_GetAndSet()
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
            //Assert.IsTrue(canExecuteChangedFired);
            // TODO: This is too dependent on WPF to run, see this issue in RellayCommandTest.cs
        }

        [TestMethod]
        public void MappingDataXmlPath_EstimatePackageSize_UserInputSafetyParsing()
        {

            // Non Existant Path Game Data Path
            _viewModel.GameDataPath = null;
            Assert.AreEqual("Unknown", _viewModel.PackageSize);
            _viewModel.GameDataPath = string.Empty;
            Assert.AreEqual("Unknown", _viewModel.PackageSize);
            _viewModel.GameDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); // does not exist
            Assert.AreEqual("Unknown", _viewModel.PackageSize);

            // Good Game Data Path, Non Existant Mapping Data Path
            _viewModel.GameDataPath = _gameDataPath;
            _viewModel.MappingDataXmlPath = null;
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);
            _viewModel.MappingDataXmlPath = string.Empty;
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);
            _viewModel.MappingDataXmlPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); // does not exist
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            // Good Game Data Path, Good Path with Bad XML
            _viewModel.GameDataPath = _gameDataPath;
            var badData = "<Package> <Chunk> <hello> </Chunk> </Package>";
            File.WriteAllText(_badMappingFilePath, badData);
            _viewModel.MappingDataXmlPath = _badMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            badData = "<Package> <Chunk> <hello ' > </Chunk> </Package>";
            File.WriteAllText(_badMappingFilePath, badData);
            _viewModel.MappingDataXmlPath = _badMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            badData = "<Package> <Chunk> <hello & > </Chunk> </Package>";
            File.WriteAllText(_badMappingFilePath, badData);
            _viewModel.MappingDataXmlPath = _badMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            badData = "<Package> <Chunk> <hello \" > </Chunk> </Package>";
            File.WriteAllText(_badMappingFilePath, badData);
            _viewModel.MappingDataXmlPath = _badMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            badData = "<Package> <Chunk> <<hello  > </Chunk> </Package>";
            File.WriteAllText(_badMappingFilePath, badData);
            _viewModel.MappingDataXmlPath = _badMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            // Good Game Data Path, Good Path with Good XML, No FileGroup
            _viewModel.GameDataPath = _gameDataPath;
            var goodData = "<Package> <Chunk>  </Chunk> </Package>";
            File.WriteAllText(_goodMappingFilePath, goodData);
            _viewModel.MappingDataXmlPath = _goodMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.NoFilesInLayoutFileErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            // Good Game Data Path, Good Path with Good XML, With FileGroup
            goodData = "<Package> <Chunk> <FileGroup /> </Chunk> </Package>";
            File.WriteAllText(_goodMappingFilePath, goodData);
            _viewModel.MappingDataXmlPath = _goodMappingFilePath;
            Assert.AreEqual(Resources.Strings.PackageCreation.NoFilesInLayoutFileErrorMsg, _viewModel.LayoutParseError);
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

            // Good Game Data Path, Good Path with Good XML, With FileGroup, With File
            goodData = "<Package> <Chunk Id=\"1000\"> " +
                        "<FileGroup DestinationPath=\"\\\" SourcePath=\".\" Include=\"*.txt\"> " +
                        "</Chunk> " +
                        "<Chunk Id=\"1001\"> <FileGroup DestinationPath=\"\\\" SourcePath=\".\" Include=\"testfile2.bin\" /> " +
                        "</Chunk> </Package>";
            File.WriteAllText(_goodMappingFilePath, goodData);
            _viewModel.MappingDataXmlPath = _goodMappingFilePath;
            Assert.AreEqual("< 1 GB", _viewModel.PackageSize);

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
            //Assert.IsTrue(canExecuteChangedFired);
            // TODO: This is too dependent on WPF to run, see this issue in RellayCommandTest.cs
        }

        [TestMethod]
        public void PackageType_WhenSet_UpdatesModelAndNotifies()
        {
            // Act
            _viewModel.PackageType = "Xbox";

            // Assert
            Assert.AreEqual("Xbox", _viewModel.PackageType);
            Assert.AreEqual("Xbox", _packageModelProvider.Package.PackageType);
        }

        [TestMethod]
        public void PackageSize_WhenSet_UpdatesModelAndNotifies()
        {
            // Act
            _viewModel.PackageSize = "5.2 GB";

            // Assert
            Assert.AreEqual("5.2 GB", _viewModel.PackageSize);
            Assert.AreEqual("5.2 GB", _packageModelProvider.Package.PackageSize);
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
            Assert.AreEqual("C:\\path\\package.xvc", _packageModelProvider.Package.PackageFilePath);
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
            Assert.AreEqual("C:\\path\\package.msixvc", _packageModelProvider.Package.PackageFilePath);
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
            //Assert.IsTrue(canExecuteChangedFired);
            // TODO: see this issue in RellayCommandTest.cs
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
            _viewModel.SubValPath = Path.GetTempPath();

            // Assert
            Assert.AreEqual(Path.GetTempPath(), _viewModel.SubValPath);
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

        [TestMethod]
        public void Test_MakePackageCommand_NullEmptyGameDataPath()
        {
            _viewModel.IsCreationInProgress = false;

            _viewModel.GameDataPath = null; // or string.Empty as needed
            _viewModel.HasValidGameConfig = true;
            _viewModel.MakePackageCommand.Execute(null);
            Assert.AreEqual(Resources.Strings.PackageCreation.ProvideGameDataPathErrorMsg, _viewModel.GameConfigLoadError);


            _viewModel.GameDataPath = string.Empty;
            _viewModel.HasValidGameConfig = true;
            _viewModel.MakePackageCommand.Execute(null);
            Assert.AreEqual(Resources.Strings.PackageCreation.ProvideGameDataPathErrorMsg, _viewModel.GameConfigLoadError);
        }

        [TestMethod]
        public void Test_MakePackageCommand_MalformedGameDataPath()
        {
            _viewModel.IsCreationInProgress = false;

            _viewModel.GameDataPath = @"C:\TestData\Game\malformed\" + Guid.NewGuid().ToString(); //shouldn't exist
            _viewModel.HasValidGameConfig = true;
            _viewModel.MakePackageCommand.Execute(null);
            Assert.AreEqual(Resources.Strings.PackageCreation.ProvideGameDataPathErrorMsg, _viewModel.GameConfigLoadError);
            
            _viewModel.GameDataPath = @": blah yis!"; //shouldn't exist
            _viewModel.HasValidGameConfig = true;
            _viewModel.MakePackageCommand.Execute(null);
            Assert.AreEqual(Resources.Strings.PackageCreation.ProvideGameDataPathErrorMsg, _viewModel.GameConfigLoadError);
        }

        [TestMethod]
        public void Test_MakePackageCommand_PackageFilePathBadFormat()
        {
            _viewModel.IsCreationInProgress = false;
                
    
            _viewModel.GameDataPath = Path.GetTempPath(); //should exist
            _viewModel.HasValidGameConfig = true;
            _viewModel.PackageFilePath = @": yes!"; //shouldn't exist
            _viewModel.MakePackageCommand.Execute(null);
            Assert.AreEqual(Resources.Strings.PackageCreation.FailedToCreateOutputDirectoryErrorMsg, _viewModel.OutputDirectoryError);
        }

        [TestMethod]
        public void Test_MakePackageCommand_MalformedDataXmlPath()
        {
            // TODO: Figure out why this is erring out safely earlier than expected...
            _viewModel.IsCreationInProgress = false;
            _viewModel.GameDataPath = Path.GetRandomFileName(); //should exist
            _viewModel.MappingDataXmlPath = @": yes!"; //shouldn't exist
            _viewModel.HasValidGameConfig = true;
            Assert.IsFalse(_viewModel.MakePackageCommand.CanExecute(null));
            _viewModel.MakePackageCommand.Execute(null);
            Assert.IsFalse(_viewModel.IsCreationInProgress);
            //Assert.AreEqual(Resources.Strings.PackageCreation.ProvideMappingDataXmlPathErrorMsg, _viewModel.MappingDataXmlPathError);
            //Assert.IsTrue(_viewModel.MappingDataXmlPath.Contains("generated_layout.xml"));
        }

        [TestMethod]
        public void Test_MakePackageCommand_MalformedSubValPath()
        {
            _viewModel.SubValPath = @": yes!"; //shouldn't exist
            Assert.AreEqual(_viewModel.SubValDllError, Resources.Strings.PackageCreation.SubValDllNotFoundErrorMsg);
        }


        [TestCleanup]
        public void Cleanup()
        {
            _viewModel = null;
            File.Delete(_pathConfigProvider.MakePkgPath);
            File.Delete(_goodMappingFilePath);
            File.Delete(_badMappingFilePath);

            Directory.Delete(_gameDataPath, true);
        }
    }
}