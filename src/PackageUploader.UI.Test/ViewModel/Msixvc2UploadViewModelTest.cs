// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;
using PackageUploader.ClientApi;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class Msixvc2UploadViewModelTest
{
    private Mock<IWindowService> _mockWindowService;
    private Mock<IPackageUploaderService> _mockUploaderService;
    private Mock<ILogger<Msixvc2UploadViewModel>> _mockLogger;
    private ErrorModelProvider _errorModelProvider;
    private PathConfigurationProvider _pathConfigurationProvider;
    private PackageModelProvider _packageModelProvider;

    private Msixvc2UploadViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _mockWindowService = new Mock<IWindowService>();
        _mockUploaderService = new Mock<IPackageUploaderService>();
        _mockLogger = new Mock<ILogger<Msixvc2UploadViewModel>>();
        _errorModelProvider = new ErrorModelProvider();
        _pathConfigurationProvider = new PathConfigurationProvider();
        _packageModelProvider = new PackageModelProvider();

        _viewModel = new Msixvc2UploadViewModel(
            _mockWindowService.Object,
            _mockUploaderService.Object,
            _mockLogger.Object,
            _errorModelProvider,
            _pathConfigurationProvider,
            _packageModelProvider
        );
    }

    #region Constructor & Property initialization

    [TestMethod]
    public void Constructor_InitializesProperties()
    {
        Assert.AreEqual(string.Empty, _viewModel.ContentPath);
        Assert.IsNotNull(_viewModel.UploadPackageCommand);
        Assert.IsNotNull(_viewModel.CancelButtonCommand);
        Assert.IsNotNull(_viewModel.BrowseContentPathCommand);
    }

    #endregion

    #region BuildUploadArguments Tests

    [TestMethod]
    public void BuildUploadArguments_WithBranch()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Branch: Main";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = "9ABC123DEF456";

        string args = _viewModel.BuildUploadArguments();

        Assert.IsTrue(args.StartsWith("upload /d \"C:\\game\\content\" /msixvc2"));
        Assert.IsTrue(args.Contains("/branch \"Main\""));
        Assert.IsFalse(args.Contains("/flight"));
        Assert.IsTrue(args.Contains("/market \"default\""));
        Assert.IsTrue(args.Contains("/storeid \"9ABC123DEF456\""));
        Assert.IsTrue(args.Contains("/auth CacheableBrowser"));
    }

    [TestMethod]
    public void BuildUploadArguments_WithFlight()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Flight: TestFlight";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = "None";

        string args = _viewModel.BuildUploadArguments();

        Assert.IsTrue(args.Contains("/flight \"TestFlight\""));
        Assert.IsFalse(args.Contains("/branch"));
        Assert.IsFalse(args.Contains("/storeid"));
    }

    [TestMethod]
    public void BuildUploadArguments_NoStoreId_NotIncluded()
    {
        _viewModel.ContentPath = @"C:\game\content";
        _viewModel.BranchOrFlightDisplayName = "Branch: Dev";
        _viewModel.MarketGroupName = "default";
        _viewModel.BigId = string.Empty;

        string args = _viewModel.BuildUploadArguments();

        Assert.IsFalse(args.Contains("/storeid"));
    }
    #endregion

    #region CanUpload (via UploadPackageCommand.CanExecute) Tests

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenContentPathEmpty()
    {
        _viewModel.ContentPath = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenNoBranchSelected()
    {
        _viewModel.BranchOrFlightDisplayName = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenNoMarketSelected()
    {
        _viewModel.MarketGroupName = string.Empty;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenLoadingBranches()
    {
        _viewModel.IsLoadingBranchesAndFlights = true;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenLoadingMarkets()
    {
        _viewModel.IsLoadingMarkets = true;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void CanUpload_ReturnsFalse_WhenContentPathHasError()
    {
        _viewModel.ContentPathError = "Some error";

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }
    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void CancelButton_NavigatesToMainPage()
    {
        _viewModel.CancelButtonCommand.Execute(null);

        _mockWindowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
    }

    [TestMethod]
    public void ErrorModelProvider_ReceivesCorrectValues_OnError()
    {
        // Verify the error model provider is the one we injected
        Assert.IsNotNull(_errorModelProvider);
        Assert.AreEqual(string.Empty, _errorModelProvider.Error.MainMessage);
    }
    #endregion
}
