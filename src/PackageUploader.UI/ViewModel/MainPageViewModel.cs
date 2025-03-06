// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32;
using PackageUploader.UI.Services;
using PackageUploader.UI.View;

namespace PackageUploader.UI.ViewModel;

public partial class MainPageViewModel : BaseViewModel
{
    private readonly PathConfigurationService _pathConfigurationService;
    
    public ICommand NavigateToPackageCreationCommand { get; }
    public ICommand NavigateToPackageUploadCommand { get; }

    private bool _isMakePkgEnabled = true;
    public bool IsMakePkgEnabled 
    { 
        get => _isMakePkgEnabled;
        set => SetProperty(ref _isMakePkgEnabled, value);
    }

    private bool _isPackageUploaderEnabled = true;
    public bool IsPackageUploaderEnabled
    {
        get => _isPackageUploaderEnabled;
        set => SetProperty(ref _isPackageUploaderEnabled, value);
    }

    private string _makePkgUnavailableErrorMessage = string.Empty;
    public string MakePkgUnavailableErrorMessage
    {
        get => _makePkgUnavailableErrorMessage;
        set => SetProperty(ref _makePkgUnavailableErrorMessage, value);
    }

    private string _packageUploaderUnavailableErrorMessage = string.Empty;
    public string PackageUploaderUnavailableErrorMessage
    {
        get => _packageUploaderUnavailableErrorMessage;
        set => SetProperty(ref _packageUploaderUnavailableErrorMessage, value);
    }

    public MainPageViewModel(PathConfigurationService pathConfigurationService)
    {
        _pathConfigurationService = pathConfigurationService;
        
        NavigateToPackageCreationCommand = new Command(async () => 
        {
            await Shell.Current.GoToAsync("///" + nameof(PackageCreationView));
        }, () => IsMakePkgEnabled);
        
        NavigateToPackageUploadCommand = new Command(async () =>
        {
            await Shell.Current.GoToAsync("///" + nameof(PackageUploadView));
        }, () => IsPackageUploaderEnabled);

        string makePkgPath = ResolveExecutablePath("MakePkg.exe");
        string packageUploaderPath = ResolveExecutablePath("PackageUploader.exe");

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

        if (File.Exists(packageUploaderPath))
        {
            _pathConfigurationService.PackageUploaderPath = packageUploaderPath;
            IsPackageUploaderEnabled = true;
        }
        else
        {
            IsPackageUploaderEnabled = false;
            PackageUploaderUnavailableErrorMessage = "PackageUploader.exe was not found. Package upload is unavailable.";
        }
    }

    private static string ResolveExecutablePath(string exeName)
    {
        // We search in several locations in priority order:
        // 1. Next to our current executable
        // 2. In the CurrentDirectory
        // 3. In the GDK if it's installed
        // 4. In the directories specified by the PATH environment variable
        // 5. In a relative output directory (DEBUG only)

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

#if DEBUG
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            var relativeUploaderPath = Path.Combine(assemblyDirectory, "..\\..\\..\\..\\..\\PackageUploader.Application\\bin\\Debug\\net8.0", exeName);
            return relativeUploaderPath;
        }
#endif
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
