// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.ClientApi;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.Test.ViewModel
{
    /// <summary>
    /// Tests for the ExtractIdInformationFromValidatorLog method in PackageUploadViewModel
    /// </summary>
    [TestClass]
    public class ExtractIdInformationFromValidatorLogTest
    {
        private class TestableValidatorLogViewModel : PackageUploadViewModel, IDisposable
        {
            public string TestSubValFilePath { get; set; }
            private readonly string _xmlContent;
            private bool _disposed = false;
            
            public TestableValidatorLogViewModel(
                PackageModelProvider packageModelProvider,
                IPackageUploaderService uploaderService,
                IWindowService windowService,
                UploadingProgressPercentageProvider uploadingProgressPercentageProvider,
                ErrorModelProvider errorModelProvider,
                string xmlContent) 
                : base(packageModelProvider, uploaderService, windowService, uploadingProgressPercentageProvider, errorModelProvider)
            {
                _xmlContent = xmlContent;
                TestSubValFilePath = Path.GetTempFileName();
                File.WriteAllText(TestSubValFilePath, _xmlContent);
                
                // Set the value in the base class via property
                SubValFilePath = TestSubValFilePath;
            }

            // Implements IDisposable
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        // Cleanup managed resources
                        Cleanup();
                    }
                    
                    _disposed = true;
                }
            }

            private void Cleanup()
            {
                if (File.Exists(TestSubValFilePath))
                {
                    try
                    {
                        File.Delete(TestSubValFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            // Expose the protected method for testing
            public void TestExtractIdInformationFromValidatorLog(
                Guid expectedBuildId, 
                out string type, 
                out string titleId, 
                out string storeId, 
                out string logoFilename)
            {
                ExtractIdInformationFromValidatorLog(expectedBuildId, out type, out titleId, out storeId, out logoFilename);
            }

            // Override to use our test file
            protected override bool FileExists(string path)
            {
                return path == TestSubValFilePath || File.Exists(path);
            }
        }

        private PackageModelProvider _packageModelProvider;
        private Mock<IPackageUploaderService> _mockPackageUploaderService;
        private Mock<IWindowService> _mockWindowService;
        private UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private ErrorModelProvider _errorModelProvider;

        [TestInitialize]
        public void Setup()
        {
            _packageModelProvider = new PackageModelProvider();
            _mockPackageUploaderService = new Mock<IPackageUploaderService>();
            _mockWindowService = new Mock<IWindowService>();
            _uploadingProgressPercentageProvider = new UploadingProgressPercentageProvider();
            _errorModelProvider = new ErrorModelProvider();
        }

        [TestMethod]
        public void ExtractIdInformationFromValidatorLog_ValidXml_ExtractsCorrectValues()
        {
            // Arrange
            var expectedBuildId = Guid.NewGuid();
            string validXml = $@"<?xml version='1.0' encoding='utf-8'?>
                <project>
                    <BuildId>{expectedBuildId}</BuildId>
                    <Type>MSIXVC</Type>
                    <GameConfig>
                        <Game>
                            <StoreId>9NBLGGH42THS</StoreId>
                            <TitleId>ABCDEF12</TitleId>
                            <ShellVisuals Square150x150Logo='Assets/Logo.png' />
                        </Game>
                    </GameConfig>
                </project>";

            using var viewModel = new TestableValidatorLogViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                validXml);
            // Act
            viewModel.TestExtractIdInformationFromValidatorLog(
                expectedBuildId,
                out string type,
                out string titleId,
                out string storeId,
                out string logoFilename);

            // Assert
            Assert.AreEqual("MSIXVC", type, "Type should be extracted correctly");
            Assert.AreEqual("ABCDEF12", titleId, "TitleId should be extracted correctly");
            Assert.AreEqual("9NBLGGH42THS", storeId, "StoreId should be extracted correctly");
            Assert.AreEqual("Assets/Logo.png", logoFilename, "Logo filename should be extracted correctly");
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ExtractIdInformationFromValidatorLog_MissingFile_ThrowsException()
        {
            // Arrange
            var expectedBuildId = Guid.NewGuid();
            string validXml = $@"<?xml version='1.0' encoding='utf-8'?>
                <project>
                    <BuildId>{expectedBuildId}</BuildId>
                </project>";

            using var viewModel = new TestableValidatorLogViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                validXml);
            // Delete the file to simulate missing file scenario
            if (File.Exists(viewModel.TestSubValFilePath))
            {
                File.Delete(viewModel.TestSubValFilePath);
            }

            // Act - Should throw FileNotFoundException
            viewModel.TestExtractIdInformationFromValidatorLog(
                expectedBuildId,
                out string type,
                out string titleId,
                out string storeId,
                out string logoFilename);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ExtractIdInformationFromValidatorLog_MismatchedBuildId_ThrowsException()
        {
            // Arrange
            var actualBuildId = Guid.NewGuid();
            var differentBuildId = Guid.NewGuid(); // Different from the one in the XML
            
            string validXml = $@"<?xml version='1.0' encoding='utf-8'?>
                <project>
                    <BuildId>{actualBuildId}</BuildId>
                    <Type>MSIXVC</Type>
                    <GameConfig>
                        <Game>
                            <StoreId>9NBLGGH42THS</StoreId>
                            <TitleId>ABCDEF12</TitleId>
                            <ShellVisuals Square150x150Logo='Assets/Logo.png' />
                        </Game>
                    </GameConfig>
                </project>";

            using var viewModel = new TestableValidatorLogViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                validXml);
            // Act - Should throw exception due to build ID mismatch
            viewModel.TestExtractIdInformationFromValidatorLog(
                differentBuildId, // Different from XML
                out string type,
                out string titleId,
                out string storeId,
                out string logoFilename);
        }
        
        [TestMethod]
        public void ExtractIdInformationFromValidatorLog_MissingNodes_SetsEmptyValues()
        {
            // Arrange
            var expectedBuildId = Guid.NewGuid();
            string validXml = $@"<?xml version='1.0' encoding='utf-8'?>
                <project>
                    <BuildId>{expectedBuildId}</BuildId>
                    <!-- Missing Type, StoreId, TitleId, and ShellVisuals nodes -->
                </project>";

            using var viewModel = new TestableValidatorLogViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                validXml);
            // Act
            viewModel.TestExtractIdInformationFromValidatorLog(
                expectedBuildId,
                out string type,
                out string titleId,
                out string storeId,
                out string logoFilename);

            // Assert
            Assert.AreEqual(string.Empty, type, "Type should be empty when node is missing");
            Assert.AreEqual(string.Empty, titleId, "TitleId should be empty when node is missing");
            Assert.AreEqual(string.Empty, storeId, "StoreId should be empty when node is missing");
            Assert.AreEqual(string.Empty, logoFilename, "Logo filename should be empty when node is missing");
        }

        [TestMethod]
        public void ExtractIdInformationFromValidatorLog_MissingAttributes_SetsEmptyValues()
        {
            // Arrange
            var expectedBuildId = Guid.NewGuid();
            string validXml = $@"<?xml version='1.0' encoding='utf-8'?>
                <project>
                    <BuildId>{expectedBuildId}</BuildId>
                    <Type>MSIXVC</Type>
                    <GameConfig>
                        <Game>
                            <StoreId>9NBLGGH42THS</StoreId>
                            <TitleId>ABCDEF12</TitleId>
                            <ShellVisuals />  <!-- Missing Square150x150Logo attribute -->
                        </Game>
                    </GameConfig>
                </project>";

            using var viewModel = new TestableValidatorLogViewModel(
                _packageModelProvider,
                _mockPackageUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider,
                validXml);
            // Act
            viewModel.TestExtractIdInformationFromValidatorLog(
                expectedBuildId,
                out string type,
                out string titleId,
                out string storeId,
                out string logoFilename);

            // Assert
            Assert.AreEqual("MSIXVC", type, "Type should be extracted correctly");
            Assert.AreEqual("ABCDEF12", titleId, "TitleId should be extracted correctly");
            Assert.AreEqual("9NBLGGH42THS", storeId, "StoreId should be extracted correctly");
            Assert.AreEqual(string.Empty, logoFilename, "Logo filename should be empty when attribute is missing");
        }
    }
}