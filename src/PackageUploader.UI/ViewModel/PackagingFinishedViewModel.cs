// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.ViewModel
{
    public partial class PackagingFinishedViewModel : BaseViewModel
    {
        private readonly IWindowService _windowService;
        private readonly PackageModelProvider _packageModelProvider;
        private readonly PathConfigurationProvider _pathConfigurationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IProcessStarterService _processStarterService;
        private readonly ILogger<PackagingFinishedViewModel> _logger;

        private readonly string _wdAppPath = string.Empty;

        private PartialGameConfigModel _gameConfigModel;

        private BitmapImage? _packagePreviewImage = null;
        public BitmapImage? PackagePreviewImage
        {
            get => _packagePreviewImage;
            set => SetProperty(ref _packagePreviewImage, value);
        }
        private string _packageFileName = string.Empty;
        public string PackageFileName
        {
            get => _packageFileName;
            set => SetProperty(ref _packageFileName, value);
        }
        private string _packageSize = string.Empty;
        public string PackageSize
        {
            get => _packageSize;
            set => SetProperty(ref _packageSize, value);
        }
        private string _versionNum = string.Empty;
        public string VersionNum
        {
            get => _versionNum;
            set => SetProperty(ref _versionNum, value);
        }
        private string _storeId = string.Empty;
        public string StoreId
        {
            get => _storeId;
            set => SetProperty(ref _storeId, value);
        }

        private string _packageType = string.Empty;
        public string PackageType
        {
            get => _packageType;
            set => SetProperty(ref _packageType, value);
        }

        private bool _isSigningIn = false;
        public bool IsSigningIn
        {
            get => _isSigningIn;
            set => SetProperty(ref _isSigningIn, value);
        }

        private Process? _installGameProcess = null;
        private bool _isInstallingGame = false;

        public ICommand InstallGameCommand { get; }
        public ICommand ViewPackageCommand { get; }
        public ICommand ConfigureUploadCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand GoHomeCommand { get; }

        public PackagingFinishedViewModel(IWindowService windowService, PackageModelProvider packageModelProvider, PathConfigurationProvider pathConfigurationService, IAuthenticationService authenticationService, ILogger<PackagingFinishedViewModel> logger, IProcessStarterService processStarterService)
        {
            _windowService = windowService;
            _packageModelProvider = packageModelProvider;
            _pathConfigurationService = pathConfigurationService;
            _authenticationService = authenticationService;
            _logger = logger;

            _wdAppPath = Path.GetDirectoryName(_pathConfigurationService.MakePkgPath) ?? string.Empty;
            _wdAppPath = Path.Combine(_wdAppPath, "WdApp.exe");

            InstallGameCommand = new RelayCommand(InstallGame, CanInstallGame);
            ViewPackageCommand = new RelayCommand(ViewPackage);
            ConfigureUploadCommand = new RelayCommand(ConfigureUpload);
            ViewLogsCommand = new RelayCommand(ViewLogs);
            GoHomeCommand = new RelayCommand(GoHome);

            _gameConfigModel = new PartialGameConfigModel(_packageModelProvider.Package.GameConfigFilePath);
            _processStarterService = processStarterService;
        }

        public void OnAppearing()
        {
            _gameConfigModel = new PartialGameConfigModel(_packageModelProvider.Package.GameConfigFilePath);
            PackagePreviewImage = LoadBitmapImage(_gameConfigModel.ShellVisuals.Square150x150Logo);
            PackageType = _gameConfigModel.GetDeviceFamily();
            VersionNum = _gameConfigModel.Identity.Version;

            if (!string.IsNullOrEmpty(_gameConfigModel.StoreId))
            {
                StoreId = _gameConfigModel.StoreId;
            }
            else
            {
                StoreId = "None";
            }

            FileInfo packageInfo = new(_packageModelProvider.Package.PackageFilePath);
            PackageFileName = packageInfo.Name;
            PackageSize = TranslateFileSize(packageInfo.Length);
        }

        private bool CanInstallGame()
        {
            return File.Exists(_wdAppPath) && !_isInstallingGame;
        }

        public void InstallGame()
        {
            if (_isInstallingGame)
            {
                return;
            }

            _isInstallingGame = true;

            try
            {
                // Create a temporary batch file to run the command and pause
                string batchFilePath = Path.Combine(Path.GetTempPath(), "InstallGame.bat");

                if(!File.Exists(_packageModelProvider.Package.PackageFilePath))
                {
                    throw new FileNotFoundException("PackageFilePath doesn't exist", 
                                                    _packageModelProvider.Package.PackageFilePath);
                }

                File.WriteAllText(batchFilePath,
                                  @"@echo off
                                  echo """ + _wdAppPath + @""" install """ + _packageModelProvider.Package.PackageFilePath + @"""
                                  """ + _wdAppPath + @""" install """ + _packageModelProvider.Package.PackageFilePath + @"""
                                  pause");

                _installGameProcess = _processStarterService.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFilePath}\"",
                    UseShellExecute = true
                });

                if (_installGameProcess != null)
                {
                    _installGameProcess.EnableRaisingEvents = true;
                    _installGameProcess.Exited += (sender, args) =>
                    {
                        _isInstallingGame = false;
                        _installGameProcess = null;
                        OnPropertyChanged(nameof(CanInstallGame)); // Notify UI to re-evaluate button state

                        // Clean up the temporary batch file
                        if (File.Exists(batchFilePath))
                        {
                            File.Delete(batchFilePath);
                        }
                    };
                }
                else
                {
                    _isInstallingGame = false; // Reset flag if process fails to start
                }
            }
            catch
            {
                _isInstallingGame = false; // Reset flag in case of an exception
                throw;
            }

            OnPropertyChanged(nameof(CanInstallGame)); // Notify UI to re-evaluate button state
        }

        public void ViewPackage()
        {
            _processStarterService.Start("explorer.exe", $"/select, \"{_packageModelProvider.Package.PackageFilePath}\"");
        }

        private void ViewLogs()
        {
            string logPath = _packageModelProvider.PackagingLogFilepath;
            _processStarterService.Start("explorer.exe", $"/select, \"{logPath}\"");
        }

        public async void ConfigureUpload()
        {
            // Ensure signin first
            if (!_authenticationService.IsUserLoggedIn)
            {
                IsSigningIn = true;
                var success = await _authenticationService.SignInAsync();
                IsSigningIn = false;

                if (!success)
                {
                    return; // Don't continue if sign-in failed
                }
            }

            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
                _windowService.NavigateTo(typeof(PackageUploadView));
            //});
        }

        public void GoHome()
        {
            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
                _windowService.NavigateTo(typeof(MainPageView));
            //});
        }

        private static string TranslateFileSize(long size)
        {
            if (size > 1024 * 1024 * 1024)
            {
                return $"{(size / 1024d / 1024d / 1024d):F2} GB"; // Format to 2 decimal places
            }
            else if (size > 1024 * 1024)
            {
                return $"{size / 1024 / 1024} MB"; // No need to include decimal places if the package is less than 1 GB.
            }
            else if (size > 1024)
            {
                return $"{size / 1024} KB";
            }
            else
            {
                return $"{size} B";
            }
        }

        private static BitmapImage LoadBitmapImage(string imagePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath);
            image.EndInit();
            return image;
        }
    }
}
