using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.IO;
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
        private GameProduct _gameProduct;
        private Mock<GamePackageConfiguration> _mockGamePackageConfiguration;

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

            _mockPackageUploaderService.Setup(x => x.GetPackageConfigurationAsync(null,
                                                                                  It.IsAny<IGamePackageBranch>(),
                                                                                  It.IsAny<CancellationToken>()))
                                       .ReturnsAsync(_mockGamePackageConfiguration.Object);

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
                _errorModelProvider
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
                 _errorModelProvider
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
                _errorModelProvider
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
                _errorModelProvider
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
            _uploadingProgressPercentageProvider.UploadingProgressPercentage = 50;
            Assert.AreEqual(_viewModel.ProgressValue.Percentage, 50);

            _viewModel.ProgressValue = new PackageUploadingProgress()
            {
                Percentage = 50,
                Stage = PackageUploadingProgressStage.UploadingPackage
            };
            Assert.AreEqual(50, _viewModel.ProgressValue.Percentage);
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
            _packageModelProvider.Package.PackageFilePath = @"C:\test\package.msixvc";
            Assert.AreEqual(@"C:\test\package.msixvc", _viewModel.PackageFilePath);

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
            _viewModel.FileDroppedCommand.Execute(@"C:\test\package.msixvc");
            Assert.AreEqual(@"C:\test\package.msixvc", _viewModel.PackageFilePath);

            _viewModel.FileDroppedCommand.Execute("");
            Assert.AreEqual("", _viewModel.PackageFilePath);
            Assert.AreEqual(_viewModel.PackageErrorMessage, Resources.Strings.PackageUpload.InvalidFilePathErrMsg);
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
    }
}