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
    public void ViewLogsCommandTest()
    {
        _errorScreenViewModel.ViewLogsCommand.Execute(null);
        _processStarterService.Verify(x => x.Start("explorer.exe", $"/select, \"{_errorModelProvider.Error.LogsPath}\""), Times.Once);
    }
}
