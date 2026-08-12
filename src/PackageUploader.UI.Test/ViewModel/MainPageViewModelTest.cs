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
using System.IO;
using System.Linq;
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
    private readonly List<string> _tempDirectories = new();

    private MainPageViewModel _mainPageViewModel;

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var directory in _tempDirectories)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Temp cleanup is best effort.
            }
        }

        _tempDirectories.Clear();
    }

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
            new ToolPathResolver(),
            _logger.Object
        );
    }

    [TestMethod]
    public void TestToolPathsComeFromTheSharedResolver()
    {
        // Tool discovery is shared with the command line so both hosts resolve the same binaries.
        // Injecting a stub proves the view model asks for each tool by name and stores what it gets,
        // rather than searching for them itself.
        var pathConfiguration = new Mock<PathConfigurationProvider>();
        var pathResolver = new StubToolPathResolver
        {
            Results =
            {
                ["MakePkg.exe"] = CreateTempFile("MakePkg.exe"),
                ["SubmissionValidator.dll"] = CreateTempFile("SubmissionValidator.dll"),
                ["makepkg2.exe"] = CreateTempFile("makepkg2.exe"),
            }
        };

        var viewModel = new MainPageViewModel(
            pathConfiguration.Object,
            new UserLoggedInProvider(),
            _authenticationService.Object,
            _windowService.Object,
            new Msixvc2ToolResolver(),
            pathResolver,
            _logger.Object);

        CollectionAssert.AreEquivalent(
            new[] { "MakePkg.exe", "SubmissionValidator.dll", "makepkg2.exe" },
            pathResolver.RequestedFileNames.Distinct().ToArray());

        Assert.AreEqual(pathResolver.Results["MakePkg.exe"], pathConfiguration.Object.MakePkgPath);
        Assert.AreEqual(pathResolver.Results["SubmissionValidator.dll"], pathConfiguration.Object.BaseSubValPath);
        Assert.AreEqual(pathResolver.Results["makepkg2.exe"], pathConfiguration.Object.MakePkg2Path);
        Assert.IsTrue(viewModel.IsMakePkgEnabled);
    }

    [TestMethod]
    public void TestMissingToolsLeaveMakePkgDisabled()
    {
        // The shared resolver reports a miss as null. The view model must treat that as "not found"
        // rather than storing it or throwing.
        var pathConfiguration = new Mock<PathConfigurationProvider>();
        var pathResolver = new StubToolPathResolver();

        var viewModel = new MainPageViewModel(
            pathConfiguration.Object,
            new UserLoggedInProvider(),
            _authenticationService.Object,
            _windowService.Object,
            new Msixvc2ToolResolver(),
            pathResolver,
            _logger.Object);

        Assert.IsFalse(viewModel.IsMakePkgEnabled);
        Assert.IsFalse(string.IsNullOrEmpty(viewModel.MakePkgUnavailableErrorMessage));
    }

    private string CreateTempFile(string fileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MainPageVmTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _tempDirectories.Add(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    private sealed class StubToolPathResolver : IToolPathResolver
    {
        public Dictionary<string, string> Results { get; } = new();

        public List<string> RequestedFileNames { get; } = new();

        public string Find(string fileName)
        {
            RequestedFileNames.Add(fileName);
            return Results.TryGetValue(fileName, out var path) ? path : null;
        }
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
            new ToolPathResolver(),
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
