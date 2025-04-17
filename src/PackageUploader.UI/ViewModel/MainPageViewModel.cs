// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32;
using PackageUploader.UI.Providers;
using PackageUploader.UI.View;
using PackageUploader.UI.Utility;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

namespace PackageUploader.UI.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    private readonly PathConfigurationProvider _pathConfigurationService;
    private readonly UserLoggedInProvider _userLoggedInProvider;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<MainPageViewModel> _logger;
    private CancellationTokenSource _cancellationTokenSource = new();

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

    public bool NotLoggedIn
    {
        get => !_userLoggedInProvider.UserLoggedIn;
        set
        {
            if(_userLoggedInProvider.UserLoggedIn != !value)
            {
                _userLoggedInProvider.UserLoggedIn = !value;
                OnPropertyChanged();
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

    public MainPageViewModel(PathConfigurationProvider pathConfigurationService, IAccessTokenProvider accessTokenProvider, UserLoggedInProvider userLoggedInProvider, IWindowService windowService, ILogger<MainPageViewModel> logger)
    {
        _pathConfigurationService = pathConfigurationService;
        _accessTokenProvider = accessTokenProvider;
        _userLoggedInProvider = userLoggedInProvider;
        _logger = logger;

        NavigateToPackageCreationCommand = new RelayCommand(() => 
        {
            windowService.NavigateTo(typeof(PackageCreationView));
        }, () => IsMakePkgEnabled);
        
        NavigateToPackageUploadCommand = new RelayCommand(() =>
        {
            windowService.NavigateTo(typeof(PackageUploadView));
        });
        SignInCommand = new RelayCommand(() =>
        {
            SigninStarted = true;
            SigninAsync();
        });
        PackagingLearnMoreURL = new RelayCommand<string>((url) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        });

        NotLoggedIn = true;

        string makePkgPath = ResolveExecutablePath("MakePkg.exe");

        if (File.Exists(makePkgPath))
        {
            _pathConfigurationService.MakePkgPath = makePkgPath;
            IsMakePkgEnabled = true;
        }
        else
        {
            IsMakePkgEnabled = false;
            MakePkgUnavailableErrorMessage = "MakePkg.exe was not found. Please install the GDK in order to package game contents.";
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
        OnPropertyChanged(nameof(NotLoggedIn));
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

    public async void SigninAsync()
    {
        try
        {
            // Cancel any previous login attempt
            _cancellationTokenSource.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();

            var loginTask = Task.Run(async () =>
            {
                return await _accessTokenProvider.GetTokenAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            });

            while (!loginTask.IsCompleted)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                await Task.Delay(500); // Check every 500ms
            }

            var login = await loginTask;

            if (login != null)
            {
                UpdateSignInStatus(login.AccessToken);
            }
        }
        catch (Exception)
        {
            // Ignore failures
        }
    }

    private void UpdateSignInStatus(string accessToken)
    {
        _userLoggedInProvider.UserName = string.Empty;
        _userLoggedInProvider.AccessToken = accessToken;
        _userLoggedInProvider.UserLoggedIn = true;

        // Try to extract user name from the token
        var handler = new JwtSecurityTokenHandler();

        if (handler.ReadToken(accessToken) is JwtSecurityToken jsonToken)
        {
            var claims = jsonToken.Claims;

            if (claims != null)
            {
                _userLoggedInProvider.UserName = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
            }
        }

        OnPropertyChanged(nameof(NotLoggedIn));
    }
}
