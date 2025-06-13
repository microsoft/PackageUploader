using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System.Collections.Generic;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class MainPageViewModelTest
{
    private Mock<PathConfigurationProvider> _pathConfigurationService;
    private UserLoggedInProvider _userLoggedInProvider;
    private Mock<IAuthenticationService> _authenticationService;
    private Mock<IWindowService> _windowService;
    private Mock<ILogger<MainPageViewModel>> _logger;

    private MainPageViewModel _mainPageViewModel;

    [TestInitialize]
    public void Initialize()
    {
        _pathConfigurationService = new Mock<PathConfigurationProvider>();

        _userLoggedInProvider = new UserLoggedInProvider();
        
        _authenticationService = new Mock<IAuthenticationService>();
        _authenticationService.Setup(x => x.SignInAsync())
                              .Callback( () => _userLoggedInProvider.UserLoggedIn=true )
                              .ReturnsAsync(true);

        _windowService = new Mock<IWindowService>();
        
        _logger = new Mock<ILogger<MainPageViewModel>>();

        _mainPageViewModel = new MainPageViewModel(
            _pathConfigurationService.Object, 
            _userLoggedInProvider,
            _authenticationService.Object, 
            _windowService.Object,
            _logger.Object
        );
    }

    [TestMethod]
    public void TestCaptureUserLoggedIn()
    {
        _userLoggedInProvider.UserLoggedIn = true;
        Assert.IsTrue(_mainPageViewModel.IsUserLoggedIn);
        
        _userLoggedInProvider.UserLoggedIn = false;
        Assert.IsFalse(_mainPageViewModel.IsUserLoggedIn);
    }

    [TestMethod]
    public void TestSignInCommand()
    {
        _mainPageViewModel.SignInCommand.Execute(null);
        _authenticationService.Verify(x => x.SignInAsync(), Times.Once);

        Assert.IsFalse(_mainPageViewModel.SigninStarted);
        Assert.IsTrue(_mainPageViewModel.IsUserLoggedIn);
    }

    [TestMethod]
    public void TestNavigateToPackageCreationCommand()
    {
        _mainPageViewModel.IsMakePkgEnabled = false;
        _mainPageViewModel.NavigateToPackageCreationCommand.Execute(null);
        _windowService.Verify(x => x.NavigateTo(typeof(PackageCreationView)), Times.Never);

        _mainPageViewModel.IsMakePkgEnabled = true;
        _mainPageViewModel.NavigateToPackageCreationCommand.Execute(null);
        _windowService.Verify(x => x.NavigateTo(typeof(PackageCreationView)), Times.Once);
    }

    [TestMethod]
    public void TestPackagingLearnMoreURLCommand()
    {
        // TODO: test this?
        /*
        _mainPageViewModel.PackagingLearnMoreURL.Execute("HelloWorld");
        _windowService.Verify(x => x.OpenURL("https://aka.ms/learn-more-about-packaging"), Times.Once);
        */
    }

    // TODO: Maybe test ResolveExecutablePath
    /*[TestMethod]
    public void TestResolveExecutablePath()
    {

    }*/

    [TestMethod]
    public void TestShowTenantSelectionCommand()
    {
        var tenant = new AzureTenant { DisplayName = "HelloWorld" };
        var tenant2 = new AzureTenant { DisplayName = "HelloWorld2" };
        _authenticationService.Setup(x => x.Tenant)
                              .Returns(tenant);
        _authenticationService.Setup(x => x.GetAvailableTenants())
                              .ReturnsAsync(new AzureTenantList { Value = new List<AzureTenant> { tenant2 }, Count = 1 });

        _mainPageViewModel.ShowTenantSelection = false; // so it'll inverse and show
        _mainPageViewModel.ShowTenantSelectionCommand.Execute(null);

        Assert.IsTrue(_mainPageViewModel.ShowTenantSelection);
        Assert.AreEqual(_mainPageViewModel.AvailableTenants.Count, 1);
        _authenticationService.VerifySet(x => x.Tenant = tenant2, Times.Once);
    }

    [TestMethod]
    public void TestGetTenantsCommand()
    {
        var tenant = new AzureTenant { DisplayName = "HelloWorld" };
        var tenant2 = new AzureTenant { DisplayName = "HelloWorld2" };
        _authenticationService.Setup(x => x.Tenant)
                              .Returns(tenant);
        _authenticationService.Setup(x => x.GetAvailableTenants())
                              .ReturnsAsync(new AzureTenantList { Value = new List<AzureTenant> { tenant2 }, Count = 1 });
        
        _mainPageViewModel.GetTenantsCommand.Execute(null);
        
        Assert.AreEqual(_mainPageViewModel.AvailableTenants.Count, 1);
        _authenticationService.VerifySet(x => x.Tenant = tenant2, Times.Once);
    }

}
