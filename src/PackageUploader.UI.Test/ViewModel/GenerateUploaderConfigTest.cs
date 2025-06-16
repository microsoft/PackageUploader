// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.Text.Json;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class GenerateUploaderConfigTest
    {
        private class TestableUploaderConfigViewModel : PackageUploadViewModel
        {
            public bool WriteAllTextCalled { get; private set; } = false;
            public string LastWrittenPath { get; private set; } = string.Empty;
            public string LastWrittenContents { get; private set; } = string.Empty;
            
            // Properties to verify the generated config
            public UploadConfig GeneratedConfig { get; private set; }

            public TestableUploaderConfigViewModel(
                PackageModelProvider packageModelProvider,
                IPackageUploaderService uploaderService,
                IWindowService windowService,
                UploadingProgressPercentageProvider uploadingProgressPercentageProvider,
                ErrorModelProvider errorModelProvider)
                : base(packageModelProvider, uploaderService, windowService, uploadingProgressPercentageProvider, errorModelProvider)
            {
            }

            // Expose the private method for testing
            public void TestGenerateUploaderConfig()
            {
                GenerateUploaderConfig();
            }

            // Override WriteAllText to capture data instead of writing to disk
            protected override void WriteAllText(string path, string contents)
            {
                WriteAllTextCalled = true;
                LastWrittenPath = path;
                LastWrittenContents = contents;

                // Parse the JSON to verify the structure
                try
                {
                    GeneratedConfig = JsonSerializer.Deserialize<UploadConfig>(contents);
                }
                catch
                {
                    // If parsing fails, set to null
                    GeneratedConfig = null;
                }
            }
        }

        private PackageModelProvider _packageModelProvider;
        private Mock<IPackageUploaderService> _mockUploaderService;
        private Mock<IWindowService> _mockWindowService;
        private UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private ErrorModelProvider _errorModelProvider;
        private TestableUploaderConfigViewModel _viewModel;

        // Mocks for the GamePackageBranch and configuration
        private Mock<IGamePackageBranch> _mockBranch;
        private Mock<IGamePackageBranch> _mockFlight;
        private IReadOnlyCollection<IGamePackageBranch> _branchesAndFlights;

        [TestInitialize]
        public void Setup()
        {
            // Setup package model
            _packageModelProvider = new PackageModelProvider();
            _mockUploaderService = new Mock<IPackageUploaderService>();
            _mockWindowService = new Mock<IWindowService>();
            _uploadingProgressPercentageProvider = new UploadingProgressPercentageProvider();
            _errorModelProvider = new ErrorModelProvider();

            // Setup branch and flight
            _mockBranch = new Mock<IGamePackageBranch>();
            _mockBranch.Setup(b => b.Name).Returns("TestBranch");
            _mockBranch.Setup(b => b.BranchType).Returns(GamePackageBranchType.Branch);
            _mockBranch.Setup(b => b.CurrentDraftInstanceId).Returns("branch-123");

            _mockFlight = new Mock<IGamePackageBranch>();
            _mockFlight.Setup(f => f.Name).Returns("TestFlight");
            _mockFlight.Setup(f => f.BranchType).Returns(GamePackageBranchType.Flight);
            _mockFlight.Setup(f => f.CurrentDraftInstanceId).Returns("flight-456");

            _branchesAndFlights =
            [
                _mockBranch.Object,
                _mockFlight.Object
            ];

            _packageModelProvider.Package = new PackageModel
            {
                BigId = "9NBLGGH42THS",
                PackageFilePath = @"C:\test\package.msixvc",
                EkbFilePath = @"C:\test\package.ekb",
                SubValFilePath = @"C:\test\package.xml",
                SymbolBundleFilePath = @"C:\test\symbols.zip",
            };

            // Create view model
            _viewModel = new TestableUploaderConfigViewModel(
                _packageModelProvider,
                _mockUploaderService.Object,
                _mockWindowService.Object,
                _uploadingProgressPercentageProvider,
                _errorModelProvider
            )
            {
                // Set up the view model with branches and flights
                BranchOrFlightDisplayName = "Branch: TestBranch",
                MarketGroupName = "TestMarket"
            };
        }

        [TestMethod]
        public void GenerateUploaderConfig_WithBranch_GeneratesCorrectConfig()
        {
            // Arrange - Set up the mock to return a branch when GetBranchOrFlightFromUISelection is called
            var field = typeof(PackageUploadViewModel).GetField("_branchesAndFlights", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_viewModel, _branchesAndFlights);

            _viewModel.BigId = "9NBLGGH42THS";
            _viewModel.PackageFilePath = @"C:\test\package.msixvc";
            _viewModel.BranchOrFlightDisplayName = "Branch: TestBranch";

            // Act
            _viewModel.TestGenerateUploaderConfig();

            // Assert
            Assert.IsTrue(_viewModel.WriteAllTextCalled, "WriteAllText should be called");
            
            // Verify the path has the expected format
            StringAssert.Contains(_viewModel.LastWrittenPath, "PackageUploader_UI_GeneratedConfig_");
            
            // Verify the config object was created correctly
            Assert.IsNotNull(_viewModel.GeneratedConfig, "Config should be parsed successfully");
            Assert.AreEqual("9NBLGGH42THS", _viewModel.GeneratedConfig.bigId);
            Assert.AreEqual("TestBranch", _viewModel.GeneratedConfig.branchFriendlyName);
            Assert.AreEqual(string.Empty, _viewModel.GeneratedConfig.flightName);
            Assert.AreEqual(@"C:\test\package.msixvc", _viewModel.GeneratedConfig.packageFilePath);
            Assert.AreEqual("TestMarket", _viewModel.GeneratedConfig.marketGroupName);
            Assert.AreEqual(@"C:\test\package.ekb", _viewModel.GeneratedConfig.gameAssets.ekbFilePath);
            Assert.AreEqual(@"C:\test\package.xml", _viewModel.GeneratedConfig.gameAssets.subValFilePath);
            Assert.AreEqual(@"C:\test\symbols.zip", _viewModel.GeneratedConfig.gameAssets.symbolsFilePath);
        }

        [TestMethod]
        public void GenerateUploaderConfig_WithFlight_GeneratesCorrectConfig()
        {
            // Arrange - Set up the mock to return a flight when GetBranchOrFlightFromUISelection is called
            var field = typeof(PackageUploadViewModel).GetField("_branchesAndFlights", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_viewModel, _branchesAndFlights);

            _viewModel.BranchOrFlightDisplayName = "Flight: TestFlight";

            // Act
            _viewModel.TestGenerateUploaderConfig();

            // Assert
            Assert.IsTrue(_viewModel.WriteAllTextCalled, "WriteAllText should be called");
            
            // Verify the config object was created correctly
            Assert.IsNotNull(_viewModel.GeneratedConfig, "Config should be parsed successfully");
            Assert.AreEqual("9NBLGGH42THS", _viewModel.GeneratedConfig.bigId);
            Assert.AreEqual(string.Empty, _viewModel.GeneratedConfig.branchFriendlyName);
            Assert.AreEqual("TestFlight", _viewModel.GeneratedConfig.flightName);
            Assert.AreEqual(@"C:\test\package.msixvc", _viewModel.GeneratedConfig.packageFilePath);
        }

        [TestMethod]
        public void GenerateUploaderConfig_NullBranchOrFlight_DoesNotGenerateConfig()
        {
            // Arrange - Set up null branches and flights
            var field = typeof(PackageUploadViewModel).GetField("_branchesAndFlights", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_viewModel, null);

            // Act
            _viewModel.TestGenerateUploaderConfig();

            // Assert
            Assert.IsFalse(_viewModel.WriteAllTextCalled, "WriteAllText should not be called when branch/flight is null");
            Assert.IsNull(_viewModel.GeneratedConfig, "Config should not be generated");
        }

        [TestMethod]
        public void GenerateUploaderConfig_WithNoSymbols_DoesNotIncludeSymbolsPath()
        {
            // Arrange - Set up the mock with a branch but no symbols path
            var field = typeof(PackageUploadViewModel).GetField("_branchesAndFlights", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_viewModel, _branchesAndFlights);

            _viewModel.BranchOrFlightDisplayName = "Branch: TestBranch";
            _packageModelProvider.Package.SymbolBundleFilePath = string.Empty; // No symbols

            // Act
            _viewModel.TestGenerateUploaderConfig();

            // Assert
            Assert.IsTrue(_viewModel.WriteAllTextCalled, "WriteAllText should be called");
            
            // Verify the config object doesn't include symbols path
            Assert.IsNotNull(_viewModel.GeneratedConfig, "Config should be parsed successfully");
            Assert.AreEqual(string.Empty, _viewModel.GeneratedConfig.gameAssets.symbolsFilePath);
        }

        [TestMethod]
        public void GenerateUploaderConfig_ValidJson_IsHumanReadable()
        {
            // Arrange - Set up the mock to return a branch
            var field = typeof(PackageUploadViewModel).GetField("_branchesAndFlights", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_viewModel, _branchesAndFlights);

            // Act
            _viewModel.TestGenerateUploaderConfig();

            // Assert
            Assert.IsTrue(_viewModel.WriteAllTextCalled, "WriteAllText should be called");
            
            // Check that the JSON is formatted with indentation (human-readable)
            StringAssert.Contains(_viewModel.LastWrittenContents, "\n  ");
            
            // Verify we can deserialize the JSON
            Assert.IsNotNull(_viewModel.GeneratedConfig, "JSON should be valid");
        }
    }
}