using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.Threading.Tasks;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class PackageUploadingViewModelTest
    {
        private UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private Mock<IWindowService> _mockWindowService;
        private PackageUploadingViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _uploadingProgressPercentageProvider = new ();
            _mockWindowService = new Mock<IWindowService>();
            _viewModel = new PackageUploadingViewModel(_uploadingProgressPercentageProvider, 
                                                       _mockWindowService.Object);
        }

        [TestMethod]
        public void Constructor_Should_InitializeViewModel()
        {
            // Assert
            Assert.IsNotNull(_viewModel.CancelUploadCommand);
            Assert.AreEqual(0, _viewModel.PackageUploadPercentage);
            Assert.AreEqual(PackageUploadingProgressStage.NotStarted, _viewModel.UploadStage);
        }

        [TestMethod]
        public void CancelUpload_Should_SetUploadStageToCancelled()
        {
            // Act
            _viewModel.CancelUploadCommand.Execute(null);
            // Assert
            Assert.IsTrue(_uploadingProgressPercentageProvider.UploadingCancelled);
            _mockWindowService.Verify(x => x.NavigateTo(typeof(PackageUploadView)), Times.Once);
        }

        [TestMethod]
        public void UploadingProgressUpdate_WhenPercentageChanged_Should_UpdatePackageUploadPercentage()
        {
            // Arrange
            _uploadingProgressPercentageProvider.UploadingProgressPercentage = 50;
            // Act
            _uploadingProgressPercentageProvider.UploadingProgressPercentage = 100;
            // Assert
            Assert.AreEqual(100, _viewModel.PackageUploadPercentage);
        }
        [TestMethod]
        public void Test_PercentageAndStageGetSet()
        {
            // Arrange
            _viewModel.PackageUploadPercentage = 50;
            _viewModel.UploadStage = PackageUploadingProgressStage.ProcessingPackage;
            // Act
            Assert.AreEqual(50, _uploadingProgressPercentageProvider.UploadingProgressPercentage);
            Assert.AreEqual(PackageUploadingProgressStage.ProcessingPackage, _uploadingProgressPercentageProvider.UploadStage);
        }

        [TestMethod]
        public void UploadingProgressUpdate_WhenStageChanged_Should_UpdateUploadStage()
        {
            // Arrange
            _uploadingProgressPercentageProvider.UploadStage = PackageUploadingProgressStage.NotStarted;
            // Act
            _uploadingProgressPercentageProvider.UploadStage = PackageUploadingProgressStage.ProcessingPackage;
            // Assert
            Assert.AreEqual(PackageUploadingProgressStage.ProcessingPackage, _viewModel.UploadStage);
        }
    }
}