using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;

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

}
