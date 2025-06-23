using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class PackagingProgressViewModelTest
{
    private PackageModelProvider _packageModelProvider;
    private PackingProgressPercentageProvider _packingProgressPercentageProvider;
    private Mock<IWindowService> _mockWindowService;
    private Mock<IProcessStarterService> _mockProcessStarterService;
    private PackagingProgressViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _packageModelProvider = new ();
        _packingProgressPercentageProvider = new ();
        _mockWindowService = new Mock<IWindowService>();
        _mockProcessStarterService = new Mock<IProcessStarterService>();
        _viewModel = new PackagingProgressViewModel(_packingProgressPercentageProvider,
                                                   _packageModelProvider, 
                                                   _mockWindowService.Object,
                                                   _mockProcessStarterService.Object);
    }


    [TestMethod]
    public void Test_PackagingProgressPercent()
    {
        // Arrange
        _packingProgressPercentageProvider.PackingProgressPercentage = 50;
        // Act
        _packingProgressPercentageProvider.PackingProgressPercentage = 100;
        // Assert
        Assert.AreEqual(100, _viewModel.PackingProgressPercentage);

        // Act
        _viewModel.PackingProgressPercentage = 28;
        Assert.AreEqual(28, _packingProgressPercentageProvider.PackingProgressPercentage);
    }

    [TestMethod]
    public void Test_ViewLogsCommand()
    {
        // Arrange
        string expectedLogPath = "mockLogPath";
        _packageModelProvider.PackagingLogFilepath = expectedLogPath;
        // Act
        _viewModel.ViewLogsCommand.Execute(null);
        // Assert
        _mockProcessStarterService.Verify(x => x.Start("explorer.exe", $"/select, \"{expectedLogPath}\""), Times.Once);
    }

    [TestMethod]
    public void Test_CancelCreationCommand()
    {
        // Arrange
        _packingProgressPercentageProvider.PackingCancelled = false;
        // Act
        _viewModel.CancelCreationCommand.Execute(null);
        // Assert
        Assert.IsTrue(_packingProgressPercentageProvider.PackingCancelled);
        _mockWindowService.Verify(x => x.NavigateTo(typeof(PackageCreationView)), Times.Once);
    }

}
