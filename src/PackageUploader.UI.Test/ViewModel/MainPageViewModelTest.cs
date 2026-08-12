using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using PackageUploader.ClientApi.Tools;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
            new Msixvc2ToolResolver(),
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
        Assert.AreEqual(1, _mainPageViewModel.AvailableTenants.Count);
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
        
        Assert.AreEqual(1, _mainPageViewModel.AvailableTenants.Count);
        _authenticationService.VerifySet(x => x.Tenant = tenant2, Times.Once);
    }

    #region MSIXVC2 capability probe

    private MainPageViewModel CreateViewModel(IMsixvc2ToolResolver resolver) =>
        new(
            _pathConfigurationService.Object,
            _userLoggedInProvider,
            _authenticationService.Object,
            _windowService.Object,
            resolver,
            _logger.Object);

    [TestMethod]
    public async Task Msixvc2Probe_EnablesMsixvc2_WhenAToolIsResolved()
    {
        var resolver = new Mock<IMsixvc2ToolResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Msixvc2Tool(@"C:\gdk\MakePkg.exe", IsMakePkg2Fallback: false));

        var viewModel = CreateViewModel(resolver.Object);
        await viewModel.Msixvc2ProbeTask;

        Assert.IsTrue(viewModel.IsMsixvc2Enabled);
        Assert.AreEqual(string.Empty, viewModel.Msixvc2UnavailableErrorMessage);
    }

    [TestMethod]
    public async Task Msixvc2Probe_DisablesMsixvc2AndSetsMessage_WhenNoToolIsResolved()
    {
        var resolver = new Mock<IMsixvc2ToolResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((Msixvc2Tool)null);

        var viewModel = CreateViewModel(resolver.Object);
        await viewModel.Msixvc2ProbeTask;

        Assert.IsFalse(viewModel.IsMsixvc2Enabled);
        Assert.AreEqual(
            PackageUploader.UI.Resources.Strings.MainPage.MakePkg2NotFoundErrorMsg,
            viewModel.Msixvc2UnavailableErrorMessage);
    }

    [TestMethod]
    public async Task Msixvc2Probe_DoesNotBlockTheConstructor()
    {
        // The probe launches a child process and can block for up to the probe timeout (twice, if
        // MakePkg.exe fails and we fall back to makepkg2.exe). It must never run inline on the UI
        // thread during construction.
        var probeStarted = new ManualResetEventSlim(false);
        var releaseProbe = new ManualResetEventSlim(false);

        var resolver = new Mock<IMsixvc2ToolResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() =>
                {
                    probeStarted.Set();
                    releaseProbe.Wait(TimeSpan.FromSeconds(30));
                    return new Msixvc2Tool(@"C:\gdk\MakePkg.exe", IsMakePkg2Fallback: false);
                });

        var stopwatch = Stopwatch.StartNew();
        var viewModel = CreateViewModel(resolver.Object);
        stopwatch.Stop();

        Assert.IsTrue(probeStarted.Wait(TimeSpan.FromSeconds(10)), "The probe should have been started in the background.");
        Assert.IsTrue(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"The constructor blocked for {stopwatch.Elapsed.TotalSeconds:F1}s waiting on the capability probe.");

        // The property keeps its safe default until the probe reports back.
        Assert.IsFalse(viewModel.IsMsixvc2Enabled);

        releaseProbe.Set();
        await viewModel.Msixvc2ProbeTask;

        Assert.IsTrue(viewModel.IsMsixvc2Enabled);
    }

    [TestMethod]
    public async Task Msixvc2Probe_DisablesMsixvc2_WhenTheResolverThrows()
    {
        var resolver = new Mock<IMsixvc2ToolResolver>();
        resolver.Setup(x => x.Resolve(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("boom"));

        var viewModel = CreateViewModel(resolver.Object);
        await viewModel.Msixvc2ProbeTask;

        Assert.IsFalse(viewModel.IsMsixvc2Enabled);
    }

    #endregion
}
