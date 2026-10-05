using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System;
using System.Diagnostics;
using System.Windows;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class ErrorScreenViewModelTest
{
    private Mock<IWindowService> _windowService;
    private ErrorModelProvider _errorModelProvider;
    private Mock<IClipboardService> _clipboardService;
    private Mock<IProcessStarterService> _processStarterService;

    private ErrorScreenViewModel _errorScreenViewModel;

    [TestInitialize]
    public void Initialize()
    {
        _windowService = new Mock<IWindowService>();
        
        _clipboardService = new Mock<IClipboardService>();
        _processStarterService = new Mock<IProcessStarterService>();

        _errorModelProvider = new ErrorModelProvider();
        _errorModelProvider.Error.MainMessage = "TestMainMessage";
        _errorModelProvider.Error.DetailMessage = "TestDetailMessage";
        _errorModelProvider.Error.OriginPage = typeof(string);
        _errorModelProvider.Error.LogsPath = "TestLogsPath";

        _errorScreenViewModel = new ErrorScreenViewModel(
            _windowService.Object,
            _errorModelProvider,
            _clipboardService.Object,
            _processStarterService.Object
        );
    }

    [TestMethod]
    public void TestAttributes()
    {
        Assert.AreEqual(_errorScreenViewModel.ErrorTitle, _errorModelProvider.Error.MainMessage);
        Assert.AreEqual(_errorScreenViewModel.ErrorDescription, _errorModelProvider.Error.DetailMessage);
        Assert.IsTrue(_errorScreenViewModel.HasLogs);
        Assert.AreEqual(Resources.Strings.ErrorPage.GenericRecoveryGuidance, _errorScreenViewModel.RecoveryGuidance);
        Assert.IsNotNull(_errorScreenViewModel.HomeCommand);
    }

    [TestMethod]
    public void CopyErrorCommandTest()
    {
        _errorScreenViewModel.CopyErrorCommand.Execute(null);

        _clipboardService.Verify(x => x.SetData(DataFormats.Text, _errorModelProvider.Error.MainMessage + Environment.NewLine + _errorModelProvider.Error.DetailMessage), Times.Once);
    }

    [TestMethod]
    public void GoBackAndFixCommandTest()
    {
        _errorScreenViewModel.GoBackAndFixCommand.Execute(null);
        _windowService.Verify(x => x.NavigateTo(It.Is<Type>(x => x == typeof(string))), Times.Once);

    }

    [TestMethod]
    public void GoBackAndFix_WithoutAnOriginPage_NavigatesToTheMainPageView()
    {
        // The fallback has to name a view, not a view model: WindowService.NavigateTo throws
        // ArgumentException for any type that does not inherit from UIElement.
        _errorModelProvider.Error.OriginPage = null;

        _errorScreenViewModel.GoBackAndFixCommand.Execute(null);

        _windowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
        Assert.IsTrue(
            typeof(UIElement).IsAssignableFrom(typeof(MainPageView)),
            "The fallback navigation target must be something WindowService will accept.");
    }

    [TestMethod]
    public void ViewLogsCommandTest()
    {
        _errorScreenViewModel.ViewLogsCommand.Execute(null);
        _processStarterService.Verify(x => x.Start("explorer.exe", $"/select, \"{_errorModelProvider.Error.LogsPath}\""), Times.Once);
    }

    [TestMethod]
    public void ViewLogsCommand_WithoutLogPath_DoesNotOpenExplorer()
    {
        _errorModelProvider.Error.LogsPath = string.Empty;

        _errorScreenViewModel.ViewLogsCommand.Execute(null);

        _processStarterService.Verify(
            x => x.Start(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public void HomeCommand_NavigatesToMainPage()
    {
        _errorScreenViewModel.HomeCommand.Execute(null);

        _windowService.Verify(x => x.NavigateTo(typeof(MainPageView)), Times.Once);
    }

    [TestMethod]
    public void RecoveryGuidance_ReflectsOriginWorkflow()
    {
        _errorModelProvider.Error.OriginPage = typeof(PackageCreationView);
        Assert.AreEqual(Resources.Strings.ErrorPage.PackagingRecoveryGuidance, _errorScreenViewModel.RecoveryGuidance);

        _errorModelProvider.Error.OriginPage = typeof(PackageUploadView);
        Assert.AreEqual(Resources.Strings.ErrorPage.UploadRecoveryGuidance, _errorScreenViewModel.RecoveryGuidance);

        _errorModelProvider.Error.OriginPage = typeof(Msixvc2UploadView);
        Assert.AreEqual(Resources.Strings.ErrorPage.UploadRecoveryGuidance, _errorScreenViewModel.RecoveryGuidance);
    }
}
