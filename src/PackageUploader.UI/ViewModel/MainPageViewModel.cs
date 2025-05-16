// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32;
using PackageUploader.UI.Providers;
using PackageUploader.UI.View;
using PackageUploader.UI.Utility;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace PackageUploader.UI.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    private readonly PathConfigurationProvider _pathConfigurationService;
    private readonly UserLoggedInProvider _userLoggedInProvider;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<MainPageViewModel> _logger;

    public ICommand NavigateToPackageCreationCommand { get; }
    public ICommand NavigateToPackageUploadCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand PackagingLearnMoreURL { get; }

    private bool _isMakePkgEnabled = true;
    public bool IsMakePkgEnabled 
    { 
        get => _isMakePkgEnabled;
        set => SetProperty(ref _isMakePkgEnabled, value);
    }

    private string _makePkgUnavailableErrorMessage = string.Empty;
    public string MakePkgUnavailableErrorMessage
    {
        get => _makePkgUnavailableErrorMessage;
        set => SetProperty(ref _makePkgUnavailableErrorMessage, value);
    }

    public bool IsUserLoggedIn
    {
        get => _userLoggedInProvider.UserLoggedIn;
        set
        {
            if (_userLoggedInProvider.UserLoggedIn != value)
            {
                _userLoggedInProvider.UserLoggedIn = value;
                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
        }
    }

    private bool _signinStarted = false;
    public bool SigninStarted
    {
        get => _signinStarted;
        set
        {
            if (_signinStarted != value)
            {
                _signinStarted = value;
                OnPropertyChanged(nameof(SigninStarted));
            }
        }
    }

    public MainPageViewModel(
        PathConfigurationProvider pathConfigurationService, 
        UserLoggedInProvider userLoggedInProvider, 
        IAuthenticationService authenticationService,
        IWindowService windowService, 
        ILogger<MainPageViewModel> logger)
    {
        _pathConfigurationService = pathConfigurationService;
        _userLoggedInProvider = userLoggedInProvider;
        _authenticationService = authenticationService;
        _logger = logger;

        // Subscribe to UserLoggedInProvider changes
        _userLoggedInProvider.PropertyChanged += UserLoggedInProvider_PropertyChanged;

        NavigateToPackageCreationCommand = new RelayCommand(() => 
        {
            windowService.NavigateTo(typeof(PackageCreationView));
        }, () => IsMakePkgEnabled);
        
        SignInCommand = new RelayCommand(async () =>
        {
            SigninStarted = true;
            await _authenticationService.SignInAsync();
            SigninStarted = false;
            OnPropertyChanged(nameof(IsUserLoggedIn));
        });

        NavigateToPackageUploadCommand = new RelayCommand(async () =>
        {
            if (!IsUserLoggedIn)
            {
                SigninStarted = true;
                await _authenticationService.SignInAsync();
                SigninStarted = false;
            }

            if (IsUserLoggedIn)
            {
                windowService.NavigateTo(typeof(PackageUploadView));
            }
        });

        PackagingLearnMoreURL = new RelayCommand<string>((url) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        });

        IsUserLoggedIn = false;

        string makePkgPath = ResolveExecutablePath("MakePkg.exe");

        if (File.Exists(makePkgPath))
        {
            _pathConfigurationService.MakePkgPath = makePkgPath;
            IsMakePkgEnabled = true;
        }
        else
        {
            IsMakePkgEnabled = false;
            //MakePkgUnavailableErrorMessage = "MakePkg.exe was not found. Please install the GDK in order to package game contents.";
            MakePkgUnavailableErrorMessage = PackageUploader.UI.Resources.Strings.MainPage.MakePackageNotFoundErrorMsg;
        }

        // Log version of the tool
        _logger.LogInformation("PackageUploader.UI version {version} is starting from location {location}.", GetVersion(), AppContext.BaseDirectory);

        string makePkgVersion = string.Empty;
        if (File.Exists(makePkgPath))
        {
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(makePkgPath);
            makePkgVersion = fileVersionInfo.FileVersion ?? string.Empty;

            _logger.LogInformation("Using MakePkg.exe version: {makePkgVersion} from location {makePkgLocation}.", makePkgVersion, makePkgPath);
        }
    }

    private void UserLoggedInProvider_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserLoggedInProvider.UserLoggedIn))
        {
            OnPropertyChanged(nameof(IsUserLoggedIn));
        }
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (assemblyVersionAttribute is not null)
        {
            return assemblyVersionAttribute.InformationalVersion;
        }
        return assembly.GetName().Version?.ToString() ?? string.Empty;
    }

    public void OnAppearing()
    {
        OnPropertyChanged(nameof(IsUserLoggedIn));
    }

    private static string ResolveExecutablePath(string exeName)
    {
        // We search in several locations in priority order:
        // 1. Next to our current executable
        // 2. In the CurrentDirectory
        // 3. In the GDK if it's installed
        // 4. In the directories specified by the PATH environment variable

        // Use AppContext.BaseDirectory instead of Assembly.Location for single-file compatibility
        var assemblyDirectory = AppContext.BaseDirectory;

        if (Directory.Exists(assemblyDirectory))
        {
            var nextToExePath = Path.Combine(assemblyDirectory, exeName);

            if (File.Exists(nextToExePath))
            {
                return nextToExePath;
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();

        var currentDirectoryPath = Path.Combine(currentDirectory, exeName);

        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        string GdkRegistryPath = @"SOFTWARE\Microsoft\GDK\Installed Roots";
        string? gdkPath = Registry.GetValue($@"HKEY_LOCAL_MACHINE\{GdkRegistryPath}", "GDKInstallPath", null) as string;

        if (!string.IsNullOrEmpty(gdkPath))
        {
            var gdkExePath = Path.Combine(gdkPath, "bin", exeName);
            if (File.Exists(gdkExePath))
            {
                return gdkExePath;
            }
        }

        string? exePath = FindExecutableInPath(exeName);

        if (File.Exists(exePath))
        {
            return exePath;
        }

        return string.Empty;
    }

    private static string? FindExecutableInPath(string exeName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathValue))
        {
            return null;
        }

        var paths = pathValue.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var exePath = Path.Combine(path, exeName);
            if (File.Exists(exePath))
            {
                return exePath;
            }
        }
        return null;
    }
}
