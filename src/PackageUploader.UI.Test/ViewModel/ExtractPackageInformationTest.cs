// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.ClientApi;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;

namespace PackageUploader.UI.Test.ViewModel
{
    /// <summary>
    /// Tests for the ExtractPackageInformation method in PackageUploadViewModel using the TestablePackageUploadViewModel
    /// </summary>
    [TestClass]
    public class ExtractPackageInformationTest
    {
        private PackageModelProvider _packageModelProvider;
        private Mock<IPackageUploaderService> _mockPackageUploaderService;
        private Mock<IWindowService> _mockWindowService;
        private UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private ErrorModelProvider _errorModelProvider;
        private TestablePackageUploadViewModel _viewModel;
        private string _tempFolder;
        private string _mockPackagePath;

        [TestInitialize]
        public void Setup()
        {
            // Create temp directory structure for testing
            _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
            _mockPackagePath = Path.Combine(_tempFolder, "testpackage.xvc");

            // Setup mocks
            _packageModelProvider = new PackageModelProvider();
            _mockPackageUploaderService = new Mock<IPackageUploaderService>();
            _mockWindowService = new Mock<IWindowService>();
            _uploadingProgressPercentageProvider = new UploadingProgressPercentageProvider();
            _errorModelProvider = new ErrorModelProvider();

            // Create the testable view model
            _viewModel = new TestablePackageUploadViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider
            );

            // Create a test file
            using var fs = File.Create(_mockPackagePath);
            fs.SetLength(1024 * 1024); // 1MB
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up temp files
            if (Directory.Exists(_tempFolder))
            {
                try
                {
                    Directory.Delete(_tempFolder, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Tests successful extraction of package information with storeId present
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_WithStoreId_SetsPropertiesCorrectly()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "ABCDEF12";
            var packageType = "MSIXVC";
            var logoFilename = "Assets/Logo.png";
            var expectedEkbPath = Path.Combine(_tempFolder, $"testpackage_Full_{keyId}.ekb");
            var expectedSubValPath = Path.Combine(_tempFolder, "Validator_testpackage.xml");
            var expectedSymbolPath = Path.Combine(_tempFolder, $"testpackage_{titleId}.zip");
            
            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;
            
            // Create valid file content bytes for the logo image
            byte[] logoBytes = new byte[100];
            for (int i = 0; i < logoBytes.Length; i++)
            {
                logoBytes[i] = (byte)i;
            }
            _viewModel.FileContentsToReturn = logoBytes;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.IsTrue(_viewModel.GetBuildAndKeyIdCalled, "GetBuildAndKeyId should be called");
            Assert.IsTrue(_viewModel.ExtractIdInformationCalled, "ExtractIdInformation should be called");
            Assert.IsTrue(_viewModel.ExtractFileCalled, "ExtractFile should be called");

            Assert.AreEqual(storeId, _viewModel.BigId, "BigId should be set correctly");
            Assert.AreEqual("PC", _viewModel.PackageType, "Package type should be PC for MSIXVC");
            Assert.AreEqual("testpackage", _viewModel.PackageId, "PackageId should be set correctly");
            Assert.AreEqual(expectedEkbPath, _viewModel.EkbFilePath, "EKB path should be set correctly");
            Assert.AreEqual(expectedSubValPath, _viewModel.SubValFilePath, "SubVal path should be set correctly");
            Assert.AreEqual(expectedSymbolPath, _viewModel.SymbolBundleFilePath, "Symbol path should be set correctly");
            Assert.IsFalse(_viewModel.IsPackageMissingStoreId, "IsPackageMissingStoreId should be false");
            Assert.IsNotNull(_viewModel.PackagePreviewImage, "Package preview image should be set");
            Assert.AreEqual("10 MB", _viewModel.PackageSize, "Package size should be formatted correctly");
        }

        /// <summary>
        /// Tests handling of missing storeId
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_MissingStoreId_SetsIsPackageMissingStoreId()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = string.Empty; // Empty storeId
            var titleId = "ABCDEF12";
            var packageType = "MSIXVC";
            var logoFilename = "Assets/Logo.png";

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.IsTrue(_viewModel.IsPackageMissingStoreId, "IsPackageMissingStoreId should be true");
            Assert.AreEqual(string.Empty, _viewModel.BigId, "BigId should be empty");
            Assert.IsNotNull(_viewModel.PackageErrorMessage, "Error message should be set");
            Assert.IsTrue(_viewModel.PackageErrorMessage.Contains("StoreId"), "Error message should mention StoreId");
        }

        /// <summary>
        /// Tests handling of XVC package type
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_XvcPackage_SetsConsolePackageType()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "ABCDEF12";
            var packageType = "XVC"; // XVC instead of MSIXVC
            var logoFilename = "Assets/Logo.png";

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.AreEqual("Console", _viewModel.PackageType, "Package type should be Console for XVC");
        }

        /// <summary>
        /// Tests handling of large package files with GB size
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_LargeFile_FormatsFileSizeInGB()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "ABCDEF12";
            var packageType = "MSIXVC";
            var logoFilename = "Assets/Logo.png";

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;
            _viewModel.FileSizeToReturn = 3L * 1024 * 1024 * 1024; // 3GB

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.AreEqual("3 GB", _viewModel.PackageSize, "Package size should be formatted in GB");
        }

        /// <summary>
        /// Tests handling of exception in GetBuildAndKeyId
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_ExceptionInGetBuildAndKeyId_SetsErrorMessage()
        {
            // Arrange
            _viewModel.ThrowExceptionInGetBuildAndKey = true;
            _viewModel.ExceptionToThrow = new InvalidOperationException("Test exception");

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.IsTrue(_viewModel.GetBuildAndKeyIdCalled, "GetBuildAndKeyId should be called");
            Assert.IsFalse(_viewModel.ExtractIdInformationCalled, "ExtractIdInformation should not be called");
            Assert.IsFalse(_viewModel.ExtractFileCalled, "ExtractFile should not be called");
            Assert.IsNotNull(_viewModel.PackageErrorMessage, "Error message should be set");
            Assert.IsTrue(_viewModel.PackageErrorMessage.Contains("Test exception"), "Error message should include the exception message");
        }

        /// <summary>
        /// Tests handling of exception in ExtractIdInformationFromValidatorLog
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_ExceptionInExtractIdInfo_SetsErrorMessage()
        {
            // Arrange
            _viewModel.ThrowExceptionInExtractId = true;
            _viewModel.ExceptionToThrow = new InvalidOperationException("Validator log error");

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.IsTrue(_viewModel.GetBuildAndKeyIdCalled, "GetBuildAndKeyId should be called");
            Assert.IsTrue(_viewModel.ExtractIdInformationCalled, "ExtractIdInformation should be called");
            Assert.IsFalse(_viewModel.ExtractFileCalled, "ExtractFile should not be called");
            Assert.IsNotNull(_viewModel.PackageErrorMessage, "Error message should be set");
            Assert.IsTrue(_viewModel.PackageErrorMessage.Contains("Validator log error"), "Error message should include the exception message");
        }

        /// <summary>
        /// Tests handling of missing logo filename
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_MissingLogoFilename_DoesNotCallExtractFile()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "ABCDEF12";
            var packageType = "MSIXVC";
            var logoFilename = string.Empty; // No logo filename

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.IsTrue(_viewModel.GetBuildAndKeyIdCalled, "GetBuildAndKeyId should be called");
            Assert.IsTrue(_viewModel.ExtractIdInformationCalled, "ExtractIdInformation should be called");
            Assert.IsFalse(_viewModel.ExtractFileCalled, "ExtractFile should not be called when logo filename is empty");
            Assert.IsNull(_viewModel.PackagePreviewImage, "Package preview image should be null");
        }

        /// <summary>
        /// Tests that symbol bundle file path is not set when file does not exist
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_SymbolFileDoesNotExist_SetsEmptySymbolBundlePath()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "NONEXISTENT"; // Use a different title ID that won't match our test setup
            var packageType = "MSIXVC";
            var logoFilename = "Assets/Logo.png";

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.AreEqual(string.Empty, _viewModel.SymbolBundleFilePath, "Symbol bundle file path should be empty when file doesn't exist");
        }

        /// <summary>
        /// Tests that all file paths are constructed correctly
        /// </summary>
        [TestMethod]
        public void ExtractPackageInformation_FilePathsConstructedCorrectly()
        {
            // Arrange
            var buildId = Guid.NewGuid();
            var keyId = Guid.NewGuid();
            var storeId = "9NBLGGH42THS";
            var titleId = "ABCDEF12";
            var packageType = "MSIXVC";
            var logoFilename = "Assets/Logo.png";
            var expectedEkbPath = Path.Combine(_tempFolder, $"testpackage_Full_{keyId}.ekb");
            var expectedSubValPath = Path.Combine(_tempFolder, "Validator_testpackage.xml");

            _viewModel.BuildIdToReturn = buildId;
            _viewModel.KeyIdToReturn = keyId;
            _viewModel.StoreIdToReturn = storeId;
            _viewModel.TitleIdToReturn = titleId;
            _viewModel.TypeToReturn = packageType;
            _viewModel.LogoFilenameToReturn = logoFilename;

            // Act
            _viewModel.TestExtractPackageInformation(_mockPackagePath);

            // Assert
            Assert.AreEqual(expectedEkbPath, _viewModel.EkbFilePath, "EKB file path should be constructed correctly");
            Assert.AreEqual(expectedSubValPath, _viewModel.SubValFilePath, "SubVal file path should be constructed correctly");
        }
    }
}