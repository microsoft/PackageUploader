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

namespace PackageUploader.UI.Test;

[TestClass]
public class ErrorScreenViewModelTest
{
    private Mock<IWindowService> _windowService;
    private ErrorModelProvider _errorModelProvider;

    private ErrorScreenViewModel _errorScreenViewModel;

    [TestInitialize]
    public void Initialize()
    {
        _windowService = new Mock<IWindowService>();
        
        _errorModelProvider = new ErrorModelProvider();
        _errorModelProvider.Error.MainMessage = "TestMainMessage";
        _errorModelProvider.Error.DetailMessage = "TestDetailMessage";
        _errorModelProvider.Error.OriginPage = typeof(string);
        _errorModelProvider.Error.LogsPath = "TestLogsPath";

        _errorScreenViewModel = new ErrorScreenViewModel(
            _windowService.Object,
            _errorModelProvider
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
        /*_errorScreenViewModel.CopyErrorCommand.Execute(null);
        Assert.IsTrue(Clipboard.ContainsText());
        
        var copiedText = Clipboard.GetText();
        var expectedText = _errorModelProvider.Error.MainMessage + Environment.NewLine + _errorModelProvider.Error.DetailMessage;
        Assert.AreEqual(expectedText, copiedText);*/

        // would need to refactor CopyError to take dependency injection for Clipboard.SetData...
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
        //_errorScreenViewModel.ViewLogsCommand.Execute(null);
        // would need to refactor ViewLogs to accept some kind of dependency injection
        //so we can mock up Process.Start
    }
}
