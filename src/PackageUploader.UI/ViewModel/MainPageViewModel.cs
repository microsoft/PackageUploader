// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32;
using PackageUploader.UI.Providers;
using PackageUploader.UI.View;

namespace PackageUploader.UI.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    private readonly PathConfigurationProvider _pathConfigurationService;
    
    public ICommand NavigateToPackageCreationCommand { get; }
    public ICommand NavigateToPackageUploadCommand { get; }
    public ICommand NavigateToLoginPage { get; }

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

    public MainPageViewModel(PathConfigurationProvider pathConfigurationService)
    {
        _pathConfigurationService = pathConfigurationService;
        
        NavigateToPackageCreationCommand = new Command(async () => 
        {
            await Shell.Current.GoToAsync("///" + nameof(PackageCreationView));
        }, () => IsMakePkgEnabled);
        
        NavigateToPackageUploadCommand = new Command(async () =>
        {
            await Shell.Current.GoToAsync("///" + nameof(PackageUploadView));
        });
        NavigateToLoginPage = new Command(async () =>
        {
            await Shell.Current.GoToAsync("///" + nameof(LoginView));
        });

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
    }

    private static string ResolveExecutablePath(string exeName)
    {
        // We search in several locations in priority order:
        // 1. Next to our current executable
        // 2. In the CurrentDirectory
        // 3. In the GDK if it's installed
        // 4. In the directories specified by the PATH environment variable

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

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
